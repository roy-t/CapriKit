"""
Exports a collection from a .blend file to the CapriKit Textureless Model format (see CKTModelData.cs).
Use Export-CKTModel.ps1, or run Blender directly:

    blender --background <file.blend> --python-exit-code 1 --python export_ckt.py -- <collection> <output file>

Collection "<name>" becomes LOD 0, collections named "<name>.lod.<n>" become LOD n. Extra LODs are optional,
but must be numbered consecutively starting at 1. Every mesh object in a LOD collection (including nested
collections) is exported, regardless of its visibility. Modifiers are applied.

Conversion from Blender:
- Blender is right-handed Z-up, CKT is right-handed Y-up: (x, y, z) -> (x, z, -y). Blender's front (-Y) becomes +Z.
- Blender front faces are counter-clockwise, CKT front faces are clockwise, so the winding is reversed.
- Positions are in world space, converted to meters using the scene's unit scale.
- Material properties come from the first Principled BSDF node. Linked inputs (textures) are ignored.

File layout, little-endian, in the field order of the C# records:
- header:    26 bytes ASCII file type, int32 version, name (as BinaryWriter.Write(string): 7-bit encoded
             length followed by UTF-8 bytes), int32 material, mesh, vertex, index and triangle counts
- materials: 9 x float32 (base color rgb, metallic, roughness, emission color rgb, emission strength)
- meshes:    6 x uint32 (vertex offset/count, index offset/count, triangle offset/count),
             6 x float32 (bounds min xyz, bounds max xyz)
- vertices:  6 x float32 (position xyz, normal xyz)
- indices:   uint32, relative to the first vertex of their mesh
- triangles: uint32 (material index)
"""

import re
import struct
import sys

import bpy
from mathutils import Vector

FILE_TYPE = b"CapriKit.Textureless.Model"
FILE_TYPE_VERSION = 1

# Used for triangles without a material, matches Blender's own default
DEFAULT_MATERIAL = (0.8, 0.8, 0.8, 0.0, 0.5, 0.0, 0.0, 0.0, 0.0)


def parse_arguments():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 2:
        raise ValueError("Expected arguments after '--': <collection> <output file>")
    return argv[0], argv[1]


def find_lod_collections(name):
    base = bpy.data.collections.get(name)
    if base is None:
        raise ValueError(f"Collection '{name}' does not exist")

    pattern = re.compile(rf"^{re.escape(name)}\.lod\.(\d+)$")
    lods = {}
    for collection in bpy.data.collections:
        match = pattern.match(collection.name)
        if match:
            lod = int(match.group(1))
            if lod in lods:
                raise ValueError(f"Collections '{lods[lod].name}' and '{collection.name}' both define LOD {lod}")
            lods[lod] = collection

    expected = list(range(1, len(lods) + 1))
    if sorted(lods) != expected:
        raise ValueError(f"LOD collections of '{name}' must be numbered consecutively starting at 1, found LODs {sorted(lods)}")

    return [base] + [lods[lod] for lod in expected]


def create_depsgraph(collections):
    # Objects in excluded or unlinked collections, or disabled in viewports, are not evaluated so their modifiers
    # would be silently skipped. A temporary scene sidesteps collection state. The .blend file is never saved.
    scene = bpy.data.scenes.new("ckt_export")
    for collection in collections:
        scene.collection.children.link(collection)
        for obj in collection.all_objects:
            obj.hide_viewport = False

    with bpy.context.temp_override(scene=scene, view_layer=scene.view_layers[0]):
        return bpy.context.evaluated_depsgraph_get()


def read_material(material):
    if material is None:
        return DEFAULT_MATERIAL

    principled = None
    if material.node_tree is not None:
        principled = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)

    if principled is None:
        r, g, b, _ = material.diffuse_color
        return (r, g, b, material.metallic, material.roughness, 0.0, 0.0, 0.0, 0.0)

    inputs = principled.inputs
    names = ("Base Color", "Metallic", "Roughness", "Emission Color", "Emission Strength")
    linked = [n for n in names if inputs[n].is_linked]
    if linked:
        print(f"Warning: material '{material.name}' has linked inputs {linked}, using their unlinked values")

    # Blender stores colors in linear space, like CKT
    base = inputs["Base Color"].default_value
    emission = inputs["Emission Color"].default_value
    return (base[0], base[1], base[2],
            inputs["Metallic"].default_value,
            inputs["Roughness"].default_value,
            emission[0], emission[1], emission[2],
            inputs["Emission Strength"].default_value)


def to_y_up(v):
    return (v.x, v.z, -v.y)


class ModelBuilder:
    def __init__(self):
        self.materials = []
        self.material_lookup = {}
        self.meshes = []
        self.vertices = []
        self.indices = []
        self.triangles = []

    def material_index(self, material):
        key = material.name_full if material is not None else None
        index = self.material_lookup.get(key)
        if index is None:
            index = len(self.materials)
            self.material_lookup[key] = index
            self.materials.append(read_material(material))
        return index

    def add_lod(self, collection, depsgraph, unit_scale):
        objects = [o for o in collection.all_objects if o.type == 'MESH']
        if not objects:
            raise ValueError(f"Collection '{collection.name}' contains no mesh objects")

        vertex_offset = len(self.vertices)
        index_offset = len(self.indices)
        triangle_offset = len(self.triangles)

        # Vertices are deduplicated per LOD so that indices stay relative to the LOD's slice
        vertex_lookup = {}
        for obj in objects:
            evaluated = obj.evaluated_get(depsgraph)
            mesh = evaluated.to_mesh()
            try:
                self._add_mesh(obj, evaluated.matrix_world, mesh, unit_scale, vertex_lookup)
            finally:
                evaluated.to_mesh_clear()

        lod_vertices = self.vertices[vertex_offset:]
        bounds_min = [min(v[0][axis] for v in lod_vertices) for axis in range(3)]
        bounds_max = [max(v[0][axis] for v in lod_vertices) for axis in range(3)]

        self.meshes.append((
            vertex_offset, len(self.vertices) - vertex_offset,
            index_offset, len(self.indices) - index_offset,
            triangle_offset, len(self.triangles) - triangle_offset,
            bounds_min, bounds_max))

    def _add_mesh(self, obj, matrix_world, mesh, unit_scale, vertex_lookup):
        mesh.calc_loop_triangles()
        normal_matrix = matrix_world.to_3x3().inverted_safe().transposed()
        # A mirroring transform (negative scale) already reverses the winding
        mirrored = matrix_world.determinant() < 0.0
        slots = [slot.material for slot in obj.material_slots]

        for triangle in mesh.loop_triangles:
            corners = []
            for vertex_index, split_normal in zip(triangle.vertices, triangle.split_normals):
                position = to_y_up(matrix_world @ mesh.vertices[vertex_index].co * unit_scale)
                normal = to_y_up((normal_matrix @ Vector(split_normal)).normalized())
                key = (position, normal)
                index = vertex_lookup.get(key)
                if index is None:
                    index = len(vertex_lookup)
                    vertex_lookup[key] = index
                    self.vertices.append(key)
                corners.append(index)

            if not mirrored:
                corners[1], corners[2] = corners[2], corners[1]
            self.indices.extend(corners)

            material = slots[triangle.material_index] if triangle.material_index < len(slots) else None
            self.triangles.append(self.material_index(material))


def write_string(file, text):
    data = text.encode("utf-8")
    length = len(data)
    while length >= 0x80:
        file.write(bytes([(length & 0x7F) | 0x80]))
        length >>= 7
    file.write(bytes([length]))
    file.write(data)


def write_model(path, name, model):
    with open(path, "wb") as file:
        file.write(FILE_TYPE)
        file.write(struct.pack("<i", FILE_TYPE_VERSION))
        write_string(file, name)
        file.write(struct.pack("<5i", len(model.materials), len(model.meshes), len(model.vertices),
                               len(model.indices), len(model.triangles)))

        for material in model.materials:
            file.write(struct.pack("<9f", *material))
        for (*counts, bounds_min, bounds_max) in model.meshes:
            file.write(struct.pack("<6I6f", *counts, *bounds_min, *bounds_max))
        for position, normal in model.vertices:
            file.write(struct.pack("<6f", *position, *normal))
        file.write(struct.pack(f"<{len(model.indices)}I", *model.indices))
        file.write(struct.pack(f"<{len(model.triangles)}I", *model.triangles))


def main():
    name, output = parse_arguments()
    collections = find_lod_collections(name)
    unit_scale = bpy.context.scene.unit_settings.scale_length
    depsgraph = create_depsgraph(collections)

    model = ModelBuilder()
    for collection in collections:
        model.add_lod(collection, depsgraph, unit_scale)

    write_model(output, name, model)
    print(f"Exported '{name}' to '{output}': {len(model.meshes)} LOD(s), {len(model.materials)} material(s), "
          f"{len(model.vertices)} vertices, {len(model.triangles)} triangles")


main()

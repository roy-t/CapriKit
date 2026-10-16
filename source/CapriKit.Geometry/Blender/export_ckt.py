"""
Exports a collection from a .blend file to the CapriKit Textureless Model format (see CKTModelData.cs).
Use Export-CKTModel.ps1, or run Blender directly:

    blender --background <file.blend> --python-exit-code 1 --python export_ckt.py -- <collection> <output file>

Collection "<name>" becomes LOD 0, collections named "<name>.lod.<n>" become LOD n. Extra LODs are optional,
but must be numbered consecutively starting at 1.

Blender's built-in OBJ exporter evaluates each LOD collection the way it is rendered: modifiers (render settings),
collection instances, geometry nodes instances, curves and text are all included, objects and collections disabled
for rendering are not. The view layer state of the LOD collections (excluded, hidden) is ignored. This script only
converts the OBJ and MTL files to CKT, triangulating faces the exporter left as polygons and skipping objects
the exporter wrote twice.

Conversion from Blender:
- Blender is right-handed Z-up, CKT is right-handed Y-up: (x, y, z) -> (x, z, -y). Blender's front (-Y) becomes +Z.
- OBJ front faces are counter-clockwise, CKT front faces are clockwise, so the winding is reversed.
- Positions are in world space, converted to meters using the scene's unit scale.
- Materials come from the MTL file: Kd (base color), Pm (metallic), Pr (roughness) and Ke (emission color
  multiplied by its strength). Textures are ignored. Triangles without a material use DEFAULT_MATERIAL.

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

import os
import re
import struct
import sys
import tempfile

import bpy
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

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


def export_obj(collection, path, unit_scale):
    # The exporter skips collections that are excluded from, or not linked to, the active view layer.
    # A temporary scene with only this collection sidesteps that. The .blend file is never saved.
    scene = bpy.data.scenes.new("ckt_export")
    scene.collection.children.link(collection)
    with bpy.context.temp_override(scene=scene, view_layer=scene.view_layers[0]):
        result = bpy.ops.wm.obj_export(
            filepath=path, collection=collection.name, export_eval_mode='DAG_EVAL_RENDER', apply_modifiers=True,
            forward_axis='NEGATIVE_Z', up_axis='Y', global_scale=unit_scale, export_triangulated_mesh=True,
            export_normals=True, export_uv=False, export_materials=True, export_pbr_extensions=True,
            export_object_groups=True)
    bpy.data.scenes.remove(scene)

    if result != {'FINISHED'} or not os.path.exists(path):
        raise ValueError(f"Collection '{collection.name}' contains nothing that is rendered")


def read_obj(path):
    """Returns the triangles as [((position, normal) x 3, material name or None)] and the MTL file name"""
    positions, normals, groups = [], [], [[]]
    material, library = None, None
    with open(path, encoding="utf-8") as file:
        for line in file:
            kind, _, rest = line.strip().partition(" ")
            if kind == "v":
                positions.append(tuple(float(c) for c in rest.split()))
            elif kind == "vn":
                normals.append(tuple(float(c) for c in rest.split()))
            elif kind == "g":
                groups.append([])
                # Objects without a material have no usemtl, so they would inherit the previous object's material
                material = None
            elif kind == "usemtl":
                material = rest or None
            elif kind == "mtllib":
                library = rest
            elif kind == "f":
                corners = []
                for token in rest.split():
                    position, _, normal = token.split("/")
                    corners.append((positions[int(position) - 1], normals[int(normal) - 1]))
                groups[-1].extend((triangle, material) for triangle in triangulate(corners))

    # The exporter writes curve objects twice, once as polygons and once as triangles. Groups that use exactly
    # the same vertex positions as an earlier group are coincident, and thus invisible, so they are skipped.
    triangles, seen = [], set()
    for group in groups:
        key = frozenset(position for corners, _ in group for position, _ in corners)
        if key not in seen:
            seen.add(key)
            triangles.extend(group)
    return triangles, library


def triangulate(corners):
    """Triangulates faces the exporter did not triangulate, such as those of curve objects, keeping their winding"""
    if len(corners) == 3:
        return [corners]

    points = [Vector(position) for position, _ in corners]
    # Newell's method gives the face normal, also for concave faces
    normal = sum((a.cross(b) for a, b in zip(points, points[1:] + points[:1])), Vector())
    result = []
    for i, j, k in tessellate_polygon([points]):
        if (points[j] - points[i]).cross(points[k] - points[i]).dot(normal) < 0.0:
            j, k = k, j
        result.append([corners[i], corners[j], corners[k]])
    return result


def read_mtl(path):
    materials = {}
    values = None
    with open(path, encoding="utf-8") as file:
        for line in file:
            kind, _, rest = line.strip().partition(" ")
            if kind == "newmtl":
                values = materials[rest] = {}
            elif values is not None and kind in ("Kd", "Pm", "Pr", "Ke"):
                values[kind] = [float(c) for c in rest.split()]
    return {name: to_ckt_material(values) for name, values in materials.items()}


def to_ckt_material(values):
    base = values.get("Kd", DEFAULT_MATERIAL[0:3])
    metallic = values.get("Pm", [DEFAULT_MATERIAL[3]])[0]
    roughness = values.get("Pr", [DEFAULT_MATERIAL[4]])[0]
    # Ke is the emission color multiplied by its strength, split it again so the color stays in [0..1]
    emission = values.get("Ke", [0.0, 0.0, 0.0])
    strength = max(emission)
    color = [c / strength for c in emission] if strength > 0.0 else [0.0, 0.0, 0.0]
    return (*base, metallic, roughness, *color, strength)


class ModelBuilder:
    def __init__(self):
        self.materials = []
        self.material_lookup = {}
        self.meshes = []
        self.vertices = []
        self.indices = []
        self.triangles = []

    def material_index(self, name, materials):
        index = self.material_lookup.get(name)
        if index is None:
            index = len(self.materials)
            self.material_lookup[name] = index
            self.materials.append(materials[name] if name is not None else DEFAULT_MATERIAL)
        return index

    def add_lod(self, triangles, materials):
        vertex_offset = len(self.vertices)
        index_offset = len(self.indices)
        triangle_offset = len(self.triangles)

        # Vertices are deduplicated per LOD so that indices stay relative to the LOD's slice
        vertex_lookup = {}
        for corners, material in triangles:
            indices = []
            for vertex in corners:
                index = vertex_lookup.get(vertex)
                if index is None:
                    index = len(vertex_lookup)
                    vertex_lookup[vertex] = index
                    self.vertices.append(vertex)
                indices.append(index)

            # Counter-clockwise (OBJ) to clockwise (CKT)
            self.indices.extend((indices[0], indices[2], indices[1]))
            self.triangles.append(self.material_index(material, materials))

        lod_vertices = self.vertices[vertex_offset:]
        bounds_min = [min(v[0][axis] for v in lod_vertices) for axis in range(3)]
        bounds_max = [max(v[0][axis] for v in lod_vertices) for axis in range(3)]

        self.meshes.append((
            vertex_offset, len(self.vertices) - vertex_offset,
            index_offset, len(self.indices) - index_offset,
            triangle_offset, len(self.triangles) - triangle_offset,
            bounds_min, bounds_max))


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

    model = ModelBuilder()
    with tempfile.TemporaryDirectory() as directory:
        for lod, collection in enumerate(collections):
            path = os.path.join(directory, f"lod{lod}.obj")
            export_obj(collection, path, unit_scale)
            triangles, library = read_obj(path)
            if not triangles:
                raise ValueError(f"Collection '{collection.name}' contains no faces that are rendered")
            materials = read_mtl(os.path.join(directory, library)) if library else {}
            model.add_lod(triangles, materials)

    write_model(output, name, model)
    print(f"Exported '{name}' to '{output}': {len(model.meshes)} LOD(s), {len(model.materials)} material(s), "
          f"{len(model.vertices)} vertices, {len(model.triangles)} triangles")


main()

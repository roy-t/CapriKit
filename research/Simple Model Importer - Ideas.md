# Simple Model Importer - Ideas

I want to create a model exporter in Blender (Python) and importer (C#, in a new CapriKit project). Let's first look at what the Blender scene contains. 

In Blender there are collections. I want the exporter to export every collection that matches a prefix. For example given `Container.1TUE` should export the collection `Container.1TUE` and any other collections with the same prefix like `Container.1TUE.LOD1` or `Container.1TUE.LOD2`. So one file contains multiple (related) models. The data is exported as arrays per data type. Which look a little bit like an enhanced version of a combined .obj+.mtl file.

Before exporting all modifiers need to be applied and all polygons need to be triangulated. Everything also needs to be transformed so that Y+ is up instead of Z+. Data is stored in meters

The file is a binary file and starts with a header with a GUID that identifies the file type and a 32bit integer that represents the version of the file type in case we later make changes/extensions. After that there are integers that represent the byte length of each array. So that an importer can pre-allocate arrays to store the data in.

The arrays themselves are laid out as follows:

- materials (an array of all unique materials present in the scene for now only the RGB base color, metalic and roughness values)
- vertices (3 floats per vertex for the x, y and z values)
- indices (integer indices, for example (indices[0], indices[1], indices[2] represent the indexes into the vertices array for the first triangle of the first model)
- triangles (triangles[0] is an integer that points to the material in the materials array that corresponds to the material that the triangle defined by indices[0], indices[1], indices[2] should have. For every 3 entries in indices there is one entry in triangles).

The arrays continguously store the materials, vertices, indices and triangles data for all exported collections. So that I don't have to move data around to later create the buffers in DirectX that are gpoing to hold all this data.

A final array helps me navigate the file. Each element contains the name (length prefixed UTF8 string) of an exported collection  as well as the offsets and lengths for the slices into the vertices, indices and triangles array that represent this collection.


As well as an array with the models (collections) present in the file and their 


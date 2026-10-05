"""Bake the supplied MisterH GLB to native Unity mesh data with installed Blender.
Run with blender --background --disable-autoexec --python <this file>.
"""
import bpy, json, pathlib
from mathutils import Vector
root = pathlib.Path(__file__).resolve().parents[2]
out = root / 'Assets/HumanEye/ThirdParty/MisterH'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(out / 'realistic_human_eye.glb'))
# The source body's local Z is the optical axis; its scale gives a unit eye radius.
body_inverse = bpy.data.objects['mesh_0'].matrix_world.inverted()
groups = {name: dict(name=name, vertices=[], normals=[], uv=[], triangles=[]) for name in ('sclera','iris','pupil','cornea')}
for obj in bpy.data.objects:
    if obj.type != 'MESH': continue
    obj.data.calc_loop_triangles()
    matrix = body_inverse @ obj.matrix_world
    normal_matrix = matrix.to_3x3().inverted().transposed()
    for triangle in obj.data.loop_triangles:
        coords = [obj.data.uv_layers.active.data[i].uv for i in triangle.loops]
        name = 'cornea' if obj.name != 'mesh_0' else 'sclera'
        if name == 'sclera' and sum(p.y for p in coords)/3 < .5:
            name = 'pupil' if max((p-Vector((.5,.25))).length for p in coords) < .105 else 'iris'
        group = groups[name]
        for loop_id in reversed(triangle.loops):
            loop = obj.data.loops[loop_id]
            point = matrix @ obj.data.vertices[loop.vertex_index].co
            normal = (normal_matrix @ obj.data.vertices[loop.vertex_index].normal).normalized()
            # Reflect X to align the source with the lesson; reverse face winding for Unity.
            group['vertices'].append(dict(x=-point.x,y=point.y,z=point.z))
            group['normals'].append(dict(x=-normal.x,y=normal.y,z=normal.z))
            coord = obj.data.uv_layers.active.data[loop_id].uv
            group['uv'].append(dict(x=coord.x,y=coord.y))
            group['triangles'].append(len(group['triangles']))
(root / 'Tools/HumanEye/MisterH_Eye.json').write_text(json.dumps(dict(meshes=list(groups.values())),separators=(',',':')))
for img in bpy.data.images:
    if img.type == 'IMAGE':
        img.filepath_raw = str(out / (img.name.replace('/', '_') + '.png'))
        img.file_format = 'PNG'; img.save()
print({name:len(data['triangles'])//3 for name,data in groups.items()})


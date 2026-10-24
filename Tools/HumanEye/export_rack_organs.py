"""Extract owner-supplied Rack 04 organs; keep the cabinet and source FBX untouched.
Blender --background --factory-startup --python Tools/HumanEye/export_rack_organs.py
"""
import bpy, json, math, pathlib, numpy as np
from mathutils import Matrix, Vector

root = pathlib.Path(__file__).resolve().parents[2]
out = root / 'Assets/HumanEye/ThirdParty/UserModels/RackAnatomy'
out.mkdir(parents=True, exist_ok=True)
source = pathlib.Path(r'C:/Users/ADMIN/Documents/Received Files/Rack 04.fbx')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.parent and o.parent.name == 'Digestive']
assert len(meshes) == 30 and not any(o.type == 'ARMATURE' for o in bpy.context.scene.objects), len(meshes)
source_names = [o.name for o in meshes]
rotation = Matrix.Rotation(math.radians(105), 4, 'Z')
for o in meshes:
    xf = rotation @ o.matrix_world
    o.parent = None; o.matrix_world = Matrix.Identity(4); o.data.transform(xf)
mouth = next(o for o in meshes if o.name == 'Rig_Digestive_Mouth_Geo1')
mouth_lo = Vector([min(v.co[i] for v in mouth.data.vertices) for i in range(3)])
mouth_hi = Vector([max(v.co[i] for v in mouth.data.vertices) for i in range(3)])
mouth_center = (mouth_lo + mouth_hi) * .5
# Rack 04 has no eyes, crown or feet; calibrate its mouth, rather than stretching torso bounds into a whole body.
scale = .32
origin = mouth_center - Vector((0, -.075, 1.56)) / scale
for o in meshes:
    for v in o.data.vertices: v.co = (v.co - origin) * scale
# The mouth and teeth belong to the registration only: an organ view with floating teeth and no skull reads as a
# detached jaw (quality pass, 14 October 2026), so they stay out of the presentation mesh.
teeth = [o for o in meshes if o is mouth or any(k in o.name for k in ('Incisor', 'Canine', 'Premolar', 'Molar'))]
assert len(teeth) == 15, len(teeth)
for o in teeth: bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in meshes if o not in teeth]

materials = list(dict.fromkeys(m for o in meshes for m in o.data.materials))
assert len(materials) <= 16 and all(m and m.use_nodes for m in materials)
grid, tile, edge = 4, 512, 4
size = grid * tile
atlas = np.zeros((size, size, 4), dtype=np.float32); atlas[:, :, 3] = 1
for index, mat in enumerate(materials):
    bs = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    links = bs.inputs['Base Color'].links
    if links:
        assert links[0].from_node.type == 'TEX_IMAGE', mat.name
        img = links[0].from_node.image; img.pixels[0]; img.scale(tile - edge * 2, tile - edge * 2)
        pixels = np.empty((tile - edge * 2) ** 2 * 4, dtype=np.float32); img.pixels.foreach_get(pixels)
        pixels = np.pad(pixels.reshape((tile - edge * 2, tile - edge * 2, 4)), ((edge, edge), (edge, edge), (0, 0)), mode='edge')
    else:
        pixels = np.broadcast_to(np.array(bs.inputs['Base Color'].default_value, dtype=np.float32), (tile, tile, 4)).copy()
    x, y = index % grid, index // grid
    atlas[y * tile:(y + 1) * tile, x * tile:(x + 1) * tile] = pixels
for o in meshes:
    if not o.data.uv_layers: o.data.uv_layers.new(name='UVMap')
    uv = o.data.uv_layers.active
    pixels = np.empty(len(o.data.loops) * 2, dtype=np.float32)
    uv.data.foreach_get('uv', pixels); pixels = pixels.reshape((-1, 2))
    for polygon in o.data.polygons:
        index = materials.index(o.data.materials[polygon.material_index]); x, y = index % grid, index // grid
        part = pixels[polygon.loop_start:polygon.loop_start + polygon.loop_total]
        part[:] = (part * (tile - edge * 2) + np.array((edge + x * tile, edge + y * tile))) / size
    uv.data.foreach_set('uv', pixels.ravel())
    for name in [layer.name for layer in o.data.uv_layers if layer.name != uv.name]:   # by name: removal invalidates layer references
        o.data.uv_layers.remove(o.data.uv_layers[name])
    o.data.uv_layers.active.name = 'UVMap'
image = bpy.data.images.new('Organs_Albedo', width=size, height=size, alpha=False)
image.pixels.foreach_set(atlas.ravel()); image.file_format = 'PNG'; image.filepath_raw = str(out / 'Organs_Albedo.png'); image.save()
material = bpy.data.materials.new('Rack_Organs'); material.use_nodes = True
bs = material.node_tree.nodes.get('Principled BSDF'); bs.inputs['Roughness'].default_value = .6
tex = material.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = image
material.node_tree.links.new(tex.outputs['Color'], bs.inputs['Base Color'])
before = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:
    o.data.materials.clear(); o.data.materials.append(material)
    for p in o.data.polygons: p.material_index = 0
    bpy.context.view_layer.objects.active = o
    if len(o.data.polygons) > 500:
        mod = o.modifiers.new('Presentation mesh budget', 'DECIMATE'); mod.ratio = .4
        bpy.ops.object.modifier_apply(modifier=mod.name)
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]; bpy.ops.object.join()
result = bpy.context.object; result.name = 'Rack_Organs'
for o in list(bpy.context.scene.objects):
    if o != result: bpy.data.objects.remove(o, do_unlink=True)
markers = {'EyeSocket_L': (.032, -.047, 1.654), 'EyeSocket_R': (-.032, -.047, 1.654),
           'FaceDirection': (0, -1, 1.654), 'BodyBase': (0, 0, 0), 'BodyTop': (0, 0, 1.75)}
for name, location in markers.items():
    marker = bpy.data.objects.new(name, None); marker.location = location
    bpy.context.scene.collection.objects.link(marker); marker.select_set(True)
after = sum(len(p.vertices) - 2 for p in result.data.polygons)
assert after < 180000 and len(result.data.materials) == 1 and len(result.data.uv_layers) == 1, (after, len(result.data.materials), [layer.name for layer in result.data.uv_layers])
bpy.ops.export_scene.fbx(filepath=str(out / 'Organs.fbx'), use_selection=True, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True,
    object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
report = dict(source=str(source), output='Organs', source_triangles=before, triangles=after, materials=1,
    atlas=size, rigged=False, yaw_correction=105, source_meshes=source_names, source_materials=[m.name for m in materials],
    included='Lungs, trachea, stomach, liver, colon, small intestine, kidneys, ureters, adrenal glands, thymus, spleen (mouth and teeth excluded)',
    absent='Eyes, brain, skull, external head, skin, feet; mouth and teeth left out on purpose', registration_markers=markers,
    registration='Inferred whole-body frame; mouth center=(0,-0.075,1.56), source scale=0.32. Use BodyBase/BodyTop for vertical fitting; supply the existing project eye separately.',
    bounds_min=[min(v.co[i] for v in result.data.vertices) for i in range(3)],
    bounds_max=[max(v.co[i] for v in result.data.vertices) for i in range(3)])
(out / 'Organs_ImportReport.json').write_text(json.dumps(report, indent=2))
print('RACK_EXPORTED', json.dumps(report))

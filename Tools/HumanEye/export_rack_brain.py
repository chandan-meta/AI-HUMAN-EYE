"""Extract the supplied Rack 06 brain and its embedded maps; preserve UVs and lobe topology."""
import bpy, json, pathlib, hashlib
from mathutils import Vector, Matrix

root = pathlib.Path(__file__).resolve().parents[2]
source = pathlib.Path(r'C:/Users/ADMIN/Documents/Received Files/Rack 06.fbx')
out = root / 'Assets/HumanEye/ThirdParty/UserModels/RackBrain'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
names = ['Frontal_Lobe', 'Parietal_Lobe', 'Temporal_Lobe', 'Occipital_Lobe', 'HINDBRAIN', 'Midbraind', 'Hippocampus', 'Amyglada']
meshes = [bpy.data.objects[n] for n in names]
points = [o.matrix_world @ v.co for o in meshes[:4] for v in o.data.vertices]
lo = Vector([min(p[i] for p in points) for i in range(3)])
hi = Vector([max(p[i] for p in points) for i in range(3)])
center = (lo + hi) / 2
xf = Matrix.Rotation(3.141592653589793, 4, 'Z') @ Matrix.Scale(.13, 4) @ Matrix.Translation(-center)
maps = {}
for img in bpy.data.images:
    if not img.packed_file or 'Brain.fbm' not in img.filepath:
        continue
    name = pathlib.PureWindowsPath(img.filepath).stem
    img.filepath_raw = str(out / (name + '.png')); img.file_format = 'PNG'
    if max(img.size) > 2048: img.scale(2048, 2048)
    img.save()
    maps[name] = list(img.size)
for o in meshes:
    world = o.matrix_world.copy()
    o.parent = None; o.matrix_world = Matrix.Identity(4)
    o.data.transform(xf @ world)
    assert len(o.data.uv_layers) > 0, o.name
    for p in o.data.polygons: p.use_smooth = True
for o in list(bpy.context.scene.objects):
    if o not in meshes: bpy.data.objects.remove(o, do_unlink=True)
for name, position in {'BrainCenter': (0,0,0), 'BrainFront': (0,-1,0), 'BrainUp': (0,0,1)}.items():
    o = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(o); o.location = position
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out / 'Rack06_Brain.fbx'), use_selection=True, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True,
    object_types={'MESH','EMPTY'}, mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
triangles = sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
assert triangles == 77882, triangles
report = dict(source=str(source), sha256=hashlib.sha256(source.read_bytes()).hexdigest(), triangles=triangles,
    meshes=names, textures=maps, uv='Original UVs retained', excluded='Cabinet, glass, rack, lights',
    orientation='Frontal lobe toward BrainFront; BrainUp is superior',
    nerve='No complete optic pathway in supplied brain; existing labeled pathway retained',
    license='User supplied; no new download or inferred redistribution permission')
(out / 'Rack06_ImportReport.json').write_text(json.dumps(report, indent=2))
print('RACK06_EXPORTED', json.dumps(report))

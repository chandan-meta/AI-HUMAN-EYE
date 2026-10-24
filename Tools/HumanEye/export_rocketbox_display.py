"""Bake the owner-supplied Human.unitypackage person (Microsoft Rocketbox Business_Male_06, MIT) to a static display FBX
for the scan subject: arms lowered, eyeballs removed (the lesson's anatomical eyes sit in the sockets), trousers split
from the rest so they stay opaque through the X-ray scan, EyeSocket_L/R empties at the eye centres.
Run: blender --background --factory-startup --python Tools/HumanEye/export_rocketbox_display.py
Writes Assets/HumanEye/ThirdParty/UserModels/Rocketbox/ (Rocketbox_Display.fbx + PNG textures + eye_radius.txt).
"""
import bpy, bmesh, tarfile, tempfile, pathlib, shutil, sys
from mathutils import Vector, Matrix

root = pathlib.Path(__file__).resolve().parents[2]
package = pathlib.Path(r"D:/AI unity project/models/eye/Human.unitypackage")
out = root / "Assets/HumanEye/ThirdParty/UserModels/Rocketbox"
out.mkdir(parents=True, exist_ok=True)
blink_export = '--blink' in sys.argv  # A separate derivative; the existing static display stays untouched.

# ---- unpack the .unitypackage (a tar.gz of <guid>/{pathname,asset})
work = pathlib.Path(tempfile.mkdtemp(prefix="rocketbox_"))
with tarfile.open(package, "r:gz") as tar:
    tar.extractall(work)
files = {}
for d in work.iterdir():
    if (d / "pathname").exists() and (d / "asset").exists():
        files[(d / "pathname").read_text().splitlines()[0]] = d / "asset"
fbx = next(p for k, p in files.items() if k.endswith("Business_Male_06_facial.fbx"))
for k, p in files.items():   # textures -> PNG next to the display model
    if not blink_export and "/Rocketbox/Business_Male_06/" in k and k.endswith(".tga"):
        named = work / pathlib.Path(k).name; shutil.copyfile(p, named)   # Blender detects the format by extension
        img = bpy.data.images.load(str(named)); img.pixels[0]; img.file_format = "PNG"   # touch the pixels: images load lazily
        img.save(filepath=str(out / (pathlib.Path(k).stem + ".png")), quality=100); bpy.data.images.remove(img)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(fbx), ignore_leaf_bones=True)
rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
body = next(o for o in bpy.data.objects if o.type == "MESH")
mw = rig.matrix_world

# ---- eyeballs: faces whose vertices follow the eye bones. The mesh is only a thin front cap, so the eye centre is the
#      eye bone and the cap's front gives the cornea-apex distance; then the cap is deleted.
def group_verts(name):
    gi = body.vertex_groups[name].index
    return {v.index for v in body.data.vertices if any(g.group == gi and g.weight > .5 for g in v.groups)}
eye = {}
for side, bone in (("L", "Bip01 LEye"), ("R", "Bip01 REye")):
    idx = group_verts(bone)
    pts = [body.matrix_world @ body.data.vertices[i].co for i in idx]
    c = rig.matrix_world @ rig.data.bones[bone].head_local
    eye[side] = (c, max((p - c).length for p in pts), idx)   # radius = bone → front of the cap (the cornea apex)
    print("EYE", side, tuple(round(x, 4) for x in c), "radius", round(eye[side][1], 4), "verts", len(idx))
bm = bmesh.new(); bm.from_mesh(body.data)
drop = eye["L"][2] | eye["R"][2]
bm.verts.ensure_lookup_table()
# the mouth interior (teeth, tongue, cavity: 1-5 cm behind the lips) would show as a dark box through the X-ray skin
head_slot = next(i for i, m in enumerate(body.data.materials) if m.name.endswith("_head"))
def mouth(f):
    c = body.matrix_world @ f.calc_center_median()
    return f.material_index == head_slot and abs(c.x) < .03 and 1.556 < c.z < 1.615 and -.105 < c.y < -.055   # above the chin: never the neck
# Preserve the complete lips/chin for the close portrait; the old bounds filter cuts surface faces too.
gone = [f for f in bm.faces if all(v.index in drop for v in f.verts) or (not blink_export and mouth(f))]
print("REMOVED faces", len(gone))
bmesh.ops.delete(bm, geom=gone, context="FACES")
bm.to_mesh(body.data); bm.free()

# ---- trousers: body-material faces below the belt in the rest pose (hands are at shoulder height there)
body_slot = next(i for i, m in enumerate(body.data.materials) if m.name.endswith("_body"))
BELT = .985
me = body.data
legs = {p.index for p in me.polygons if p.material_index == body_slot and
        max((body.matrix_world @ me.vertices[v].co).z for v in p.vertices) < BELT}
print("TROUSER_FACES", len(legs), "of", len(me.polygons))

# ---- lower the arms (rest T-pose -> relaxed, slightly forward), forearms a little bent
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="POSE")
def aim(bone, child, target):
    pb = rig.pose.bones[bone]; cb = rig.pose.bones[child]
    bpy.context.view_layer.update()
    h, d = pb.head.copy(), (cb.head - pb.head).normalized()
    q = d.rotation_difference(Vector(target).normalized())
    pb.matrix = Matrix.Translation(h) @ q.to_matrix().to_4x4() @ Matrix.Translation(-h) @ pb.matrix
    bpy.context.view_layer.update()
for s, x in (("L", 1), ("R", -1)):
    aim(f"Bip01 {s} UpperArm", f"Bip01 {s} Forearm", (.22 * x, .04, -1))
    aim(f"Bip01 {s} Forearm", f"Bip01 {s} Hand", (.1 * x, -.16, -1))
bpy.ops.object.mode_set(mode="OBJECT")

# ---- bake the posed mesh, split trousers, drop the rig
dg = bpy.context.evaluated_depsgraph_get()
closed = None
if blink_export:
    blink = body.data.shape_keys.key_blocks['AU_45_Blink']
    blink.value = 1; bpy.context.view_layer.update()
    closed = bpy.data.meshes.new_from_object(body.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    blink.value = 0; bpy.context.view_layer.update()
baked = bpy.data.meshes.new_from_object(body.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
if closed: assert len(closed.vertices) == len(baked.vertices), 'Blink topology changed'
whole = bpy.data.objects.new("Body", baked); whole.matrix_world = body.matrix_world.copy()
bpy.context.scene.collection.objects.link(whole)
if whole.data.shape_keys: whole.shape_key_clear()
bm = bmesh.new(); bm.from_mesh(whole.data); bm.faces.ensure_lookup_table()
if blink_export:
    index = bm.verts.layers.int.new('blink_index')
    for i, v in enumerate(bm.verts): v[index] = i
trousers_mesh = whole.data.copy()
bm2 = bmesh.new(); bm2.from_mesh(trousers_mesh); bm2.faces.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index in legs], context="FACES")
bmesh.ops.delete(bm2, geom=[f for f in bm2.faces if f.index not in legs], context="FACES")
bm.to_mesh(whole.data); bm.free(); bm2.to_mesh(trousers_mesh); bm2.free()
if blink_export:
    whole.shape_key_add(name='Basis')
    key = whole.shape_key_add(name='Blink')
    index = whole.data.attributes['blink_index']
    for i, point in enumerate(key.data): point.co = closed.vertices[index.data[i].value].co
    changed = sum((a.co - b.co).length > 1e-6 for a, b in zip(key.data, whole.data.vertices))
    assert changed > 20, f'Expected real eyelid movement, got {changed} vertices'
    whole.data.attributes.remove(index)
    print('BLINK_VERTICES', changed)
trousers = bpy.data.objects.new("Trousers", trousers_mesh); trousers.matrix_world = whole.matrix_world.copy()
bpy.context.scene.collection.objects.link(trousers)
for o in list(bpy.data.objects):
    if o not in (whole, trousers): bpy.data.objects.remove(o, do_unlink=True)

# ---- frame: feet on z = 0, centred on the eyes' midline
pts = [whole.matrix_world @ v.co for v in whole.data.vertices] + [trousers.matrix_world @ v.co for v in trousers.data.vertices]
mid = (eye["L"][0] + eye["R"][0]) / 2
T = Matrix.Translation(Vector((-mid.x, -mid.y, -min(p.z for p in pts))))
for o in (whole, trousers):
    o.matrix_world = T @ o.matrix_world
    o.select_set(True)
bpy.context.view_layer.objects.active = whole
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for side in ("L", "R"):
    e = bpy.data.objects.new(f"EyeSocket_{side}", None); e.location = T @ eye[side][0]
    bpy.context.scene.collection.objects.link(e); e.select_set(True)
if not blink_export: (out / "eye_radius.txt").write_text(f"{(eye['L'][1] + eye['R'][1]) / 2:.5f}\n")
bpy.ops.export_scene.fbx(filepath=str(out / ("Rocketbox_Blink.fbx" if blink_export else "Rocketbox_Display.fbx")), use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, object_types={"MESH", "EMPTY"}, use_mesh_modifiers=not blink_export,
                         mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False, add_leaf_bones=False, bake_anim=False)
shutil.rmtree(work, ignore_errors=True)
print("ROCKETBOX_EXPORTED", len(whole.data.polygons), len(trousers.data.polygons), [m.name for m in whole.data.materials])

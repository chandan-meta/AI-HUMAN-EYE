"""Prepare the owner's rigged Niko (niko.fbx, 15 October 2026: Mixamo-named skeleton, Tripo mesh, modelled shape keys Close,
Ou, Blink, Eye_L, Eye_R, Eye_U, Eye_D) for Unity: transforms applied, 1.2 m tall with the feet at the origin, the two pocket
meshes joined to the body (one SkinnedMeshRenderer), at most four weights per vertex, the supplied shape keys kept as they
are, plus one corrective BrowUp (the modelled brows lift) for surprise and curiosity, which the source lacks. The packed
base colour JPEG is written beside the FBX unchanged. No animation clips: NikoRig poses the bones procedurally.
  "C:/Program Files/Blender Foundation/Blender 4.5/blender.exe" --background --factory-startup --python Tools/HumanEye/export_niko.py -- <niko.fbx>"""
import bpy, sys, json, pathlib, numpy as np
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src = pathlib.Path(argv[0])
root = pathlib.Path(__file__).resolve().parents[2]
out = root / "Assets/HumanEye/ThirdParty/UserModels/Niko"; out.mkdir(parents=True, exist_ok=True)
HEIGHT = 1.2
SUPPLIED = {"Close", "Ou", "Blink", "Eye_L", "Eye_R", "Eye_U", "Eye_D"}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(src))
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
body = max(meshes, key=lambda o: len(o.data.vertices))
assert body.data.shape_keys and {k.name for k in body.data.shape_keys.key_blocks[1:]} == SUPPLIED, [k.name for k in body.data.shape_keys.key_blocks]

# ---- one body: the pockets join the main mesh (their vertices enter every shape key at rest)
bpy.ops.object.select_all(action="DESELECT")
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = body
if len(meshes) > 1: bpy.ops.object.join()
arm.name = "Niko_Armature"; body.name = "Niko_Body"; me = body.data; me.name = "Niko_Body"

# ---- apply the armature's import transform (cm in a 0.01-scaled, rotated node) and scale to HEIGHT, feet on the origin
bpy.ops.object.select_all(action="DESELECT"); body.select_set(True); bpy.context.view_layer.objects.active = body
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
for o in (arm, body):
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
s = HEIGHT / (co[:, 2].max() - co[:, 2].min()); lift = -co[:, 2].min()
for o in (arm, body):
    o.location = (0, 0, lift * s); o.scale = (s, s, s)
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.select_all(action="DESELECT"); body.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_NAME")   # keeps the existing weights and the Armature modifier target
for m in [m for m in body.modifiers if m.type == "ARMATURE"][1:]: body.modifiers.remove(m)   # parent_set may add a second modifier
next(m for m in body.modifiers if m.type == "ARMATURE").object = arm

# ---- at most four weights per vertex (Unity's default skin quality), normalised
bpy.ops.object.select_all(action="DESELECT"); body.select_set(True); bpy.context.view_layer.objects.active = body
bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)

# ---- BrowUp: the modelled brows (dark geometry above the eyes) rise by 2.2 % of the height; nothing else moves
co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
img = next(i for i in bpy.data.images if i.size[0] > 0); img.pixels[0]
W, H = img.size; px = np.empty(W * H * 4, np.float32); img.pixels.foreach_get(px); px = px.reshape(H, W, 4)
uv = np.empty(len(me.loops) * 2, np.float32); me.uv_layers.active.data.foreach_get("uv", uv); uv = uv.reshape(-1, 2)
vi = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", vi)
col = np.zeros((len(me.vertices), 3), np.float32); cnt = np.zeros(len(me.vertices))
samp = px[np.clip((uv[:, 1] % 1 * H).astype(int), 0, H - 1), np.clip((uv[:, 0] % 1 * W).astype(int), 0, W - 1), :3]
np.add.at(col, vi, samp); np.add.at(cnt, vi, 1); col /= np.maximum(cnt, 1)[:, None]
eye_keys = me.shape_keys.key_blocks
def moved(name):
    k = np.empty(len(me.vertices) * 3, np.float32); eye_keys[name].data.foreach_get("co", k)
    return np.linalg.norm(k.reshape(-1, 3) - co, axis=1) > 1e-6
eyes = co[moved("Eye_L")]; eye_top = eyes[:, 2].max(); eye_z = eyes[:, 2].mean()
r, g, b = col.T
dark = (r < .42) & (g < .32) & (b < .28) & (r > b)   # dark brown brow paint/geometry
brow = dark & (co[:, 2] > eye_top - .01 * HEIGHT) & (co[:, 2] < eye_top + .12 * HEIGHT) & (co[:, 1] < 0) & (np.abs(co[:, 0]) < .3)
assert brow.sum() > 40, f"brows not found: {brow.sum()}"
key = body.shape_key_add(name="BrowUp", from_mix=False)
d = np.zeros_like(co); d[brow, 2] = .022 * HEIGHT
key.data.foreach_set("co", (co + d).ravel())

# ---- texture beside the FBX (packed in the source), export
(out / "Niko_BaseColor.jpg").write_bytes(img.packed_file.data if img.packed_file else pathlib.Path(bpy.path.abspath(img.filepath)).read_bytes())   # the supplied JPEG, byte for byte
bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); body.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out / "Niko.fbx"), use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                         object_types={"ARMATURE", "MESH"}, use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False,
                         bake_anim=False, path_mode="STRIP", armature_nodetype="NULL")
co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
report = dict(source=str(src), height=HEIGHT, scale=float(s), vertices=len(co), faces=len(me.polygons), bones=len(arm.data.bones),
              bounds=[co.min(0).tolist(), co.max(0).tolist()], eye_centre_z=float(eye_z), brow_vertices=int(brow.sum()),
              max_influences=max(len(v.groups) for v in me.vertices), unweighted_vertices=sum(1 for v in me.vertices if sum(g.weight for g in v.groups) < .01),
              shape_keys=[k.name for k in me.shape_keys.key_blocks], bone_names=[b.name for b in arm.data.bones])
(out / "Niko_ImportReport.json").write_text(json.dumps(report, indent=2))
print("NIKO_EXPORT " + json.dumps({k: v for k, v in report.items() if k != "bone_names"}))

import bpy, os, json, collections, math
from mathutils import Vector, Matrix
SRC = r"C:\Users\ADMIN\AppData\Local\Temp\claude\D--AI-unity-project-AI-HUMAN-EYE\4102d456-6422-45e3-80fc-72f260f6f5db\scratchpad\models\src"
WORK = r"C:\Users\ADMIN\AppData\Local\Temp\claude\D--AI-unity-project-AI-HUMAN-EYE\4102d456-6422-45e3-80fc-72f260f6f5db\scratchpad\models\work"
OUT = r"C:\Users\ADMIN\AppData\Local\Temp\claude\D--AI-unity-project-AI-HUMAN-EYE\4102d456-6422-45e3-80fc-72f260f6f5db\scratchpad\models\out"
FRAME = 61

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "Matt__Motion.Fbx"), use_anim=True, ignore_leaf_bones=True, use_custom_normals=True, use_image_search=False)
rig = bpy.data.objects["Armature"]
body_src = bpy.data.objects["CC_Base_Body"]
bpy.data.objects.remove(bpy.data.objects["CC_Base_Tongue"], do_unlink=True)
sc.frame_set(FRAME)

# ---- append clothes/hair from the trimmed Matt.Fbx blend and bind them to the motion rig
names = ["Fit_shirts", "Slim_Jeans", "Canvas_shoes", "Sweeping_Bangs"]
with bpy.data.libraries.load(os.path.join(WORK, "matt_clothes.blend"), link=False) as (data_from, data_to):
    data_to.objects = names
cloth_objs = []
for o in data_to.objects:
    sc.collection.objects.link(o)
    cloth_objs.append(o)
for o in cloth_objs:
    old_parent = o.parent
    print("CLOTH", o.name, "parent", old_parent.name if old_parent else None, "pinv", [round(x, 4) for x in (o.matrix_parent_inverse.to_translation())], "scale", tuple(old_parent.scale) if old_parent else None)
    o.parent = rig
    for md in o.modifiers:
        if md.type == 'ARMATURE':
            md.object = rig
    # vertex groups must match bone names
    missing = [vg.name for vg in o.vertex_groups if vg.name not in rig.data.bones]
    print("CLOTH_VG_MISSING", o.name, missing)
for o in list(bpy.data.objects):
    if o.type == 'ARMATURE' and o is not rig:
        print("REMOVE extra armature", o.name)
        bpy.data.objects.remove(o, do_unlink=True)
bpy.context.view_layer.update()
sc.frame_set(FRAME)
bpy.context.view_layer.update()

# ---- record bone data (world, before frame transform) at FRAME
mw = rig.matrix_world
hip = mw @ rig.pose.bones["CC_Base_Hip"].head
eyeL = mw @ rig.pose.bones["CC_Base_L_Eye"].head
eyeR = mw @ rig.pose.bones["CC_Base_R_Eye"].head
print("HIP", tuple(round(v, 4) for v in hip), "EYE_L", tuple(round(v, 4) for v in eyeL), "EYE_R", tuple(round(v, 4) for v in eyeR))

bones = {}
for pb in rig.pose.bones:
    b = pb.bone
    bones[pb.name] = {
        "parent": pb.parent.name if pb.parent else None,
        "rest_mat": [list(r) for r in (mw @ b.matrix_local)],
        "pose_mat": [list(r) for r in (mw @ pb.matrix)],
        "rest_head": list(mw @ b.head_local), "rest_tail": list(mw @ b.tail_local),
        "pose_head": list(mw @ pb.head), "pose_tail": list(mw @ pb.tail),
    }

# ---- bake evaluated meshes (pose at FRAME, shape keys, modifiers) into static meshes
dg = bpy.context.evaluated_depsgraph_get()
def bake(o, newname):
    ob = o.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ob, preserve_all_data_layers=True, depsgraph=dg)
    me.name = newname
    new = bpy.data.objects.new(newname, me)
    new.matrix_world = o.matrix_world.copy()
    sc.collection.objects.link(new)
    return new
body = bake(body_src, "Body")
shirt = bake(bpy.data.objects["Fit_shirts"], "Shirt")
jeans = bake(bpy.data.objects["Slim_Jeans"], "Jeans")
shoes = bake(bpy.data.objects["Canvas_shoes"], "Shoes")
hair = bake(bpy.data.objects["Sweeping_Bangs"], "Hair")
for o in (body, shirt, jeans, shoes, hair):
    print("BAKED", o.name, "verts", len(o.data.vertices), "polys", len(o.data.polygons), "uv", [u.name for u in o.data.uv_layers], "shapekeys", bool(o.data.shape_keys), "mats", [m.name if m else None for m in o.data.materials],
          "has_custom_normals", 'custom_normal' in o.data.attributes)
# remove sources + rig
for o in list(bpy.data.objects):
    if o.name not in ("Body", "Shirt", "Jeans", "Shoes", "Hair"):
        bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)

# ---- materials
def make_mat(name, png, alpha=False):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    img = bpy.data.images.get(png) or bpy.data.images.load(os.path.join(OUT, png), check_existing=True)
    img.name = png
    tex.image = img
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.6
    if alpha:
        nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
        mat.surface_render_method = 'BLENDED'
        mat.use_backface_culling = False
        mat.show_transparent_back = False
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat

skin_names = ["Skin_Head", "Skin_Body", "Skin_Arm", "Skin_Leg", "Skin_Nails"]
skin_pngs = ["Skin_Head_D.png", "Skin_Body_D.png", "Skin_Arm_D.png", "Skin_Leg_D.png", "Skin_Nails_D.png"]

# ---- body: split by UV tile, delete eyelash tile 5, shift u into [0,1)
me = body.data
me.materials.clear()
for n, p in zip(skin_names, skin_pngs):
    me.materials.append(make_mat(n, p))
me.materials.append(bpy.data.materials.new("__Eyelash_tmp"))
uv = me.uv_layers.active.data
tile_count = collections.Counter()
for poly in me.polygons:
    t = int(math.floor(uv[poly.loop_start].uv.x))
    t = max(0, min(5, t))
    tile_count[t] += 1
    poly.material_index = t
    for li in poly.loop_indices:
        uv[li].uv.x -= t
print("BODY_TILE_FACES", dict(sorted(tile_count.items())))
# delete eyelash faces (slot 5) in edit mode (keeps custom normals)
for o in bpy.data.objects:
    o.select_set(False)
bpy.context.view_layer.objects.active = body
body.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='DESELECT')
body.active_material_index = 5
bpy.ops.object.material_slot_select()
bpy.ops.mesh.delete(type='FACE')
bpy.ops.mesh.select_all(action='DESELECT')
bpy.ops.object.mode_set(mode='OBJECT')
body.active_material_index = 5
bpy.ops.object.material_slot_remove()
bpy.data.materials.remove(bpy.data.materials["__Eyelash_tmp"])
used = set(i for p in me.polygons for i in p.vertices)
print("BODY_AFTER", "verts", len(me.vertices), "polys", len(me.polygons), "loose_verts", len(me.vertices) - len(used), "mats", [m.name for m in me.materials])
cnt = collections.Counter(p.material_index for p in me.polygons)
print("BODY_SLOT_FACES", dict(sorted(cnt.items())))
umin = min(l.uv.x for l in me.uv_layers.active.data); umax = max(l.uv.x for l in me.uv_layers.active.data)
print("BODY_U_RANGE", round(umin, 4), round(umax, 4))

# ---- clothes materials
def set_single_mat(o, mat):
    o.data.materials.clear()
    o.data.materials.append(mat)
    for p in o.data.polygons:
        p.material_index = 0
set_single_mat(shirt, make_mat("Shirt", "Shirt_D.png"))
set_single_mat(jeans, make_mat("Jeans", "Jeans_D.png"))
set_single_mat(shoes, make_mat("Shoes", "Shoes_D.png"))
# hair: slot 0 = Scalp_Transparency, slot 1 = Hair_Transparency in the source
hm = hair.data
src_names = [m.name if m else None for m in hm.materials]
print("HAIR_SRC_MATS", src_names, "faces per slot", dict(collections.Counter(p.material_index for p in hm.polygons)))
hair_mat = make_mat("Hair", "Hair_DA.png", alpha=True)
scalp_mat = make_mat("Scalp", "Scalp_DA.png", alpha=True)
new_mats = []
for n in src_names:
    new_mats.append(scalp_mat if (n and n.startswith("Scalp")) else hair_mat)
orig_idx = [p.material_index for p in hm.polygons]
hm.materials.clear()
# keep index order: ensure unique list and remap
uniq = []
for m in new_mats:
    if m not in uniq:
        uniq.append(m)
remap = {i: uniq.index(m) for i, m in enumerate(new_mats)}
for m in uniq:
    hm.materials.append(m)
for p, i in zip(hm.polygons, orig_idx):
    p.material_index = remap[i]
# drop the source CC materials/images that are no longer used
for m in list(bpy.data.materials):
    if m.users == 0:
        bpy.data.materials.remove(m)
bpy.ops.outliner.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
print("MATS_NOW", sorted(m.name for m in bpy.data.materials), "IMGS_NOW", sorted(i.name for i in bpy.data.images))
print("HAIR_MATS", [m.name for m in hm.materials], "faces per slot", dict(collections.Counter(p.material_index for p in hm.polygons)))

# ---- frame transform: hip XY -> 0, lowest shoe vertex -> z=0
def world_pts(o):
    return [o.matrix_world @ v.co for v in o.data.vertices]
shoe_pts = world_pts(shoes)
minz = min(p.z for p in shoe_pts)
body_pts = world_pts(body)
print("PRE minz shoes", round(minz, 4), "body minz", round(min(p.z for p in body_pts), 4), "body bounds", tuple(round(min(p[i] for p in body_pts), 4) for i in range(3)), tuple(round(max(p[i] for p in body_pts), 4) for i in range(3)))
T = Matrix.Translation(Vector((-hip.x, -hip.y, -minz)))
print("FRAME_T", tuple(round(v, 5) for v in T.to_translation()))
for o in (body, shirt, jeans, shoes, hair):
    o.matrix_world = T @ o.matrix_world
    o.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in (body, shirt, jeans, shoes, hair):
    o.select_set(False)
    pts = world_pts(o)
    print("FINAL", o.name, "bounds", tuple(round(min(p[i] for p in pts), 4) for i in range(3)), tuple(round(max(p[i] for p in pts), 4) for i in range(3)), "matrix identity", o.matrix_world == Matrix.Identity(4))

# ---- eye socket empties
def mk_empty(name, pos):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = 'SPHERE'
    e.empty_display_size = 0.012
    e.location = T @ pos
    sc.collection.objects.link(e)
    return e
eL = mk_empty("EyeSocket_L", eyeL)
eR = mk_empty("EyeSocket_R", eyeR)
print("EYESOCKET_L", tuple(round(v, 5) for v in eL.location), "EYESOCKET_R", tuple(round(v, 5) for v in eR.location))

# bone data in the final frame
for n, d in bones.items():
    for k in ("rest_mat", "pose_mat"):
        d[k] = [list(r) for r in (T @ Matrix(d[k]))]
    for k in ("rest_head", "rest_tail", "pose_head", "pose_tail"):
        d[k] = list(T @ Vector(d[k]))
with open(os.path.join(WORK, "matt_bones.json"), "w") as f:
    json.dump({"frame": FRAME, "T": [list(r) for r in T], "bones": bones}, f)

# ---- image paths: absolute into OUT (exporter copies in place)
for img in bpy.data.images:
    img.filepath = os.path.join(OUT, img.name)
    print("IMG", img.name, img.filepath, tuple(img.size))

# ---- export
for o in bpy.data.objects:
    o.select_set(o.name in ("Body", "Shirt", "Jeans", "Shoes", "Hair", "EyeSocket_L", "EyeSocket_R"))
fbx_path = os.path.join(OUT, "Matt_Display.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', global_scale=1.0,
                         axis_forward='-Z', axis_up='Y', bake_space_transform=True, object_types={'MESH', 'EMPTY'}, use_mesh_modifiers=True,
                         mesh_smooth_type='FACE', use_custom_props=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False, bake_anim=False,
                         use_triangles=False, use_tspace=False)
print("EXPORTED", fbx_path, os.path.getsize(fbx_path))
import shutil
if os.path.isdir(os.path.join(OUT, "Matt_Display.fbm")):
    shutil.rmtree(os.path.join(OUT, "Matt_Display.fbm")); print("REMOVED stale Matt_Display.fbm")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, "matt_posed.blend"), compress=True)
print("SAVED matt_posed.blend")

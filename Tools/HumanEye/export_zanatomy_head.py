"""Headless Blender export of Z-Anatomy (CC BY-SA 4.0) head structures for the Human Eye module.
Run:  blender -b --factory-startup --disable-autoexec Startup.blend --python export_zanatomy_head.py -- <out.fbx>
Exports joined, smooth-shaded meshes: ZA_Skull (cranial bones + mandible), ZA_Brain (cerebral lobes except
occipital, limbic lobe, insula, interlobar sulci, cerebellum, brainstem, midbrain), ZA_VisualArea (occipital lobe),
and two eyeball markers (ZA_EyeMarker_L/R) used only to register the skull to the scan figure's eye sockets.
Inner-ear and kidney components (NC licences) are never selected."""
import bpy, bmesh, sys, mathutils

out = sys.argv[sys.argv.index("--") + 1]
BRAIN = {"Frontal lobe.g", "Parietal lobe.g", "Temporal lobe.g", "Limbic lobe.g", "Insula.g", "Interlobar sulci.g", "Cerebellum.g", "Brainstem.g", "Midbrain.g"}
OCCIPITAL = {"Occipital lobe.g"}
SKULL = {"Bones of cranium.g"}
DEEP = {"Cerebellum.g", "Brainstem.g", "Midbrain.g"}   # dense in the source (~230k polys); the cerebral lobes (~70k) stay at full resolution

def ancestors(o):
    p = o.parent
    while p:
        yield p.name
        p = p.parent

def group(o):
    a = set(ancestors(o))
    if o.name == "Mandible" or a & SKULL: return "ZA_Skull"
    if a & OCCIPITAL: return "ZA_VisualArea"
    if a & BRAIN: return "ZA_BrainDeep" if a & DEEP else "ZA_Brain"
    return None

deps = bpy.context.evaluated_depsgraph_get()
groups = {}
for o in bpy.data.objects:
    if o.type != 'MESH' or len(o.data.polygons) == 0: continue
    g = group(o)
    if g: groups.setdefault(g, []).append(o)

def joined(name, objs, ratio):
    bm = bmesh.new()
    for o in objs:
        me = o.evaluated_get(deps).to_mesh()
        me.transform(o.matrix_world)
        bm.from_mesh(me)
        o.evaluated_get(deps).to_mesh_clear()
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
    mesh = bpy.data.meshes.new(name); bm.to_mesh(mesh); bm.free()
    obj = bpy.data.objects.new(name, mesh); bpy.context.scene.collection.objects.link(obj)
    if ratio < 1:
        mod = obj.modifiers.new("decimate", 'DECIMATE'); mod.ratio = ratio
        bpy.context.view_layer.objects.active = obj; obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=mod.name); obj.select_set(False)
    for p in obj.data.polygons: p.use_smooth = True
    print("EXPORT", name, len(objs), "objects", len(obj.data.polygons), "polys")
    return obj

def marker(name, src):
    o = bpy.data.objects.get(src)
    centre = sum((o.matrix_world @ mathutils.Vector(c) for c in o.bound_box), mathutils.Vector()) / 8
    bpy.ops.mesh.primitive_cube_add(size=0.002, location=centre)
    m = bpy.context.active_object; m.name = name
    print("MARKER", name, tuple(round(v, 4) for v in centre))
    return m

deep = joined("ZA_BrainDeep", groups["ZA_BrainDeep"], .14)
new = [joined("ZA_Skull", groups["ZA_Skull"], .3), joined("ZA_Brain", groups["ZA_Brain"] + [deep], 1), joined("ZA_VisualArea", groups["ZA_VisualArea"], 1)]
new += [marker("ZA_EyeMarker_L", "Posterior segment of eyeball.l"), marker("ZA_EyeMarker_R", "Posterior segment of eyeball.r"),
        marker("ZA_EyeFront_L", "Anterior segment of eyeball.l"), marker("ZA_EyeFront_R", "Anterior segment of eyeball.r")]
bpy.ops.object.select_all(action='DESELECT')
for o in new: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                         bake_space_transform=True, mesh_smooth_type='OFF', use_mesh_modifiers=False, add_leaf_bones=False)
print("WROTE", out)

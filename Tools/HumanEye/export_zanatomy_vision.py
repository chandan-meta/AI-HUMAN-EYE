"""Headless Blender export of the Z-Anatomy (CC BY-SA 4.0) structures the eye cinematics reveal, in the same space and
axis convention as export_zanatomy_head.py (registered in Unity to the scan figure's eye sockets by the eye markers):
  ZA_FaceMuscles  facial + epicranial muscles (orbicularis oculi apart: ZA_Orbicularis), temporalis, masseter, platysma, sternocleidomastoid (slot 0 muscle, 1 tendon)
  ZA_EyeMuscles   the six extra-ocular muscles, levator palpebrae, trochlea and common tendinous ring, both orbits
  ZA_OpticNerve   optic nerves (II), the source curves given a 1.6 mm radius
  ZA_Chiasm / ZA_OpticTract / ZA_LGN / ZA_V1   optic chiasm, optic tracts, lateral geniculate bodies, calcarine cortex
  ZA_Radiation    optic radiations: not in the atlas, drawn as three fibres per side from the LGN round the ventricle to
                  the calcarine cortex (the inferior one through Meyer's loop)
  ZA_EyeMarker_L/R, ZA_EyeFront_L/R   registration markers (as in the head export)
Writes <out>.fbx and <out>.json (signal routes in Z-Anatomy space: retina -> nerve -> chiasm -> tract -> LGN -> radiation -> V1,
nasal fibres crossing at the chiasm). Inner-ear and kidney components (NC licences) are never selected.
Run: blender -b --factory-startup --disable-autoexec Startup.blend --python export_zanatomy_vision.py -- <out.fbx>"""
import bpy, bmesh, sys, json, mathutils
from mathutils import Vector

out = sys.argv[sys.argv.index("--") + 1]
FACE_GROUPS = {"Facial muscles.g", "Epicranius muscle.g"}
FACE_NAMES = {f"{n}.{s}" for s in "lr" for n in ("Temporalis muscle", "Superficial part of masseter", "Deep part of masseter", "Platysma", "Sternocleidomastoid muscle")}
EYE_SKIP = ("tarsus",)
deps = bpy.context.evaluated_depsgraph_get()

def ancestors(o):
    p = o.parent
    while p:
        yield p.name
        p = p.parent

def material_names():
    for name, colour in (("ZA_Muscle", (.55, .1, .08, 1)), ("ZA_Tendon", (.92, .9, .85, 1))):
        if name not in bpy.data.materials:
            m = bpy.data.materials.new(name); m.diffuse_color = colour
    return bpy.data.materials["ZA_Muscle"], bpy.data.materials["ZA_Tendon"]

def joined(name, objs, tendons=False, uv_cm=1.5, ratio=1.0):
    """World-space join; with tendons, faces whose source slot is a 'Tendon' material go to slot 1. Box-projected UVs
    (one tile per uv_cm) let a tiling fibre normal map read on surfaces that have none."""
    bm = bmesh.new()
    for o in objs:
        ev = o.evaluated_get(deps); me = ev.to_mesh(); me.transform(o.matrix_world)
        slots = [s.material.name if s.material else "" for s in o.material_slots]
        first = len(bm.faces)
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        for i, p in enumerate(me.polygons):
            tendon = tendons and p.material_index < len(slots) and "Tendon" in slots[p.material_index]
            bm.faces[first + i].material_index = 1 if tendon else 0
        ev.to_mesh_clear()
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00005)
    uv = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
        u, v = [(1, 2), (0, 2), (0, 1)][ax]
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co[u] * 100 / uv_cm, loop.vert.co[v] * 100 / uv_cm)
    mesh = bpy.data.meshes.new(name); bm.to_mesh(mesh); bm.free()
    obj = bpy.data.objects.new(name, mesh); bpy.context.scene.collection.objects.link(obj)
    muscle, tendon = material_names()
    mesh.materials.append(muscle)
    if tendons: mesh.materials.append(tendon)
    if ratio < 1:   # the layer is on screen for seconds: keep it light for tablets (as the head export does for the skull)
        mod = obj.modifiers.new("decimate", 'DECIMATE'); mod.ratio = ratio
        bpy.context.view_layer.objects.active = obj; obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=mod.name); obj.select_set(False)
    for p in mesh.polygons: p.use_smooth = True
    print("EXPORT", name, len(objs), "objects", len(obj.data.polygons), "polys")
    return obj

def tube(name, paths, radius):
    """Smooth bevelled curves through control points (auto handles), converted to one mesh."""
    cu = bpy.data.curves.new(name + "_curve", 'CURVE'); cu.dimensions = '3D'
    cu.bevel_depth = radius; cu.bevel_resolution = 3; cu.resolution_u = 10; cu.use_fill_caps = True
    for pts in paths:
        s = cu.splines.new('BEZIER'); s.bezier_points.add(len(pts) - 1)
        for bp, p in zip(s.bezier_points, pts):
            bp.co = p; bp.handle_left_type = bp.handle_right_type = 'AUTO'
    tmp = bpy.data.objects.new(name + "_tmp", cu); bpy.context.scene.collection.objects.link(tmp)
    obj = joined(name, [tmp]); bpy.data.objects.remove(tmp)
    return obj

def centre(o):
    return sum((o.matrix_world @ Vector(c) for c in o.bound_box), Vector()) / 8

def marker(name, src):
    bpy.ops.mesh.primitive_cube_add(size=0.002, location=centre(bpy.data.objects[src]))
    m = bpy.context.active_object; m.name = name
    return m

def curve_points(o):
    s = o.data.splines[0]
    pts = [p.co for p in s.bezier_points] if s.type == 'BEZIER' else [p.co.xyz for p in s.points]
    return [o.matrix_world @ Vector(p) for p in pts]

O = bpy.data.objects
face = [o for o in O if o.type == 'MESH' and len(o.data.polygons) and (FACE_GROUPS & set(ancestors(o)) or o.name in FACE_NAMES)]
oculi = [o for o in face if "orbicularis oculi" in o.name]   # its own part: the muscle around the eye glows when the skin wipes off
face = [o for o in face if o not in oculi]
eyem = [o for o in O if o.type == 'MESH' and len(o.data.polygons) and "Extra-ocular muscles.g" in set(ancestors(o)) and not any(k in o.name for k in EYE_SKIP)]
nerves = [O["Optic nerve (II).l"], O["Optic nerve (II).r"]]
for n in nerves:
    n.data.bevel_depth = .0016; n.data.bevel_resolution = 3; n.data.resolution_u = 12; n.data.use_fill_caps = True
deps.update()

new = [joined("ZA_FaceMuscles", face, tendons=True, ratio=.3), joined("ZA_Orbicularis", oculi, tendons=True, ratio=.6), joined("ZA_EyeMuscles", eyem, tendons=True, ratio=.5), joined("ZA_OpticNerve", nerves),
       joined("ZA_Chiasm", [O["Optic chiasm.l"], O["Optic chiasm.r"]]), joined("ZA_OpticTract", [O["Optic tract.l"], O["Optic tract.r"]]),
       joined("ZA_LGN", [O["Lateral geniculate body.l"], O["Lateral geniculate body.r"]]),
       joined("ZA_V1", [O["Calcarine sulcus.l"], O["Calcarine sulcus.r"]])]

# ---- optic radiations and the signal routes (Z-Anatomy space: +x left, -y anterior, +z up) ----
routes, fibres = {}, []
chiasm = (centre(O["Optic chiasm.l"]) + centre(O["Optic chiasm.r"])) / 2
def tract_line(side):
    """Centre line of an optic tract: vertex means in six slabs from the chiasm back to the LGN."""
    o = O["Optic tract." + side]; vs = [o.matrix_world @ v.co for v in o.data.vertices]
    y0, y1 = min(v.y for v in vs), max(v.y for v in vs); line = []
    for k in range(6):
        a, b = y0 + (y1 - y0) * k / 6, y0 + (y1 - y0) * (k + 1) / 6
        slab = [v for v in vs if a <= v.y <= b]
        if slab: line.append(sum(slab, Vector()) / len(slab))
    return line
for side, sx in (("l", 1), ("r", -1)):
    lgn = centre(O["Lateral geniculate body." + side]); v1 = centre(O["Calcarine sulcus." + side]); mid = (lgn + v1) / 2
    up, low = v1 + Vector((0, 0, .006)), v1 + Vector((0, 0, -.006))
    fibres += [
        [lgn, lgn + Vector((sx * .010, .008, .008)), mid + Vector((sx * .016, 0, .010)), v1 + Vector((sx * .006, -.012, .006)), up],
        [lgn, lgn + Vector((sx * .011, .008, .001)), mid + Vector((sx * .018, 0, .002)), v1 + Vector((sx * .008, -.012, 0)), v1],
        [lgn, lgn + Vector((sx * .008, -.010, -.008)), lgn + Vector((sx * .016, -.004, -.014)), mid + Vector((sx * .019, 0, -.008)), v1 + Vector((sx * .008, -.012, -.006)), low],
    ]
    side_tract = tract_line(side)
    centre_fibre = fibres[-2]
    for eye, ex in (("l", 1), ("r", -1)):
        nerve = curve_points(O["Optic nerve (II)." + eye])
        if (nerve[0] - chiasm).length < (nerve[-1] - chiasm).length: nerve.reverse()   # eye first
        crossing = eye != side   # nasal fibres cross to the other hemisphere at the chiasm
        at_chiasm = chiasm + Vector((0 if crossing else sx * .004, 0, 0))
        routes[f"vp_{eye.upper()}{side.upper()}"] = [list(p) for p in nerve + [at_chiasm] + side_tract + [lgn] + centre_fibre[1:]]
new.append(tube("ZA_Radiation", fibres, .0008))
new += [marker("ZA_EyeMarker_L", "Posterior segment of eyeball.l"), marker("ZA_EyeMarker_R", "Posterior segment of eyeball.r"),
        marker("ZA_EyeFront_L", "Anterior segment of eyeball.l"), marker("ZA_EyeFront_R", "Anterior segment of eyeball.r")]

bpy.ops.object.select_all(action='DESELECT')
for o in new: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                         bake_space_transform=True, mesh_smooth_type='OFF', use_mesh_modifiers=False, add_leaf_bones=False)
# Routes in the eye-marker frame, the frame Unity registers the atlas by (Builder.PlaceZAnatomy): origin between the eyes,
# gaze forward, left = left eye minus right eye made perpendicular to the gaze, up = gaze x left; unit = distance between the
# eyes. Unity rebuilds the same frame from the scan figure's sockets, so no axis or unit conversion is involved.
eL, eR = centre(O["Posterior segment of eyeball.l"]), centre(O["Posterior segment of eyeball.r"])
fL, fR = centre(O["Anterior segment of eyeball.l"]), centre(O["Anterior segment of eyeball.r"])
o, unit = (eL + eR) / 2, (eL - eR).length
fwd = ((fL + fR) / 2 - o).normalized(); left = (eL - eR); left = (left - fwd * left.dot(fwd)).normalized(); up = fwd.cross(left)
frame = lambda p: [round((Vector(p) - o).dot(a) / unit, 5) for a in (left, fwd, up)]
json.dump({"frame": "left, forward, up; unit = interocular distance; points flattened x,y,z",
           "routes": [{"id": k, "points": [c for p in v for c in frame(p)]} for k, v in routes.items()]},
          open(out.replace(".fbx", ".json"), "w"), indent=1)
print("WROTE", out, "routes", {k: len(v) for k, v in routes.items()}, "up", tuple(round(c, 3) for c in up))

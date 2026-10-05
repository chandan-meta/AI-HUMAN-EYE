"""Pose the Rigify skeleton to Matt's f61 pose, fit it inside Matt's body, bake, export.
usage: blender -b --factory-startup --python 06_fit_skeleton.py -- <pose|rest> <eye_w> [export] [nolimbs]
"""
import bpy, os, json, math, sys, time, collections
import numpy as np
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree

SRC = r"C:\Users\ADMIN\AppData\Local\Temp\claude\D--AI-unity-project-AI-HUMAN-EYE\4102d456-6422-45e3-80fc-72f260f6f5db\scratchpad\models\src"
WORK = r"C:\Users\ADMIN\AppData\Local\Temp\claude\D--AI-unity-project-AI-HUMAN-EYE\4102d456-6422-45e3-80fc-72f260f6f5db\scratchpad\models\work"
OUT = r"C:\Users\ADMIN\AppData\Local\Temp\claude\D--AI-unity-project-AI-HUMAN-EYE\4102d456-6422-45e3-80fc-72f260f6f5db\scratchpad\models\out"
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MODE = argv[0] if argv else "pose"
EYE_W = float(argv[1]) if len(argv) > 1 else 0.2
EXPORT = "export" in argv[2:]
LIMBS = "nolimbs" not in argv[2:]
KEY = "pose" if MODE != "rest" else "rest"

# ------------------------------------------------------------------ load skeleton
bpy.ops.wm.open_mainfile(filepath=os.path.join(SRC, "skeleton_pose_01.blend"))
sc = bpy.context.scene
rig = bpy.data.objects["rig"]
skel_meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith("Skeleton")]
names_all = [o.name for o in skel_meshes]
for o in list(bpy.data.objects):
    if o is rig or o in skel_meshes:
        continue
    bpy.data.objects.remove(o, do_unlink=True)
rig.hide_viewport = False; rig.hide_set(False)
for o in skel_meshes:
    o.hide_viewport = False; o.hide_set(False); o.hide_render = False
rig.animation_data_clear()
for pb in rig.pose.bones:
    for c in list(pb.constraints):
        pb.constraints.remove(c)
    pb.matrix_basis = Matrix.Identity(4)
rig.data.pose_position = 'POSE'
rig.matrix_world = Matrix.Identity(4)
bpy.context.view_layer.update()

# edit bones: DEF bones re-parented to DEF twins, all disconnected (explicit FK below), no scale inheritance
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb = rig.data.edit_bones
was_connected = {}
for b in eb:
    if b.name.startswith("DEF-"):
        was_connected[b.name] = b.use_connect
        if b.parent and b.parent.name.startswith("ORG-"):
            anc = b.parent; twin = None
            while anc is not None:
                cand = eb.get("DEF-" + anc.name[4:]) if anc.name.startswith("ORG-") else None
                if cand is not None and cand.name != b.name:
                    twin = cand; break
                anc = anc.parent
            if twin is None and ".0" in b.name and "palm" in b.name:
                twin = eb.get("DEF-hand." + b.name[-1])
            if twin is None:
                chain = []; a2 = b.parent
                while a2 is not None:
                    chain.append(a2.name); a2 = a2.parent
                print("NO_TWIN", b.name, chain)
            if twin:
                b.parent = twin
        b.use_connect = False
        b.inherit_scale = 'NONE'
for b in eb:
    if b.name.startswith("DEF-") and (b.parent is None or not b.parent.name.startswith("DEF-")):
        print("DEF_PARENT_NOT_DEF", b.name, b.parent.name if b.parent else None)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()

# ------------------------------------------------------------------ Matt bone data (final frame, metres)
J = json.load(open(os.path.join(WORK, "matt_bones.json")))
CB = J["bones"]
def cc_head(name):
    return Vector(CB[name][KEY + "_head"])
def cc_dir(name, end):
    h = cc_head(name)
    t = cc_head(end) if end in CB else Vector(CB[name][KEY + "_tail"])
    return (t - h).normalized()
def cc_rot(name, key):
    return Matrix(CB[name][key + "_mat"]).to_3x3().normalized()
def cc_delta(name):
    if KEY == "rest":
        return Matrix.Identity(3)
    return cc_rot(name, "pose") @ cc_rot(name, "rest").inverted()

MAP = {
    "DEF-spine": ("CC_Base_Pelvis", "CC_Base_Waist", "delta"),
    "DEF-spine.001": ("CC_Base_Waist", "CC_Base_Spine01", "delta"),
    "DEF-spine.002": ("CC_Base_Spine01", "CC_Base_Spine02", "delta"),
    "DEF-spine.003": ("CC_Base_Spine02", "CC_Base_NeckTwist01", "delta"),
    "DEF-spine.004": ("CC_Base_NeckTwist01", "CC_Base_NeckTwist02", "delta"),
    "DEF-spine.005": ("CC_Base_NeckTwist02", "CC_Base_Head", "delta"),
    "DEF-spine.006": ("CC_Base_Head", None, "delta"),
}
for s_ in ("L", "R"):
    S = s_
    MAP["DEF-shoulder." + s_] = ("CC_Base_%s_Clavicle" % S, "CC_Base_%s_Upperarm" % S, "delta")
    MAP["DEF-upper_arm." + s_] = ("CC_Base_%s_Upperarm" % S, "CC_Base_%s_Forearm" % S, "align")
    MAP["DEF-upper_arm.%s.001" % s_] = ("CC_Base_%s_Upperarm" % S, "CC_Base_%s_Forearm" % S, "align")
    MAP["DEF-forearm." + s_] = ("CC_Base_%s_Forearm" % S, "CC_Base_%s_Hand" % S, "align")
    MAP["DEF-forearm.%s.001" % s_] = ("CC_Base_%s_Forearm" % S, "CC_Base_%s_Hand" % S, "align")
    MAP["DEF-hand." + s_] = ("CC_Base_%s_Hand" % S, None, "align")
    MAP["DEF-thigh." + s_] = ("CC_Base_%s_Thigh" % S, "CC_Base_%s_Calf" % S, "align")
    MAP["DEF-shin." + s_] = ("CC_Base_%s_Calf" % S, "CC_Base_%s_Foot" % S, "align")
    MAP["DEF-foot." + s_] = ("CC_Base_%s_Foot" % S, "CC_Base_%s_ToeBase" % S, "delta")
    MAP["DEF-toe." + s_] = ("CC_Base_%s_ToeBase" % S, None, "delta")
    for i in range(1, 5):
        MAP["DEF-palm.0%d.%s" % (i, s_)] = ("CC_Base_%s_Hand" % S, None, "delta")
    for f, F in (("thumb", "Thumb"), ("f_index", "Index"), ("f_middle", "Mid"), ("f_ring", "Ring"), ("f_pinky", "Pinky")):
        MAP["DEF-%s.01.%s" % (f, s_)] = ("CC_Base_%s_%s1" % (S, F), None, "delta")
        MAP["DEF-%s.02.%s" % (f, s_)] = ("CC_Base_%s_%s2" % (S, F), None, "delta")
        MAP["DEF-%s.03.%s" % (f, s_)] = ("CC_Base_%s_%s2" % (S, F), None, "delta")
SEG_END = {}
for s_ in ("L", "R"):
    SEG_END.update({"DEF-upper_arm.%s" % s_: "DEF-forearm.%s" % s_, "DEF-upper_arm.%s.001" % s_: "DEF-forearm.%s" % s_,
                    "DEF-forearm.%s" % s_: "DEF-hand.%s" % s_, "DEF-forearm.%s.001" % s_: "DEF-hand.%s" % s_,
                    "DEF-shoulder.%s" % s_: "DEF-upper_arm.%s" % s_})
# chains whose total length is matched to Matt's joint distance (segment scale k)
CHAINS = []
for s_ in ("L", "R"):
    CHAINS += [(["DEF-upper_arm.%s" % s_, "DEF-upper_arm.%s.001" % s_], "CC_Base_%s_Upperarm" % s_, "CC_Base_%s_Forearm" % s_),
               (["DEF-forearm.%s" % s_, "DEF-forearm.%s.001" % s_], "CC_Base_%s_Forearm" % s_, "CC_Base_%s_Hand" % s_),
               (["DEF-thigh.%s" % s_], "CC_Base_%s_Thigh" % s_, "CC_Base_%s_Calf" % s_),
               (["DEF-shin.%s" % s_], "CC_Base_%s_Calf" % s_, "CC_Base_%s_Foot" % s_),
               (["DEF-foot.%s" % s_], "CC_Base_%s_Foot" % s_, "CC_Base_%s_ToeBase" % s_)]
ROOT_TARGET = {}
for s_ in ("L", "R"):
    ROOT_TARGET["DEF-upper_arm.%s" % s_] = "CC_Base_%s_Upperarm" % s_
    ROOT_TARGET["DEF-thigh.%s" % s_] = "CC_Base_%s_Thigh" % s_

defs = [b for b in rig.data.bones if b.name.startswith("DEF-")]
def depth(b):
    d = 0
    while b.parent:
        d += 1; b = b.parent
    return d
defs.sort(key=lambda b: depth(b))
REST = {b.name: (b.head_local.copy(), b.tail_local.copy(), b.matrix_local.to_3x3(), b.length) for b in defs}

def solve_rotations():
    """world rotation R per DEF bone (R @ rest_rot = posed orientation)"""
    R = {}; extra = {}; angs = {}
    for b in defs:
        if b.name not in MAP:
            continue
        ccname, ccend, mode = MAP[b.name]
        Rd = cc_delta(ccname)
        rest_head, rest_tail, rest_rot, _ = REST[b.name]
        endb = SEG_END.get(b.name)
        d_rest = ((REST[endb][0] if endb else rest_tail) - rest_head).normalized()
        par = b.parent; Epar = Matrix.Identity(3)
        while par is not None:
            if par.name in extra:
                Epar = extra[par.name]; break
            par = par.parent
        if mode == "align":
            d_target = cc_dir(ccname, ccend)
            d_now = (Rd @ d_rest).normalized()
            Ra = d_now.rotation_difference(d_target).to_matrix()
            R[b.name] = Ra @ Rd; extra[b.name] = Ra; angs[b.name] = math.degrees(d_now.angle(d_target))
        else:
            R[b.name] = Epar @ Rd; extra[b.name] = Epar; angs[b.name] = 0.0
    return R, angs

def subtree_scale(n, XF, ROOTS):
    """uniform scale inherited from the nearest XF ancestor; stops at explicitly rooted (pinned) bones"""
    b = rig.data.bones[n]
    while b is not None:
        if b.name in XF:
            return XF[b.name][1]
        if b.name in ROOTS:
            return 1.0
        b = b.parent
    return 1.0

def apply_fk(R, K, ROOTS, XF=None):
    """explicit forward kinematics: head positions from parents (scaled along parent axis when originally connected),
    K: axial scale per bone, ROOTS: explicit head positions (rig frame),
    XF: {bone: (extra translation, uniform scale)} -- the uniform scale applies to the bone and all its DEF descendants"""
    XF = XF or {}
    H = {}; TAIL = {}; UNI = {}
    for b in defs:
        n = b.name
        rest_head, rest_tail, rest_rot, blen = REST[n]
        Rn = R.get(n, Matrix.Identity(3))
        k = K.get(n, 1.0)
        uni = subtree_scale(n, XF, ROOTS)
        if n in ROOTS:
            h = Vector(ROOTS[n])
        elif b.parent is not None and b.parent.name in H:
            p = b.parent.name
            if was_connected.get(n, False):
                h = TAIL[p]
            else:
                h = H[p] + UNI.get(p, 1.0) * (R.get(p, Matrix.Identity(3)) @ (rest_head - REST[p][0]))
        else:
            h = rest_head.copy()
        if n in XF:
            h = h + Vector(XF[n][0])
            if len(XF[n]) > 2:
                Rn = XF[n][2] @ Rn
        UNI[n] = uni
        rot = Rn @ rest_rot
        H[n] = h
        TAIL[n] = h + rot @ Vector((0, blen * k * uni, 0))
        M = Matrix.Translation(h) @ rot.to_4x4() @ Matrix.Diagonal((uni, uni * k, uni, 1.0))
        pb = rig.pose.bones[n]
        pb.matrix = M
        bpy.context.view_layer.update()
    worst = 0.0
    for b in defs:
        pb = rig.pose.bones[b.name]
        worst = max(worst, (pb.head - H[b.name]).length, (pb.tail - TAIL[b.name]).length)
    print("FK_VERIFY max head/tail error m", round(worst, 6))
    return H, TAIL

R, angs = solve_rotations()
for n in ("DEF-spine", "DEF-spine.003", "DEF-thigh.L", "DEF-shin.L", "DEF-upper_arm.L", "DEF-forearm.L", "DEF-hand.L", "DEF-foot.L"):
    print("ALIGN_CORRECTION_DEG", n, round(angs.get(n, 0.0), 1))
H0, T0 = apply_fk(R, {}, {})

def bake_points():
    dg = bpy.context.evaluated_depsgraph_get()
    pts = {}
    for o in skel_meshes:
        ob = o.evaluated_get(dg)
        me = ob.to_mesh()
        arr = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", arr)
        arr = arr.reshape(-1, 3)
        M = np.array(ob.matrix_world); arr = arr @ M[:3, :3].T + M[:3, 3]
        pts[o.name] = arr
        ob.to_mesh_clear()
    return pts
mesh_pts = bake_points()
allpts = np.concatenate(list(mesh_pts.values()))
print("POSED_SKEL_BOUNDS", allpts.min(0).round(4).tolist(), allpts.max(0).round(4).tolist(), "n", len(allpts))

# ------------------------------------------------------------------ Matt body BVH + eye empties (final frame)
with bpy.data.libraries.load(os.path.join(WORK, "matt_posed.blend"), link=False) as (data_from, data_to):
    data_to.objects = ["Body", "EyeSocket_L", "EyeSocket_R"]
body = data_to.objects[0]; eL = data_to.objects[1]; eR = data_to.objects[2]
for o in data_to.objects:
    sc.collection.objects.link(o)
eyeL = np.array(eL.location); eyeR = np.array(eR.location)
bme = body.data
bvh = BVHTree.FromPolygons([v.co.copy() for v in bme.vertices], [tuple(p.vertices) for p in bme.polygons], all_triangles=False, epsilon=0.0)
print("EYES", eyeL.round(4).tolist(), eyeR.round(4).tolist())

DIRS3 = [Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))]
DIRS5 = DIRS3 + [Vector((-1, 0, 0)), Vector((0, 0, -1))]
DIRS6 = DIRS5 + [Vector((0, -1, 0))]
def ray_hits(v, d):
    n = 0; o_ = v; rc = bvh.ray_cast
    for _ in range(40):
        hit = rc(o_, d, 10.0)
        if hit[0] is None: break
        n += 1; o_ = hit[0] + d * 1e-5
    return n
def signed_dist(P, dirs=DIRS3, envelope=True):
    """distance to Matt's body surface; negative = inside. Inside = ray-parity majority vote, or (envelope) fully enclosed
    by the body in all 6 axis directions (points in the closed mouth cavity count as inside)."""
    out = np.empty(len(P)); fn = bvh.find_nearest; need = len(dirs) // 2 + 1
    for i, p in enumerate(P):
        v = Vector(p)
        loc, nor, idx, dist = fn(v)
        hits = [ray_hits(v, d) for d in dirs]
        votes = sum(h & 1 for h in hits)
        inside = votes >= need
        if not inside and envelope and all(h > 0 for h in hits):
            inside = all(ray_hits(v, d) > 0 for d in DIRS6[len(dirs):])
        out[i] = -dist if inside else dist
    return out

GROUPS = {
    "head": ["Skeleton.083", "Skeleton.146"],
    "torso": ["Skeleton", "Skeleton.062", "Skeleton.097", "Skeleton.098", "Skeleton.099", "Skeleton.116", "Skeleton.199", "Skeleton.200"],
    "arms": ["Skeleton.003", "Skeleton.004", "Skeleton.012", "Skeleton.041"],
    "hands": ["Skeleton.006", "Skeleton.016", "Skeleton.028", "Skeleton.044"],
    "legs": ["Skeleton.001", "Skeleton.002", "Skeleton.123", "Skeleton.124", "Skeleton.126", "Skeleton.127"],
    "feet": ["Skeleton.005", "Skeleton.007", "Skeleton.166", "Skeleton.178"],
}
MARGIN = 0.006
rng = np.random.default_rng(0)
def subsample(pts, frac=6, lo=200):
    return {n: rng.choice(len(p), size=min(len(p), max(lo, len(p) // frac)), replace=False) for n, p in pts.items()}

def transform(P, s, o):
    return P * s + np.asarray(o)

def penalty(P):
    d = signed_dist(P)
    pen = np.maximum(0.0, d + MARGIN)
    return float((pen * pen).mean())

# ------------------------------------------------------------------ global scale/offset fit
def global_fit(pts):
    idx = subsample(pts)
    samp = np.concatenate([pts[n][idx[n]] for n in names_all])
    wts = np.concatenate([np.full(len(idx[n]), 0.25 if n in GROUPS["hands"] + GROUPS["feet"] else 1.0) for n in names_all])
    def objective(s, o):
        d = signed_dist(transform(samp, s, o))
        pen = np.maximum(0.0, d + MARGIN)
        return float((wts * pen * pen).sum() / wts.sum())
    t0 = time.time(); best = None
    for s in (0.95, 0.97, 0.99, 1.01):
        for ox in (-0.02, 0.0, 0.02):
            for oy in (0.04, 0.07, 0.10, 0.13, 0.16):
                for oz in (-0.02, 0.01, 0.04, 0.07):
                    Jv = objective(s, (ox, oy, oz))
                    if best is None or Jv < best[0]:
                        best = (Jv, s, [ox, oy, oz])
    Jb, s, o = best
    steps = [0.01, 0.015, 0.015, 0.015]
    for it in range(60):
        improved = False
        for k in range(4):
            for sign in (-1, 1):
                s2 = s + sign * steps[0] if k == 0 else s
                o2 = list(o)
                if k > 0: o2[k - 1] += sign * steps[k]
                Jv = objective(s2, o2)
                if Jv < Jb:
                    Jb, s, o = Jv, s2, o2; improved = True
        if not improved:
            steps = [x * 0.5 for x in steps]
            if steps[0] < 0.0005: break
    print("GLOBAL_FIT scale", round(s, 4), "offset", [round(v, 4) for v in o], "J", round(Jb, 8), "t", round(time.time() - t0, 1))
    return s, o
s, o = global_fit(mesh_pts)

# ------------------------------------------------------------------ per-segment: limb roots at Matt's joints, segment lengths matched
K = {}; ROOTS = {}
H1 = H0
if LIMBS:
    for chain, a, b_ in CHAINS:
        L = sum(REST[n][3] for n in chain)
        target = (cc_head(b_) - cc_head(a)).length / s
        k = target / L
        for n in chain:
            K[n] = k
        print("SEGMENT_SCALE", chain, "skeleton_len", round(L, 4), "matt_len/s", round(target, 4), "k", round(k, 3))
    for n, cc in ROOT_TARGET.items():
        ROOTS[n] = (cc_head(cc) - Vector(o)) / s
    H1, T1 = apply_fk(R, K, ROOTS)
    mesh_pts = bake_points()

# ------------------------------------------------------------------ orbit centres of the cranium (mean-shift on a frontal ray-depth grid, rig frame)
def orbit_centres(seedL, seedR):
    cr = bpy.data.objects["Skeleton.083"]
    dg = bpy.context.evaluated_depsgraph_get()
    ob = cr.evaluated_get(dg); me = ob.to_mesh(); M = ob.matrix_world
    cb = BVHTree.FromPolygons([M @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons], all_triangles=False, epsilon=0.0)
    ob.to_mesh_clear()
    res = []
    for seed in (seedL, seedR):
        c = np.array(seed, dtype=float); ndeep = 0
        for it in range(8):
            xs = np.arange(c[0] - 0.035, c[0] + 0.035, 0.002); zs = np.arange(c[2] - 0.035, c[2] + 0.035, 0.002)
            D = np.full((len(zs), len(xs)), np.nan)
            for i, z in enumerate(zs):
                for j, x in enumerate(xs):
                    hit = cb.ray_cast(Vector((x, -1.0, z)), Vector((0, 1, 0)), 3.0)
                    if hit[0] is not None: D[i, j] = hit[0].y
            if not np.isfinite(D).any():
                break
            brow = np.nanmin(D)
            X, Z = np.meshgrid(xs, zs)
            rr = np.sqrt((X - c[0]) ** 2 + (Z - c[2]) ** 2)
            deep = np.isfinite(D) & (D - brow > 0.020) & (rr < 0.022)
            ndeep = int(deep.sum())
            if ndeep < 10:
                break
            cn = np.array([X[deep].mean(), 0.0, Z[deep].mean()])
            ring = np.isfinite(D) & (rr > 0.020) & (rr < 0.028)
            rim_y = float(np.nanmin(D[ring])) if ring.any() else brow
            moved = np.linalg.norm(cn[[0, 2]] - c[[0, 2]])
            c = np.array([cn[0], rim_y, cn[2]])
            if moved < 0.0005:
                break
        res.append((c, ndeep))
    return res

def orbit_report(tag, s, o):
    seedL = (eyeL - np.asarray(o)) / s; seedR = (eyeR - np.asarray(o)) / s
    (cL, nL), (cR, nR) = orbit_centres(seedL, seedR)
    oL = transform(cL, s, o); oR = transform(cR, s, o)
    print("ORBIT", tag, "L centre_x, rim_y, centre_z (final m)", oL.round(4).tolist(), "cells", nL, "| R", oR.round(4).tolist(), "cells", nR)
    print("ORBIT_VS_EYE", tag, "L dx,dz(cm)", ((oL - eyeL)[[0, 2]] * 100).round(2).tolist(), "eye_depth_behind_rim_cm", round((eyeL[1] - oL[1]) * 100, 2),
          "| R dx,dz(cm)", ((oR - eyeR)[[0, 2]] * 100).round(2).tolist(), "eye_depth_behind_rim_cm", round((eyeR[1] - oR[1]) * 100, 2))
    return cL, cR
cL, cR = orbit_report("PRE_HEAD", s, o)

# ------------------------------------------------------------------ local rigid+uniform-scale refinements of bone subtrees
SCALE_REG = 1e-4
def local_refine(tag, root, mesh_names, Hcur, k_range=(0.85, 1.1), eye=False, trans_mask=(1, 1, 1), rot_mask=(0, 0, 0), n_samp=1500, margin=0.004,
                 pivot=None, trans_limit=0.03, rot_limit=12.0):
    """rigid approximation: group vertices P' = k*Rot*(P - pivot) + pivot + d (rig frame); converted to a head-based XF for the root bone"""
    head = np.array(Hcur[root])
    pivot = head if pivot is None else np.asarray(pivot, dtype=float)
    P0 = np.concatenate([mesh_pts[n] for n in mesh_names])
    Ps = P0[rng.choice(len(P0), size=min(n_samp, len(P0)), replace=False)]
    eyeLq = (eyeL - np.asarray(o)) / s; eyeRq = (eyeR - np.asarray(o)) / s
    EYE_DEPTH = 0.012 / s
    def rotm(pitch, yaw, roll):
        return np.array(Matrix.Rotation(math.radians(yaw), 3, 'Z') @ Matrix.Rotation(math.radians(roll), 3, 'Y') @ Matrix.Rotation(math.radians(pitch), 3, 'X'))
    def xf(P, x):
        return ((P - pivot) @ rotm(x[4], x[5], x[6]).T) * x[3] + pivot + np.asarray(x[:3])
    def obj(x):
        dd = signed_dist(transform(xf(Ps, x), s, o))
        pen = np.maximum(0.0, dd + margin)
        Jv = float((pen * pen).mean()) + SCALE_REG * (x[3] - 1.0) ** 2
        if eye and EYE_W > 0:
            oL = xf(cL, x); oR = xf(cR, x)
            e = ((oL - eyeLq)[[0, 2]] ** 2).sum() + ((oR - eyeRq)[[0, 2]] ** 2).sum()
            e += 0.25 * ((oL[1] + EYE_DEPTH - eyeLq[1]) ** 2 + (oR[1] + EYE_DEPTH - eyeRq[1]) ** 2)
            Jv += EYE_W * float(e) * s * s
        return Jv
    x = [0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0]   # dx dy dz k pitch yaw roll
    Jb = obj(x); J0 = Jb
    steps = [0.008, 0.008, 0.008, 0.02, 3.0, 3.0, 3.0]
    active = [trans_mask[0], trans_mask[1], trans_mask[2], 1, rot_mask[0], rot_mask[1], rot_mask[2]]
    for it in range(120):
        improved = False
        for q in range(7):
            if not active[q]: continue
            for sign in (-1, 1):
                x2 = list(x); x2[q] += sign * steps[q]
                if not (k_range[0] <= x2[3] <= k_range[1]): continue
                if any(abs(v) > trans_limit / s + 1e-9 for v in x2[:3]): continue
                if any(abs(v) > rot_limit for v in x2[4:]): continue
                Jv = obj(x2)
                if Jv < Jb:
                    Jb, x = Jv, x2; improved = True
        if not improved:
            steps = [v * 0.5 for v in steps]
            if steps[0] < 0.0003: break
    Rm = rotm(x[4], x[5], x[6])
    new_head = pivot + (Rm @ (head - pivot)) * x[3] + np.asarray(x[:3])
    d_head = new_head - head
    print("LOCAL_REFINE", tag, "translation(final m)", [round(v * s, 4) for v in x[:3]], "scale", round(x[3], 4), "pitch/yaw/roll deg", [round(v, 2) for v in x[4:]],
          "J0", round(J0, 8), "J", round(Jb, 8))
    return (Vector(d_head.tolist()), x[3], Matrix(Rm.tolist()))

XF = {}
for n_ in ("DEF-spine.001", "DEF-spine.002"):
    ROOTS[n_] = H1[n_].copy()
hip_mid = (np.array(H1["DEF-thigh.L"]) + np.array(H1["DEF-thigh.R"])) * 0.5
XF["DEF-spine.006"] = local_refine("head", "DEF-spine.006", GROUPS["head"], H1, k_range=(0.9, 1.05), eye=True, rot_mask=(1, 1, 0), trans_limit=0.03, rot_limit=8.0)
XF["DEF-spine"] = local_refine("pelvis", "DEF-spine", ["Skeleton"], H1, k_range=(0.9, 1.0), rot_mask=(1, 0, 0), pivot=hip_mid, trans_limit=0.015, rot_limit=12.0)
XF["DEF-spine.001"] = local_refine("lumbar", "DEF-spine.001", ["Skeleton.062"], H1, k_range=(1.0, 1.0), trans_mask=(0, 1, 1), n_samp=1200, trans_limit=0.015)
for side in ("L", "R"):
    foot_meshes = ["Skeleton.178", "Skeleton.005"] if side == "L" else ["Skeleton.166", "Skeleton.007"]
    XF["DEF-foot." + side] = local_refine("foot." + side, "DEF-foot." + side, foot_meshes, H1, k_range=(0.85, 1.05), rot_mask=(1, 1, 0), trans_limit=0.025, rot_limit=10.0)
    hand_meshes = ["Skeleton.028", "Skeleton.016"] if side == "L" else ["Skeleton.006", "Skeleton.044"]
    XF["DEF-hand." + side] = local_refine("hand." + side, "DEF-hand." + side, hand_meshes, H1, k_range=(0.85, 1.05), rot_mask=(1, 1, 1), trans_limit=0.015, rot_limit=15.0)
    scap = "Skeleton.200" if side == "L" else "Skeleton.199"
    XF["DEF-shoulder." + side] = local_refine("shoulder." + side, "DEF-shoulder." + side, [scap], H1, k_range=(0.9, 1.0), trans_limit=0.02)
H2, T2 = apply_fk(R, K, ROOTS, XF)
mesh_pts = bake_points()
cL, cR = orbit_report("POST_LOCAL", s, o)

# ------------------------------------------------------------------ containment table + worst vertices
def table(s, o, tag):
    rows = []; gsum = {}
    for g, names in GROUPS.items():
        din = 0; dn = 0; dmax = -1
        for n in names:
            P = transform(mesh_pts[n], s, o)
            dd = signed_dist(P, DIRS5)
            ds = signed_dist(P, DIRS5, envelope=False)
            inside = (dd <= 0).mean(); mx = dd.max(); strict = (ds <= 0).mean()
            w = np.argsort(dd)[-3:][::-1]
            rows.append((g, n, len(P), round(100 * inside, 1), round(100 * strict, 1), round(mx * 100, 2), [(P[i].round(3).tolist(), round(dd[i] * 100, 1)) for i in w]))
            din += (dd <= 0).sum(); dn += len(P); dmax = max(dmax, mx); gsum.setdefault(g + "_strict", 0); gsum[g + "_strict"] += (ds <= 0).sum()
        gsum[g] = (round(100 * din / dn, 1), round(dmax * 100, 2), round(100 * gsum.pop(g + "_strict") / dn, 1))
    print("TABLE", tag, "scale", round(s, 4), "offset", [round(v, 4) for v in o])
    print("ROW format: (group, mesh, nverts, inside%_envelope, inside%_strict_parity, max_out_cm)")
    for r in rows:
        print("ROW", tag, r[:6], "worst", r[6])
    for g, v in gsum.items():
        print("GROUP", tag, g, "inside%", v[0], "strict_inside%", v[2], "max_out_cm", v[1])
    return gsum
gs = table(s, o, "FIT")

# ------------------------------------------------------------------ apply fit, bake meshes, save/export
rig.scale = (s, s, s)
rig.location = Vector(o)
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
baked = []
for ob0 in skel_meshes:
    ob = ob0.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ob, preserve_all_data_layers=True, depsgraph=dg)
    new = bpy.data.objects.new(ob0.name + "__baked", me)
    new.matrix_world = ob0.matrix_world.copy()
    sc.collection.objects.link(new)
    baked.append((ob0.name, new, me))
for name, new, me in baked:
    bpy.data.objects.remove(bpy.data.objects[name], do_unlink=True)
for name, new, me in baked:
    new.name = name; me.name = name
bpy.data.objects.remove(rig, do_unlink=True)
for o_ in (body, eL, eR):
    bpy.data.objects.remove(o_, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
for o_ in bpy.data.objects:
    o_.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects["Skeleton"]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bone_mat = bpy.data.materials.get("material_0.001") or bpy.data.materials.new("Bone")
bone_mat.name = "Bone"
if bone_mat.node_tree:
    for n in list(bone_mat.node_tree.nodes):
        if n.type == 'TEX_IMAGE':
            print("BONE_MAT_IMAGE", n.image.name if n.image else None)
            bone_mat.node_tree.nodes.remove(n)
allp = []
for o_ in bpy.data.objects:
    o_.data.materials.clear(); o_.data.materials.append(bone_mat)
    P = np.empty(len(o_.data.vertices) * 3); o_.data.vertices.foreach_get("co", P); allp.append(P.reshape(-1, 3))
allp = np.concatenate(allp)
print("EXPORT_SKEL_BOUNDS", allp.min(0).round(4).tolist(), allp.max(0).round(4).tolist(), "objects", len(bpy.data.objects))
json.dump({"mode": MODE, "scale": s, "offset": list(o), "segment_scale": K, "local_xf": {n_: [list(map(float, v[0])), v[1], [list(r_) for r_ in v[2]]] for n_, v in XF.items()}, "groups": gs,
           "orbitL": transform(cL, s, o).tolist(), "orbitR": transform(cR, s, o).tolist()}, open(os.path.join(WORK, "skel_fit.json"), "w"))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, "skel_export.blend"), compress=True)
print("SAVED skel_export.blend")
if EXPORT:
    fbx_path = os.path.join(OUT, "Skeleton_Display.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', global_scale=1.0,
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, object_types={'MESH', 'EMPTY'}, use_mesh_modifiers=True,
                             mesh_smooth_type='FACE', use_custom_props=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False, bake_anim=False,
                             use_triangles=False, use_tspace=False)
    print("EXPORTED", fbx_path, os.path.getsize(fbx_path))

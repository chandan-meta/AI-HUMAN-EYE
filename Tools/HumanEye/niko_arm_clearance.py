"""Niko's arm poses against his own body (15 October 2026). NikoRig.Arm replicated on the shipped Niko.fbx with its real skin
weights: for every pose in NikoRig (same numbers; keep this table in step with NikoRig.Arms / TalkBeat), the outer half of the
upper arm, the forearm, the hand and the fingers must stay out of the head, and the forearm, hand and fingers out of the torso,
with the head anywhere in NikoRig's range: rolled -8, 0 or +8 degrees combined with 12 degrees up, level or 10 degrees down
(NikoRig clamps roll at 8 and neck plus head pitch, nods included, to that range). The inner half of the upper arm also carries
the collarbone skin, which sits under the jaw by build. Head and torso are per-slice convex hulls of their weighted vertices and
triangle samples.

  "C:/Program Files/Blender Foundation/Blender 4.5/blender.exe" -b --factory-startup --python Tools/HumanEye/niko_arm_clearance.py -- Assets/HumanEye/ThirdParty/UserModels/Niko/Niko.fbx

Prints one row per pose and bend extreme; exits 1 if any pose fails. Unity root frame: x right (the R arm is on -X), y up, z
forward; the FBX's Blender frame maps to it as (-x, z, -y)."""
import sys
import bpy, numpy as np

fbx = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
body = max((o for o in bpy.data.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
me = body.data; me.calc_loop_triangles()
names = [b.name.replace("mixamorig:", "") for b in arm.data.bones]
U = lambda a: np.stack([-a[..., 0], a[..., 2], -a[..., 1]], -1)
V = U(np.array([tuple(body.matrix_world @ v.co) for v in me.vertices], float))
J = U(np.array([tuple(arm.matrix_world @ b.head_local) for b in arm.data.bones], float))
ix = {n: i for i, n in enumerate(names)}
groups = {g.index: g.name.replace("mixamorig:", "") for g in body.vertex_groups}
W = np.zeros((len(V), len(names)))
for v in me.vertices:
    for g in v.groups:
        n = groups[g.group]
        if n in ix: W[v.index, ix[n]] = g.weight
W /= np.maximum(W.sum(1, keepdims=True), 1e-9)
TRIS = np.array([tuple(t.vertices) for t in me.loop_triangles])
dom = np.array(names)[W.argmax(1)]
DEPTH = .01   # 1 cm of skin contact is allowed

def rot(axis, deg):   # numerically the same as Unity's Quaternion.AngleAxis
    k = np.asarray(axis, float); k = k / np.linalg.norm(k); th = np.radians(deg)
    K = np.array([[0, -k[2], k[1]], [k[2], 0, -k[0]], [-k[1], k[0], 0]])
    return np.eye(3) + np.sin(th) * K + (1 - np.cos(th)) * K @ K
def frame(z, pole):   # Unity's Quaternion.LookRotation(z, pole orthogonalised) as a matrix
    z = z / np.linalg.norm(z); up = pole - z * (pole @ z)
    if up @ up < 1e-6: up = np.array([0, 1., 0])
    x = np.cross(up, z); x /= np.linalg.norm(x); return np.stack([x, np.cross(z, x), z], 1)
nz = lambda v: np.asarray(v, float) / np.linalg.norm(v)

def hull2(pts):
    pts = sorted(map(tuple, pts))
    if len(pts) < 3: return None
    cr = lambda o, a, b: (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, up = [], []
    for p in pts:
        while len(lo) >= 2 and cr(lo[-2], lo[-1], p) <= 0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(up) >= 2 and cr(up[-2], up[-1], p) <= 0: up.pop()
        up.append(p)
    return np.array(lo[:-1] + up[:-1])
class Solid:   # stacked 1 cm slices, each a convex hull in XZ; depth > 0 inside
    def __init__(self, P, step=.01):
        self.step = step; self.s = {}
        for y in np.arange(P[:, 1].min(), P[:, 1].max() + step, step):
            sl = P[np.abs(P[:, 1] - y) < step * 1.2][:, [0, 2]]
            h = hull2(sl) if len(sl) >= 3 else None
            if h is not None:
                e = np.roll(h, -1, 0) - h
                self.s[int(round(y / step))] = (h, np.stack([e[:, 1], -e[:, 0]], 1) / np.linalg.norm(e, axis=1)[:, None])
    def depths(self, Q):
        out = np.full(len(Q), -1.0); k = np.round(Q[:, 1] / self.step).astype(int)
        for key in np.unique(k):
            if key in self.s:
                h, n = self.s[key]; m = k == key
                out[m] = np.min(np.einsum('ec,pec->pe', n, h[None] - Q[m][:, [0, 2]][:, None]), 1)
        return out
def surface(mask, n=4):   # vertices plus points spread over the set's triangles (the mesh is sparse in places)
    T = TRIS[mask[TRIS].sum(1) >= 2]
    bc = np.array([(i / n, j / n, 1 - (i + j) / n) for i in range(n + 1) for j in range(n + 1 - i)])
    return np.vstack([V[mask], np.einsum('kb,tbc->tkc', bc, V[T]).reshape(-1, 3)])
HEAD = surface(np.isin(dom, ["Head", "HeadTop_End", "Neck"]))
TORSO = Solid(surface(np.isin(dom, ["Hips", "Spine", "Spine1", "Spine2", "LeftUpLeg", "RightUpLeg"])))
HEADS = {}
def head(tilt, pitch):
    if (tilt, pitch) not in HEADS:
        p = J[ix["Head"]]; R = rot([0, 0, 1], tilt) @ rot([1, 0, 0], pitch)
        HEADS[tilt, pitch] = Solid((HEAD - p) @ R.T + p)
    return HEADS[tilt, pitch]

def check(side, dirv, bend, toward):
    """NikoRig.Arm: clavicle lift, upper arm along dirv, forearm bent toward `toward`, roll from the pole behind the elbow."""
    an = "Left" if side < 0 else "Right"   # the R arm (-X in Unity) is the FBX's Left arm
    c, s, e, h = (J[ix[an + n]] for n in ("Shoulder", "Arm", "ForeArm", "Hand"))
    want = nz(dirv); pole = nz([side * .35, -.2, -.85])
    Rc = rot([0, 0, 1], side * 6 * np.clip(want[1] + .35, 0, 1))
    s1, e1, h1 = c + Rc @ (s - c), c + Rc @ (e - c), c + Rc @ (h - c)
    fore = rot(np.cross(want, nz(toward)), bend) @ want
    Du = frame(want, pole) @ frame(e1 - s1, pole).T
    e2 = s1 + Du @ (e1 - s1); h2 = s1 + Du @ (h1 - s1)
    Df = frame(fore, pole) @ frame(h2 - e2, pole).T
    T = {}
    for i, n in enumerate(names):
        if n == an + "Shoulder": T[i] = (Rc, c, c)
        elif n == an + "Arm": T[i] = (Du @ Rc, s, s1)
        elif n.startswith(an + "ForeArm") or n.startswith(an + "Hand"): T[i] = (Df @ Du @ Rc, e, e2)
    sel = np.array([n.startswith(an + "Arm") or n.startswith(an + "ForeArm") or n.startswith(an + "Hand") for n in dom])
    P = V[sel]; out = np.zeros_like(P)
    for i in range(len(names)):
        w = W[sel, i]
        if w.any():
            R, p0, p = T.get(i, (np.eye(3), 0, 0)); out += w[:, None] * ((P - p0) @ R.T + p)
    upper = dom[sel] == an + "Arm"
    axis = (e2 - s1) / np.linalg.norm(e2 - s1)
    cap = upper & ((out - s1) @ axis < .5 * np.linalg.norm(e - s))   # shoulder cap and collarbone skin: under the jaw by build
    hd = max(int((head(*v).depths(out[~cap]) > DEPTH).sum()) for v in ((r, p) for r in (-8, 0, 8) for p in (-12, 0, 10)))
    td = int((TORSO.depths(out[~upper]) > DEPTH).sum())
    return hd, td, e2, e2 + fore * np.linalg.norm(h - e)

# NikoRig's poses in its own terms: V(side, outward, y, z); toward None = the default V(side, -.35, .2, .85); bends include the
# oscillation and talk-accent extremes (+6 on R, +4 on L). sides: "RL" both arms mirrored, "R" or "L" one arm.
UPW = (.35, 1, .25)
F2 = tuple(nz(nz((.45, -.4, .8)) + nz((.95, -.25, .2)))); OUT = (.95, -.25, .2); MID = tuple(nz(np.add(F2, OUT)))
POSES = [
    ("idle", "RL", (.8, -1, .15), [6, 12], None),
    ("Wave/Greet/Goodbye", "R", (1, -.1, .12), [35, 45, 55], UPW), ("RaiseHand", "R", (1, -.05, .15), [50], UPW),
    ("Present reach", "R", F2, [25], None), ("Present reach out", "R", OUT, [25], None), ("Present", "L", (.6, -.55, .4), [12], None),
    ("Point front", "RL", F2, [2], None), ("Point mid", "RL", MID, [2], None), ("Point out", "RL", OUT, [2], None),
    ("PointLeft/Right", "RL", (.95, -.25, .2), [2], None), ("Pinch", "R", F2, [40], None),
    ("Snap", "R", (.95, -.15, .3), [50], (.2, 1, .45)),
    ("Think cheek", "R", (1, -.25, .2), [62, 68], (-.1, 1, .3)), ("Think hip", "L", (.9, -.45, -.1), [40, 44], (-.2, -1, .1)),
    ("Surprise", "RL", (.95, -.2, .3), [54], UPW), ("Celebrate/Excited", "RL", (1, -.12, .15), [44, 50], UPW),
    ("Encourage", "R", (.65, -.4, .65), [52, 68], None), ("Listen", "RL", (.65, -.8, .35), [35], None),
    ("Explaining 1", "R", (.6, -.5, .6), [49, 67], None), ("Explaining 1", "L", (.64, -.5, .6), [49, 65], None),
    ("Explaining 2", "R", (.8, -.3, .45), [25, 41], None), ("Explaining 3", "L", (.66, -.5, .55), [39, 55], None),
    ("Excited talk", "R", (.85, -.25, .45), [30, 52], (.2, 1, .4)), ("Excited talk", "L", (.85, -.25, .45), [30, 50], (.2, 1, .4)),
    ("Question", "R", (.55, -.4, .7), [48, 62], None), ("Question", "L", (.66, -.4, .62), [64, 68], None),
    ("Encouraging", "R", (.5, -.45, .6), [56, 70], None), ("Encouraging", "L", (.6, -.5, .55), [50, 54], None),
    ("Normal", "R", (.55, -.6, .55), [30, 56], None), ("Normal", "L", (.6, -.6, .55), [30, 54], None),
]
failed = 0
for name, sides, d, bends, tw in POSES:
    for arm_name in sides:
        side = -1 if arm_name == "R" else 1
        Vs = lambda v: np.array([side * v[0], v[1], v[2]], float)
        for b in bends:
            hd, td, elbow, hand = check(side, Vs(d), b, Vs(tw if tw else (-.35, .2, .85)))
            bad = hd > 3 or td > 5; failed += bad
            print(f"{'FAIL' if bad else 'ok  '} {name:20s} {arm_name} bend {b:3d}  head {hd:4d}  torso {td:4d}  elbow {np.round(elbow, 3)}  wrist {np.round(hand, 3)}")
print(f"NIKO_ARM_CLEARANCE {'FAILED ' + str(failed) if failed else 'PASSED'}")
sys.exit(1 if failed else 0)

"""Bake the owner-supplied eye models (D:/AI unity project/models/eye/eye model/full eye.fbx and half eye.fbx) to
native Unity mesh data in the lesson's eye-radius frame: radius 1 = sclera, optical axis +Z, kept half x >= 0
(cut face at x = 0 looking toward -x). Run: blender --background --factory-startup --python <this file>
Writes Tools/HumanEye/LocalEye.json and the textures to Assets/HumanEye/ThirdParty/LocalEye/.
"""
import bpy, bmesh, json, pathlib, shutil
import numpy as np
from mathutils import Vector, Matrix

root = pathlib.Path(__file__).resolve().parents[2]
src = pathlib.Path(r"D:/AI unity project/models/eye/eye model")
out = root / "Assets/HumanEye/ThirdParty/LocalEye"
out.mkdir(parents=True, exist_ok=True)

def load(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(src / name))
    for im in bpy.data.images:          # embedded jpgs, written once under their original names
        if im.packed_file and im.filepath.lower().endswith(".jpg") and "Almirah" not in im.filepath:   # that one is a stray wardrobe texture on the lens
            dst = out / pathlib.Path(im.filepath.replace("\\", "/")).name
            if not dst.exists():
                dst.write_bytes(im.packed_file.data)
    return {o.name: o for o in bpy.data.objects if o.type == "MESH"}

def world_verts(o):
    return np.array([tuple(o.matrix_world @ v.co) for v in o.data.vertices])

def fit_sphere(p):
    A = np.c_[2 * p, np.ones(len(p))]
    b = (p ** 2).sum(1)
    c, *_ = np.linalg.lstsq(A, b, rcond=None)
    centre = c[:3]
    return centre, float(np.sqrt(c[3] + centre @ centre))

def globe_frame(globe, keep=lambda p, c, r: True):
    """Sphere through the sclera's outer surface (nerve and inner wall rejected iteratively)."""
    p = world_verts(globe)
    c, r = fit_sphere(p)
    for _ in range(6):
        d = np.linalg.norm(p - c, axis=1)
        sel = (d > r * .96) & (d < r * 1.04) & (p[:, 0] > c[0] - r * .9)
        c, r = fit_sphere(p[sel])
    return c, r

def to_lesson(c, r, cut_y=None):
    # source: optical axis +X, up +Z, cut face +Y (half eye). lesson: x = -(y - cy), y = z - cz, z = x - cx, / r
    cy = c[1] if cut_y is None else cut_y
    def f(v):
        return Vector((-(v[1] - cy) / r, (v[2] - c[2]) / r, (v[0] - c[0]) / r))
    def n(v):
        return Vector((-v[1], v[2], v[0])).normalized()
    return f, n

groups = {}   # key -> dict(part, layer, material, vertices, normals, uv, triangles)

def add(obj, part, layer, material_of, f, n, tri_filter=None, subdivide=0):
    me = obj.data
    if subdivide:
        bm = bmesh.new(); bm.from_mesh(me)
        bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=subdivide, use_grid_fill=True)
        bm.to_mesh(me); bm.free()
    me.calc_loop_triangles()
    mw = obj.matrix_world; nm = mw.to_3x3().inverted().transposed()
    uv = me.uv_layers.active.data
    cn = me.corner_normals
    for tri in me.loop_triangles:
        slot = obj.material_slots[tri.material_index].material.name if obj.material_slots else "none"
        pts = [f(mw @ me.vertices[me.loops[l].vertex_index].co) for l in tri.loops]
        uvs = [Vector(uv[l].uv) for l in tri.loops]
        p_name = part(pts, uvs) if callable(part) else part
        if p_name is None or (tri_filter and not tri_filter(pts)):
            continue
        mat = material_of(slot, p_name)
        key = f"{layer}/{p_name}/{mat}"
        g = groups.setdefault(key, dict(part=p_name, layer=layer, material=mat, vertices=[], normals=[], uv=[], triangles=[]))
        for l in reversed(tri.loops):    # the frame change is a reflection: reverse the winding for Unity
            v = f(mw @ me.vertices[me.loops[l].vertex_index].co)
            nn = n(nm @ cn[l].vector)
            g["vertices"].append(dict(x=v.x, y=v.y, z=v.z))
            g["normals"].append(dict(x=nn.x, y=nn.y, z=nn.z))
            g["uv"].append(dict(x=uv[l].uv[0], y=uv[l].uv[1]))
            g["triangles"].append(len(g["triangles"]))

report = {}

# ---------------- closed eye (exterior) ----------------
m = load("full eye.fbx")
c, r = globe_frame(m["Eye_Ball_Full"])
f, n = to_lesson(c, r)
report["full"] = dict(centre=c.tolist(), radius=r, cornea_apex=max(f(v).z for v in world_verts(m["Cornea1"])))
IRIS, PUPIL = .19, .073          # uv radii of the painted iris and pupil (Eye_Diff.jpg)
def globe_region(pts, uvs):
    z = sum(p.z for p in pts) / 3
    if z < -.75 and (sum(p.x for p in pts) / 3) ** 2 + (sum(p.y for p in pts) / 3) ** 2 < .2:
        return "optic_nerve"        # the stub behind the globe
    if z < .6:
        return "sclera"
    d = max((u - Vector((.5, .5))).length for u in uvs)
    return "pupil" if d < PUPIL * 1.05 else "iris" if d < IRIS * 1.03 else "sclera"
add(m["Eye_Ball_Full"], globe_region, "exterior", lambda s, p: "eye_outer", f, n, subdivide=1)
add(m["Cornea1"], "cornea", "exterior", lambda s, p: "cornea", f, n)
for k in ("Full_Muscle_01_1", "Full_Muscle_02", "Full_Muscle_03", "Full_Muscle_04"):
    add(m[k], "sclera", "exterior", lambda s, p: "muscle", f, n)

# ---------------- cross-section ----------------
m = load("half eye.fbx")
cut_y = max(world_verts(m["Eye_Ball_Full1"])[:, 1])
c, r = globe_frame(m["Eye_Ball_Full1"])
f, n = to_lesson(c, r, cut_y)
def behind(pts):
    q = sum(pts, Vector()) / 3
    return q.z < -.8 and q.length > 1.0
SEC = dict(Eye_Diff="eye_outer", Eye_inner_Diff="eye_inner", Muscle_Diff="muscle", Choroid_Dif="choroid", Retina_Diff="retina",
           Red_Vain="vessel_red", Blue_Vain="vessel_blue", Cornea="cornea", Aqueous_Humor="aqueous", Lense="lens", lambert2="lens",
           Pupil1="lens", Suspensery_Ligements="ligament")
mat = lambda s, p: SEC.get(s, s)
split_nerve = lambda base: (lambda pts, uvs: "optic_nerve" if behind(pts) else base)
add(m["Eye_Ball_Full1"], split_nerve("sclera"), "section", mat, f, n)
for k in ("Cut_Muscle_01", "Cut_Muscle_02", "Full_Muscle_01"):
    add(m[k], "sclera", "section", mat, f, n)
add(m["Choroid"], split_nerve("choroid"), "section", mat, f, n)
add(m["Retina1"], split_nerve("retina"), "section", mat, f, n)
add(m["Red_Vain5"], split_nerve("retina"), "section", mat, f, n)
add(m["Blue_Vain1"], split_nerve("retina"), "section", mat, f, n)
add(m["Cornea2"], "cornea", "section", mat, f, n)
add(m["Cornea3"], "aqueous", "section", mat, f, n)
add(m["Iris1"], "iris", "section", lambda s, p: "iris", f, n)
add(m["Pupil"], "pupil", "section", lambda s, p: "iris", f, n)
add(m["Lense1"], "lens", "section", mat, f, n)
add(m["Suspensery_Ligements1"], "lens", "section", mat, f, n)

def stats(key):
    v = np.array([(q["x"], q["y"], q["z"]) for q in groups[key]["vertices"]])
    return dict(min=v.min(0).round(3).tolist(), max=v.max(0).round(3).tolist(), mean=v.mean(0).round(3).tolist(), tris=len(v) // 3)
report["half"] = dict(centre=c.tolist(), radius=r, cut_y=cut_y)
report["groups"] = {k: stats(k) for k in groups}
import gzip
with gzip.open(root / "Tools/HumanEye/LocalEye.json.gz", "wt", encoding="utf-8") as f:
    f.write(json.dumps(dict(meshes=list(groups.values())), separators=(",", ":")))
(root / "Tools/HumanEye/LocalEye_report.json").write_text(json.dumps(report, indent=1))
print("LOCAL_EYE_EXPORTED", {k: v["tris"] for k, v in report["groups"].items()})

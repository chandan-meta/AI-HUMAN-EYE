"""Rebuild the Rack 02 muscle maps at full resolution (14 October 2026): the 13 October atlas packed the fourteen 1024 px
tiles into 2048 px (504 px each) and dropped the supplied normal maps, which flattened the fibre detail. This writes a
4096 px albedo atlas and a matching normal atlas with the SAME tile layout and edge ratio (1008/1024 = 504/512), so the
saved fitted mesh's UVs stay valid. The existing 2048 atlas is used to verify the tile order before anything is written.
  "C:/Program Files/Blender Foundation/Blender 4.5/blender.exe" --background --factory-startup --python Tools/HumanEye/export_rack_muscle_maps.py"""
import bpy, pathlib, json, numpy as np

root = pathlib.Path(__file__).resolve().parents[2]
out = root / "Assets/HumanEye/ThirdParty/UserModels/RackAnatomy"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:/Users/ADMIN/Documents/Received Files/Rack 02.fbx")
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH" and (o.name == "skeleton" or o.name.startswith("Object_"))]
assert len(meshes) == 14, len(meshes)
grid, tile, edge = 4, 1024, 8; size = grid * tile

def image_of(o, socket):
    bs = next(n for n in o.data.materials[0].node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    links = bs.inputs[socket].links
    if not links: return None
    n = links[0].from_node
    if n.type == "NORMAL_MAP": n = n.inputs["Color"].links[0].from_node
    return getattr(n, "image", None)

def atlas(socket, fill):
    a = np.zeros((size, size, 4), np.float32); a[:, :, :3] = fill; a[:, :, 3] = 1
    for index, o in enumerate(meshes):
        img = image_of(o, socket)
        if img is None: continue
        copy = img.copy(); copy.scale(tile - 2 * edge, tile - 2 * edge)
        px = np.empty((tile - 2 * edge) ** 2 * 4, np.float32); copy.pixels.foreach_get(px)
        px = np.pad(px.reshape((tile - 2 * edge, tile - 2 * edge, 4)), ((edge, edge), (edge, edge), (0, 0)), mode="edge")
        x, y = index % grid, index // grid; a[y * tile:(y + 1) * tile, x * tile:(x + 1) * tile] = px
        bpy.data.images.remove(copy)
    return a

albedo = atlas("Base Color", 0.5)
# Verify the tile order against the 2048 atlas the fitted mesh was mapped to (per-tile mean colour).
old = bpy.data.images.load(str(out / "Muscles_Albedo.png")); op = np.empty(2048 * 2048 * 4, np.float32); old.pixels.foreach_get(op); op = op.reshape((2048, 2048, 4))
worst = 0
for index in range(len(meshes)):
    x, y = index % grid, index // grid
    a = albedo[y * tile:(y + 1) * tile, x * tile:(x + 1) * tile, :3].mean((0, 1)); b = op[y * 512:(y + 1) * 512, x * 512:(x + 1) * 512, :3].mean((0, 1))
    worst = max(worst, float(np.abs(a - b).max()))
assert worst < .03, f"tile order differs from the saved atlas: {worst}"
normal = atlas("Normal", (0.5, 0.5, 1.0))
for name, data in (("Muscles_Albedo", albedo), ("Muscles_Normal", normal)):
    img = bpy.data.images.new(name, width=size, height=size, alpha=False)
    img.pixels.foreach_set(data.ravel()); img.file_format = "PNG"; img.filepath_raw = str(out / (name + ".png")); img.save()
report = dict(tiles=len(meshes), atlas=size, tile=tile, edge=edge, verified_against_2048_max_mean_diff=worst)
(out / "Muscles_MapsReport.json").write_text(json.dumps(report, indent=2))
print("RACK_MAPS " + json.dumps(report))

# Blender CLI: extract ONE microscope from a scratch copy of the Brain ENV room FBX, join, ground at origin,
# export a project-ready FBX (Y-up for Unity) and a preview render. Never touches the source package.
import bpy, sys, os, math
from mathutils import Vector

src, out_fbx, out_png = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)

parts = [o for o in bpy.context.scene.objects if o.type == 'MESH' and "_LowPoly__0" in o.name and not o.name.endswith(".001")]
print("MICROSCOPE_PARTS", len(parts), sorted(o.name for o in parts))
for o in list(bpy.context.scene.objects):
    if o not in parts: bpy.data.objects.remove(o, do_unlink=True)

# Bake each part's world transform into its mesh (the parts stand upright in world space), then join.
for o in parts:
    mw = o.matrix_world.copy()
    o.data = o.data.copy()
    o.data.transform(mw)
    o.parent = None
    o.matrix_world.identity()
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
m = bpy.context.view_layer.objects.active
m.name = "Microscope"
m.data.update()

# Ground the base at the origin, centred in X/Y.
pts = [v.co.copy() for v in m.data.vertices]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
offset = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v in m.data.vertices:
    v.co -= offset
m.location = (0, 0, 0)
size = mx - mn
k = .45 / size.z
for v in m.data.vertices: v.co *= k
size *= k
m.data.update()
print("MICROSCOPE_SIZE_M", round(size.x, 3), round(size.y, 3), round(size.z, 3), "TRIS", sum(len(p.vertices) - 2 for p in m.data.polygons))
print("MATERIALS", [s.material.name for s in m.material_slots if s.material])

bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         axis_forward='-Z', axis_up='Y', path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)

# Preview (workbench, textured) to judge quality before integrating.
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.color_type = 'TEXTURE'
scene.display.shading.light = 'STUDIO'
scene.render.resolution_x, scene.render.resolution_y = 640, 640
cam_data = bpy.data.cameras.new("cam"); cam = bpy.data.objects.new("cam", cam_data); scene.collection.objects.link(cam)
h = max(size)
cam.location = (h * 1.6, -h * 1.6, h * 1.1)
direction = Vector((0, 0, size.z * .45)) - cam.location
cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam
scene.render.filepath = out_png
bpy.ops.render.render(write_still=True)
print("DONE")

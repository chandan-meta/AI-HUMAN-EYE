"""Read-only source FBX audit and neutral front/side renders (Blender --background --python)."""
import bpy, json, pathlib, sys
from mathutils import Vector

out = pathlib.Path(__file__).resolve().parents[2] / "Logs/RackAudit"
out.mkdir(parents=True, exist_ok=True)
names = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ("Rack 01", "Rack 02")
for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source = pathlib.Path(r"C:/Users/ADMIN/Documents/Received Files") / (name + ".fbx")
    bpy.ops.import_scene.fbx(filepath=str(source))
    objects = []
    points = []
    for o in bpy.context.scene.objects:
        record = dict(name=o.name, type=o.type, parent=o.parent.name if o.parent else None,
                      location=list(o.location), rotation=list(o.rotation_euler), scale=list(o.scale))
        if o.type == "MESH":
            ps = [o.matrix_world @ v.co for v in o.data.vertices]
            points += ps
            record.update(vertices=len(ps), triangles=sum(len(p.vertices)-2 for p in o.data.polygons),
                          materials=[m.name if m else None for m in o.data.materials], uv=len(o.data.uv_layers),
                          minimum=[min(p[i] for p in ps) for i in range(3)], maximum=[max(p[i] for p in ps) for i in range(3)])
        if o.type == "ARMATURE": record["bones"] = [b.name for b in o.data.bones]
        objects.append(record)
    lo = Vector([min(p[i] for p in points) for i in range(3)])
    hi = Vector([max(p[i] for p in points) for i in range(3)])
    textures = [dict(name=i.name, path=i.filepath, packed=bool(i.packed_file), size=list(i.size)) for i in bpy.data.images]
    report = dict(source=str(source), objects=objects, minimum=list(lo), maximum=list(hi), textures=textures)
    (out / (name + ".json")).write_text(json.dumps(report, indent=2))
    print(name, "AUDIT", json.dumps(dict(meshes=len([o for o in objects if o['type']=='MESH']), bounds=[list(lo),list(hi)], textures=textures)))
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"; scene.cycles.samples = 16
    scene.render.resolution_x = 720; scene.render.resolution_y = 900; scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new("Audit World"); scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.12,.14,.17,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .6
    center = (lo + hi) / 2; height = max(hi-lo)
    for offset, energy, size in [((2,-3,3), 1400, 3),((-2,-1,1),600,3),((0,2,2),900,2)]:
        data = bpy.data.lights.new("Audit Light", "AREA"); data.energy=energy*(height/2)**2; data.shape='DISK'; data.size=size*height/2
        light = bpy.data.objects.new(data.name, data); scene.collection.objects.link(light)
        light.location=center+Vector(offset)*height/2; light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    cam_data=bpy.data.cameras.new("Audit Camera"); cam=bpy.data.objects.new("Audit Camera",cam_data); scene.collection.objects.link(cam); scene.camera=cam
    cam_data.type='ORTHO'; cam_data.ortho_scale=height*1.16; cam_data.clip_end=10000
    for label, direction in [('front',(0,-1,0)),('side',(1,0,0))]:
        cam.location=center+Vector(direction)*height*2; cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/(name+'-'+label+'.png')); bpy.ops.render.render(write_still=True)

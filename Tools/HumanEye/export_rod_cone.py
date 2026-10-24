"""Export the owner's rod and cone (models/rod cone .blend, 16 October 2026) as the Scene 8 photoreceptors.
  "C:/Program Files/Blender Foundation/Blender 5.1/blender.exe" --background "D:/AI unity project/models/rod cone .blend" ^
      --python Tools/HumanEye/export_rod_cone.py [-- --preview <dir>]
Each cell (ROD, CONE: about 1 m tall along +Z, outer segment at the top, synaptic terminal feet at z = 0) is two surfaces in
the .blend: the textured cell and a slightly larger see-through 'glass' membrane (Material.001, alpha 0.16) over it. Only
the textured surface is exported (the module's Anatomy shader gives the glassy rim; a constant-alpha shell would fight the
lesson's fades). Per cell: world transform applied, axis on x = y = 0, feet on z = 0, decimated (collapse) for the
twenty-cell field, then split at the connecting cilium (the narrowest point between the inner and outer segments) into
<Cell>_Outer (the disc stack the lesson lights up) and <Cell>_Body. Empties mark the four labelled parts on the axis
(Mark_OuterSegment, Mark_InnerSegment, Mark_Nucleus, Mark_SynapticTerminal). Textures: the packed albedo JPEGs as they
are, plus a greyscale copy of the cone map (2048) for the three tinted cone classes. Metres; the Unity patch scales and
turns the cells (Builder.ApplyRodConeModels)."""
import bpy, bmesh, json, math, pathlib, sys, numpy as np
from mathutils import Matrix, Vector

root = pathlib.Path(__file__).resolve().parents[2]
out = root / 'Assets/HumanEye/ThirdParty/UserModels/RodCone'; out.mkdir(parents=True, exist_ok=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
preview = pathlib.Path(argv[argv.index('--preview') + 1]) if '--preview' in argv else None
assert pathlib.Path(bpy.data.filepath).name.lower().startswith('rod cone'), 'open "rod cone .blend" on the command line'
TRIANGLES = 7000   # per cell after decimation (twenty cells in the field)
report = dict(source=bpy.data.filepath, frame='metres, cell axis on x = y = 0, synaptic terminal feet at z = 0, outer segment toward +Z', cells={})

def textured_slot(o):
    for i, m in enumerate(o.data.materials):
        if m and m.use_nodes and any(n.type == 'TEX_IMAGE' and n.image for n in m.node_tree.nodes): return i
    raise AssertionError(f'{o.name}: no textured material')

def albedo(o):
    m = o.data.materials[textured_slot(o)]
    return next(n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image)

def pts(o):
    co = np.empty(len(o.data.vertices) * 3, np.float32); o.data.vertices.foreach_get('co', co); return co.reshape((-1, 3))

def profile(o, z0, z1, step=.005):
    """Largest distance from the axis per height band [z, z + step) between z0 and z1."""
    p = pts(o); r = np.hypot(p[:, 0], p[:, 1]); bands = []
    z = z0
    while z < z1:
        sel = (p[:, 2] >= z) & (p[:, 2] < z + step)
        bands.append((z + step / 2, float(r[sel].max()) if sel.any() else 0.0)); z += step
    return bands

def prepare(o, name):
    # textured surface only, transform applied, axis centred, feet on z = 0
    keep = textured_slot(o)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != keep], context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(o.data); bm.free()
    mat = o.data.materials[keep]; o.data.materials.clear(); o.data.materials.append(mat)
    for p in o.data.polygons: p.material_index = 0; p.use_smooth = True
    o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4)
    p = pts(o)
    o.data.transform(Matrix.Translation(Vector((-(p[:, 0].min() + p[:, 0].max()) / 2, -(p[:, 1].min() + p[:, 1].max()) / 2, -p[:, 2].min()))))
    tris = sum(len(f.vertices) - 2 for f in o.data.polygons)
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    if tris > TRIANGLES:
        d = o.modifiers.new('Decimate', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.ratio = TRIANGLES / tris; d.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=d.name)
    o.name = name; o.data.name = name
    return tris

def split(o, name):
    """The connecting cilium: the narrowest band between the inner segment's widest point and the outer segment."""
    top = float(pts(o)[:, 2].max())
    bands = [b for b in profile(o, .5 * top, .75 * top) if b[1] > 0]
    cut = min(bands, key=lambda b: b[1])[0]
    bm = bmesh.new(); bm.from_mesh(o.data)
    outer_faces = [f for f in bm.faces if f.calc_center_median().z > cut]
    outer = bpy.data.meshes.new(name + '_Outer'); body = bpy.data.meshes.new(name + '_Body')
    for mesh, faces in ((outer, outer_faces), (body, [f for f in bm.faces if f.calc_center_median().z <= cut])):
        part = bm.copy(); ids = {f.index for f in faces}
        part.faces.ensure_lookup_table()
        bmesh.ops.delete(part, geom=[f for f in part.faces if f.index not in ids], context='FACES')
        bmesh.ops.delete(part, geom=[v for v in part.verts if not v.link_faces], context='VERTS')
        part.to_mesh(mesh); part.free()
        mesh.materials.append(o.data.materials[0])
    bm.free()
    objs = []
    for mesh in (outer, body):
        x = bpy.data.objects.new(mesh.name, mesh); bpy.context.scene.collection.objects.link(x); objs.append(x)
    return objs, cut, top

def marks(o, cut, top):
    """Heights of the four labelled parts: the disc stack's middle, the inner segment's and nucleus's widest bands, the feet."""
    def widest(z0, z1): return max(profile(o, z0, z1), key=lambda b: b[1])[0]
    nucleus = widest(.18 * top, .37 * top)
    return {'Mark_OuterSegment': (cut + top) / 2, 'Mark_InnerSegment': widest(.4 * top, cut - .02 * top),
            'Mark_Nucleus': nucleus, 'Mark_SynapticTerminal': widest(0, .1 * top)}

def export(objs, heights, path):
    empties = []
    for n, z in heights.items():
        e = bpy.data.objects.new(n, None); e.location = (0, 0, z); e.empty_display_size = .02
        bpy.context.scene.collection.objects.link(e); empties.append(e)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs + empties: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE',
        use_mesh_modifiers=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False, bake_anim=False)
    for e in empties: bpy.data.objects.remove(e, do_unlink=True)

O = bpy.data.objects
cells = {'Rod': O['ROD'], 'Cone': O['CONE']}
for name, o in cells.items():
    img = albedo(o)
    assert img.packed_file, f'{img.name}: expected a packed albedo'
    (out / f'{name}_Albedo.jpg').write_bytes(img.packed_file.data)   # the supplied JPEG, unchanged
    if name == 'Cone':   # the three cone classes tint a grey copy (their disc detail kept)
        grey = img.copy(); grey.scale(2048, 2048); px = np.empty(2048 * 2048 * 4, np.float32); grey.pixels.foreach_get(px); px = px.reshape((-1, 4))
        lum = np.clip((px[:, :3] @ np.array([.2126, .7152, .0722], np.float32)) * 1.25, 0, 1); px[:, 0] = px[:, 1] = px[:, 2] = lum
        g = bpy.data.images.new('Cone_AlbedoGrey', 2048, 2048); g.pixels.foreach_set(px.ravel())
        g.file_format = 'JPEG'; g.filepath_raw = str(out / 'Cone_AlbedoGrey.jpg'); g.save()
    source_tris = prepare(o, name)
    parts, cut, top = split(o, name)
    heights = marks(o, cut, top)
    for p in parts: p.data.materials[0].name = name + '_Cell'
    export(parts, heights, out / f'{name}Cell.fbx')
    report['cells'][name] = dict(source_triangles=source_tris, triangles={p.name: sum(len(f.vertices) - 2 for f in p.data.polygons) for p in parts},
                                 height=round(top, 4), cilium=round(cut, 4), marks={k: round(v, 4) for k, v in heights.items()},
                                 width=round(float(2 * np.hypot(pts(o)[:, 0], pts(o)[:, 1]).max()), 4), albedo=img.name)
    if preview:
        preview.mkdir(parents=True, exist_ok=True)
        S = bpy.context.scene; S.render.engine = 'BLENDER_WORKBENCH'; S.display.shading.light = 'STUDIO'; S.display.shading.color_type = 'TEXTURE'
        S.render.resolution_x, S.render.resolution_y = 700, 1000
        for x in O:
            if x.type == 'MESH': x.hide_render = x not in parts
        cam = O.get('P') or bpy.data.objects.new('P', bpy.data.cameras.new('P'))
        if cam.name not in S.collection.objects: S.collection.objects.link(cam)
        cam.location = (0, -2.6, top / 2); cam.rotation_euler = (math.radians(90), 0, 0); S.camera = cam
        S.render.filepath = str(preview / f'{name}_exported.png'); bpy.ops.render.render(write_still=True)
(out / 'ImportReport.json').write_text(json.dumps(report, indent=2))
print('RODCONE_EXPORTED', json.dumps(report))

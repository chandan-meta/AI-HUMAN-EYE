"""Export the owner's ANATOMY.blend (15 October 2026) as the module's anatomy layers: skin (+ shorts, eyelashes, a Blink
blend shape), muscles, skeleton, internal organs and vessels, every system in ONE common frame (metres, feet at 0,
facing -Y in Blender = -Z in Unity) with the registration empties the existing Builder.RackLayer fit expects
(EyeSocket_L/R, FaceDirection, BodyBase, BodyTop) plus EyeFront_L/R (cornea apex: socket radius) and BrainMin/Max.
  "C:/Program Files/Blender Foundation/Blender 5.1/blender.exe" --background "D:/AI unity project/models/ANATOMY.blend" ^
      --python Tools/HumanEye/export_anatomy_blend.py [-- --preview <dir>]
The .blend arranges the systems side by side along X by their parent empties; the systems themselves share one origin,
so each mesh is taken back by its own column's parent translation (the heart and eyelashes sit under differently scaled
empties and borrow their column siblings'). Breathing morph targets (target_0/1/2) are dropped at the Basis. No decimation.
Textures: albedo JPEG at source size (skin: RGBA PNG carrying the eyelash opacity), normal PNG (DirectX maps have G
flipped for Unity), roughness inverted to a specular mask (<= 2048). The .blend's brain and eyeballs are measured, not
exported: Rack 06 stays the brain (more detailed, lobe-split for the lesson) and the lesson's cross-section eye sits in
the sockets."""
import bpy, json, math, pathlib, sys, numpy as np
from mathutils import Vector, Matrix

root = pathlib.Path(__file__).resolve().parents[2]
out = root / 'Assets/HumanEye/ThirdParty/UserModels/Anatomy'; out.mkdir(parents=True, exist_ok=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
preview = pathlib.Path(argv[argv.index('--preview') + 1]) if '--preview' in argv else None
assert bpy.data.filepath.lower().endswith('anatomy.blend'), 'open ANATOMY.blend on the command line'
O = bpy.data.objects
SCALE = .01   # the file is centimetres in a metre scene (the figure is 170 units tall)

# ---- identify the systems by their parent empties and materials, not by the numeric mesh names
def by_parent(parent): return [o for o in O if o.type == 'MESH' and o.parent and o.parent.name == parent]
def one(parent):
    m = by_parent(parent); assert len(m) == 1, (parent, [o.name for o in m]); return m[0]
systems = {
    'Skin': [one('HumanSkin'), one('Eyelash')],
    'Muscles': [one('Musclespart_1'), one('Musclespart_2'), one('Musclespart_3')],
    'Skeleton': [one('Skull'), one('Spine'), one('Ribcage'), one('Pelvis'), one('Legs'), one('Hands')],
    'Organs': [one('Humanlungs'), one('Diafragma'), one('Heart'), one('Liver'), one('Gallbladder'), one('Digestivesystem'), one('Urinary_system')],
    'Vessels': [one('Arteriasmesh'), one('Venasmesh')],
}
shorts, eye_mesh, brain_mesh = one('Shorts'), one('Eye'), one('Brain')
assert eye_mesh.data.materials[0].name == 'Eye' and brain_mesh.data.materials[0].name == 'Brain'
column_of = {'Heart': 'Arteriasmesh', 'Eyelash': 'HumanSkin'}   # scaled empties: not the column origin
def origin(o):
    p = O[column_of.get(o.parent.name, o.parent.name)]
    return Vector(p.matrix_world.translation)

# ---- every mesh into the common frame: Basis shape, parent offset removed, centimetres -> metres
meshes = [o for o in O if o.type == 'MESH']
for o in meshes:
    mw = o.matrix_world.copy()
    if o.data.shape_keys: o.shape_key_clear()   # the breathing targets; the vertices hold the Basis
    o.data.transform(Matrix.Scale(SCALE, 4) @ Matrix.Translation(-origin(o)) @ mw)
    o.parent = None; o.matrix_world = Matrix.Identity(4)
for o in [o for o in O if o.type != 'MESH']: bpy.data.objects.remove(o, do_unlink=True)   # the column empties (their names are reused below)
def pts(o):
    co = np.empty(len(o.data.vertices) * 3, np.float32); o.data.vertices.foreach_get('co', co); return co.reshape((-1, 3))
skin = systems['Skin'][0]; sp = pts(skin)
# feet on z = 0, the skin's bounding-box centre on x = y = 0 (the Unity patch re-centres the skin instance the same way)
shift = Vector(((sp[:, 0].min() + sp[:, 0].max()) / 2, (sp[:, 1].min() + sp[:, 1].max()) / 2, sp[:, 2].min()))
for o in meshes: o.data.transform(Matrix.Translation(-shift))
sp = pts(skin); top = float(sp[:, 2].max())

# ---- eyeballs: sphere fits per side (the person's left is +X: the figure faces -Y) and the cornea apex in front
ep = pts(eye_mesh); mid_x = float((ep[:, 0].min() + ep[:, 0].max()) / 2)
def sphere(q):
    A = np.c_[2 * q, np.ones(len(q))]; b = (q ** 2).sum(1); c, *_ = np.linalg.lstsq(A, b, rcond=None)
    centre = c[:3]; r = math.sqrt(c[3] + (centre ** 2).sum()); return centre, r
eyes = {}
for side, sel in (('L', ep[:, 0] > mid_x), ('R', ep[:, 0] < mid_x)):
    q = ep[sel]; c, r = sphere(q); d = np.linalg.norm(q - c, axis=1); c, r = sphere(q[d > np.percentile(d, 60)])   # outer shell
    apex = float(c[1] - q[:, 1].min())   # the cornea apex is the front-most point along -Y
    eyes[side] = dict(centre=[float(x) for x in c], shell_radius=float(r), apex=apex, socket_radius=apex / 1.126)   # the lesson eye's apex is 1.126 radii out
eye_mid = (Vector(eyes['L']['centre']) + Vector(eyes['R']['centre'])) / 2
bp = pts(brain_mesh)
landmarks = {'EyeSocket_L': eyes['L']['centre'], 'EyeSocket_R': eyes['R']['centre'],
             'EyeFront_L': [eyes['L']['centre'][0], eyes['L']['centre'][1] - eyes['L']['apex'], eyes['L']['centre'][2]],
             'EyeFront_R': [eyes['R']['centre'][0], eyes['R']['centre'][1] - eyes['R']['apex'], eyes['R']['centre'][2]],
             'FaceDirection': [eye_mid.x, eye_mid.y - 1, eye_mid.z], 'BodyBase': [0, 0, 0], 'BodyTop': [0, 0, top],
             'BrainMin': bp.min(0).tolist(), 'BrainMax': bp.max(0).tolist()}

# ---- textures: classify each material's image nodes by what they feed
tex_report = {}
def rename(m):
    n = m.name.strip('_').replace('_Mat', '').replace('Urinary_System', 'Urinary').replace('CirculatorySystem', 'Vessels').replace('DigestiveSystem', 'Digestive')
    m.name = n; return n
def save_image(img, path, quality=None):
    img.pixels[0]   # images load lazily: touch the buffer before changing the format
    img.file_format = 'JPEG' if path.suffix == '.jpg' else 'PNG'; img.filepath_raw = str(path)
    if quality:
        try: img.save(quality=quality)
        except TypeError: img.save()
    else: img.save()
def new_image(name, w, h, px, alpha=False, noncolor=False):
    img = bpy.data.images.new(name, width=w, height=h, alpha=alpha)
    if noncolor: img.colorspace_settings.name = 'Non-Color'
    img.pixels.foreach_set(px.ravel()); return img
def pixels(img):
    img.pixels[0]; w, h = img.size; px = np.empty(w * h * 4, np.float32); img.pixels.foreach_get(px); return px.reshape((h, w, 4))
def maps(m):
    found = {}
    for n in m.node_tree.nodes:
        if n.type != 'TEX_IMAGE' or not n.image or not n.outputs['Color'].links: continue
        link = n.outputs['Color'].links[0]; to = link.to_node.type, link.to_socket.name
        if to == ('BSDF_PRINCIPLED', 'Base Color'): found['albedo'] = n.image
        elif to == ('BSDF_PRINCIPLED', 'Roughness'): found['roughness'] = n.image
        elif to == ('BSDF_PRINCIPLED', 'Alpha'): found['opacity'] = n.image
        elif to == ('NORMAL_MAP', 'Color'): found['normal'] = (n.image, False)
        elif to == ('SEPARATE_COLOR', 'Color'): found['normal'] = (n.image, True)   # DirectX (-Y) map: the tree flips G
    return found
def export_textures(m, with_alpha=None):
    name = rename(m); f = maps(m); written = {}
    albedo = f['albedo']
    if with_alpha is not None:
        px = pixels(albedo); px[:, :, 3] = pixels(with_alpha)[:, :, 0]
        img = new_image(name + '_Albedo', *albedo.size, px, alpha=True); save_image(img, out / (name + '_Albedo.png')); written['albedo'] = name + '_Albedo.png'
    else:
        save_image(albedo, out / (name + '_Albedo.jpg'), 92); written['albedo'] = name + '_Albedo.jpg'
    if 'normal' in f:
        img, directx = f['normal']
        if directx:
            px = pixels(img); px[:, :, 1] = 1 - px[:, :, 1]; img = new_image(name + '_Normal', *img.size, px, noncolor=True)
        save_image(img, out / (name + '_Normal.png')); written['normal'] = name + '_Normal.png'; written['normal_was_directx'] = directx
    if 'roughness' in f:
        img = f['roughness'].copy(); img.scale(min(img.size[0], 2048), min(img.size[1], 2048))
        px = pixels(img); px[:, :, :3] = 1 - px[:, :, :3]; spec = new_image(name + '_Spec', *img.size, px, noncolor=True)
        save_image(spec, out / (name + '_Spec.jpg'), 90); written['spec'] = name + '_Spec.jpg'; bpy.data.images.remove(img)
    tex_report[name] = written; return name

# ---- the eyelashes carry the only opacity: keep them in the skin mesh under alpha clip if no skin uv shares that region
opacity = maps(skin.data.materials[0]).get('opacity'); lash = systems['Skin'][1]
op = pixels(opacity)[:, :, 0]; h_, w_ = op.shape
def opacity_at(o):
    uv = np.empty(len(o.data.loops) * 2, np.float32); o.data.uv_layers.active.data.foreach_get('uv', uv); uv = uv.reshape((-1, 2))
    return op[np.clip((uv[:, 1] % 1 * h_).astype(int), 0, h_ - 1), np.clip((uv[:, 0] % 1 * w_).astype(int), 0, w_ - 1)]
skin_in_dark = int((opacity_at(skin) < .5).sum()); lash_in_dark = int((opacity_at(lash) < .5).sum())
keep_lashes = skin_in_dark == 0 and lash_in_dark > 0
if not keep_lashes: systems['Skin'] = [skin]; bpy.data.objects.remove(lash, do_unlink=True)

# ---- materials and textures per system (shared materials are exported once)
done = set(); slots = {}
for name, objs in list(systems.items()) + [('Shorts', [shorts])]:
    for o in objs:
        for m in o.data.materials:
            if m.name in done: continue
            done.add(m.name)
            export_textures(m, with_alpha=opacity if (o is skin and keep_lashes) else None)

# ---- join each system into one object (material slots kept), landmarks, export
def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1: bpy.ops.object.join()
    result = bpy.context.view_layer.objects.active; result.name = name
    for p in result.data.polygons: p.use_smooth = True
    return result
def add_landmarks():
    made = []
    for n, p in landmarks.items():
        e = bpy.data.objects.new(n, None); e.location = p; e.empty_display_size = .01
        bpy.context.scene.collection.objects.link(e); made.append(e)
    return made
def export(objs, path):
    marks = add_landmarks()
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs + marks: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE',
        use_mesh_modifiers=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False, bake_anim=False)
    for e in marks: bpy.data.objects.remove(e, do_unlink=True)
report = dict(source=bpy.data.filepath, scale=SCALE, frame='metres, feet at z=0 (Unity y), skin bbox centre at x=y=0, faces -Y (Unity -Z)',
              eyes=eyes, landmarks=landmarks, lashes_kept=keep_lashes, skin_uv_loops_in_opacity_region=skin_in_dark, systems={}, textures=tex_report,
              not_exported=dict(brain=f'{brain_mesh.name}: {len(brain_mesh.data.polygons)} polygons; Rack 06 (77,882 triangles, lobe-split) stays the module brain',
                                eye=f'{eye_mesh.name}: measured for the sockets; the lesson cross-section eye sits in them'))
tris = lambda o: sum(len(p.vertices) - 2 for p in o.data.polygons)

body = join(systems['Skin'], 'Body')
# ---- Blink: the upper lid drops to the lower margin (its skin compressing to 30 % as it folds, the brow staying), the
#      lower lid rises a little, and lid vertices keep their distance from the eye centre so the lid wraps the globe.
#      Heights are in eye radii; the margins are measured from the skin in front of the globe. Lashes (joined) ride along.
co = pts(body); basis = body.shape_key_add(name='Basis', from_mix=False); blink = body.shape_key_add(name='Blink', from_mix=False)
moved = 0
def smooth(t): t = min(1, max(0, t)); return t * t * (3 - 2 * t)
for side in ('L', 'R'):
    c = np.array(eyes[side]['centre']); r = eyes[side]['socket_radius']
    d = (co - c) / r; dist = np.linalg.norm(d, axis=1); lateral = np.abs(d[:, 0]); dy = d[:, 1]; dz = d[:, 2]
    # proportions of the globe: the lid band (0.05..0.9 r above the centre) closes onto -0.42 r, just over the lower
    # margin; above 0.9 r the travel fades out by 2 r (the brow); the lower lid rises 0.12 r.
    region = (dy < .2) & (lateral < 1.35) & (dist < 2.6) & (dz > -.9) & (dz < 2.1)
    for i in np.nonzero(region)[0]:
        wl = 1 - smooth((lateral[i] - .85) / .5)
        if dz[i] >= -.05: disp = (-.42 + (dz[i] - .05) * .3 - dz[i]) * (1 - smooth((dz[i] - .75) / .95)) * wl
        elif dz[i] <= -.2: disp = .12 * (1 - smooth((-.2 - dz[i]) / .7)) * wl
        else: disp = (-.42 - dz[i]) * wl * .5
        if abs(disp) < .004: continue
        p = d[i].copy(); p[2] += disp
        keep = 1 - smooth((dist[i] - 1.2) / .5)                    # lid vertices stay on their own sphere around the eye
        p = p * (1 - keep) + p * (dist[i] / np.linalg.norm(p)) * keep
        blink.data[i].co = Vector(c + p * r); moved += 1
assert moved > 60, f'blink moved only {moved} vertices'
report['blink'] = dict(moved_vertices=moved, rule='lid band 0.05..0.9 r -> -0.42 r (30 % compression), brow fade to 1.7 r, lower lid +0.12 r')
shorts.name = 'Shorts'
export([body, shorts], out / 'Skin.fbx')
report['systems']['Skin'] = dict(objects=['Body', 'Shorts'], triangles=tris(body) + tris(shorts), vertices=len(body.data.vertices) + len(shorts.data.vertices),
                                 materials=[[m.name for m in body.data.materials], [m.name for m in shorts.data.materials]])
if preview:
    preview.mkdir(parents=True, exist_ok=True)
    S = bpy.context.scene; S.render.engine = 'BLENDER_WORKBENCH'; S.display.shading.light = 'STUDIO'; S.display.shading.color_type = 'TEXTURE'
    S.render.resolution_x = 1400; S.render.resolution_y = 800; S.render.image_settings.file_format = 'PNG'; S.view_settings.view_transform = 'Standard'
    cam_data = bpy.data.cameras.new('P'); cam_data.type = 'ORTHO'; cam_data.ortho_scale = .14; cam_data.clip_end = 100
    cam = bpy.data.objects.new('P', cam_data); S.collection.objects.link(cam); S.camera = cam
    cam.location = (eye_mid.x, eye_mid.y - 2, eye_mid.z); cam.rotation_euler = (math.radians(90), 0, 0)
    for o in O:
        if o.type == 'MESH': o.hide_render = o not in (body, eye_mesh)
    for value, tag in ((0, 'open'), (.5, 'half'), (1, 'closed')):
        blink.value = value; S.render.filepath = str(preview / f'blink_{tag}.png'); bpy.ops.render.render(write_still=True)
    blink.value = 0
for name in ('Muscles', 'Skeleton', 'Organs', 'Vessels'):
    o = join(systems[name], name)
    export([o], out / (name + '.fbx'))
    report['systems'][name] = dict(objects=[name], triangles=tris(o), vertices=len(o.data.vertices), materials=[m.name for m in o.data.materials])
(out / 'ImportReport.json').write_text(json.dumps(report, indent=2))
print('ANATOMY_EXPORTED', json.dumps(report))

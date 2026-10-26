"""Blender preview on the project's exported retina UVs, with unchanged vessel meshes."""
import bpy, gzip, json, math
from pathlib import Path
from mathutils import Vector
root = Path(__file__).resolve().parent
project = root.parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
data = json.load(gzip.open(project / 'Tools/HumanEye/LocalEye.json.gz'))
def material(name, color):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1)
    return m
wall=material('Retina candidate',(.8,.4,.3))
shader=wall.node_tree.nodes.get('Principled BSDF'); shader.inputs['Roughness'].default_value=.62
tex=wall.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=bpy.data.images.load(str(root/'EyeInterior_BaseColor.png'))
wall.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
red=material('Original vessel red',(.55,.025,.02)); blue=material('Original vessel blue',(.035,.08,.55))
for item in data['meshes']:
    if item['part']!='retina': continue
    me=bpy.data.meshes.new(item['material'])
    verts=[(v['x'],v['y'],v['z']) for v in item['vertices']]
    tri=item['triangles']; me.from_pydata(verts,[],[tri[i:i+3] for i in range(0,len(tri),3)])
    uv=me.uv_layers.new()
    for loop in me.loops:
        t=item['uv'][loop.vertex_index]; uv.data[loop.index].uv=(t['x'],t['y'])
    ob=bpy.data.objects.new(me.name,me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(red if item['material']=='vessel_red' else blue if item['material']=='vessel_blue' else wall)
    for p in me.polygons:p.use_smooth=True
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=1200; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.color=(.15,.15,.15)
bpy.ops.object.camera_add(location=(-3.6,.2,0)); camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.type='ORTHO'; camera.data.ortho_scale=2.5;scene.camera=camera
for pos,power,size in [((-2,3,3),180,4),((-3,-2,-1),90,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos); l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size
    l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(root/'Retina_ModelPreview_Candidate.png');bpy.ops.render.render(write_still=True)
tex.image=bpy.data.images.load(str(project/'Assets/HumanEye/ThirdParty/LocalEye/Retina_Diff.jpg'))
scene.render.filepath=str(root/'Retina_ModelPreview_Original.png');bpy.ops.render.render(write_still=True)
tex.image=bpy.data.images.load(str(root/'EyeInterior_BaseColor.png'))
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Retina_Candidate_Preview.blend'))
print('MODEL_PREVIEW_RENDERED: original retina UVs and vessel geometry; Blender preview only')

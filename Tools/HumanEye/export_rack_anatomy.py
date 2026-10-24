"""Extract owner-supplied anatomy from the Rack FBXs, excluding cabinets and glass.
Blender --background --factory-startup --python Tools/HumanEye/export_rack_anatomy.py
Produces static, metre-sized, feet-origin meshes and a single material per layer.
"""
import bpy, math, pathlib, json, numpy as np
from mathutils import Vector, Matrix

root = pathlib.Path(__file__).resolve().parents[2]
out = root / 'Assets/HumanEye/ThirdParty/UserModels/RackAnatomy'
out.mkdir(parents=True, exist_ok=True)
reports=[]
for source, name, yaw, ratio in [('Rack 01','Skeleton',0,.65), ('Rack 02','Muscles',25,.35)]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(pathlib.Path(r'C:/Users/ADMIN/Documents/Received Files')/(source+'.fbx')))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and (o.name=='skeleton' or o.name.startswith('Object_'))]
    assert meshes and not any(o.type=='ARMATURE' for o in bpy.context.scene.objects)
    # The muscle figure was baked into a turned cabinet; remove its heading before fitting.
    rotation=Matrix.Rotation(math.radians(yaw),4,'Z')
    for o in meshes:
        xf=rotation@o.matrix_world
        o.parent=None; o.matrix_world=Matrix.Identity(4); o.data.transform(xf)
    pts=[v.co.copy() for o in meshes for v in o.data.vertices]
    low=Vector([min(p[i] for p in pts) for i in range(3)]); high=Vector([max(p[i] for p in pts) for i in range(3)])
    head=[p for p in pts if p.z>high.z-(high.z-low.z)*.12]
    midx=(min(p.x for p in head)+max(p.x for p in head))/2
    midy=(min(p.y for p in head)+max(p.y for p in head))/2
    height=high.z-low.z
    for o in meshes:
        for v in o.data.vertices: v.co=(v.co-Vector((midx,midy,low.z)))*(1.75/height)
    # Embedded material images are self-contained; atlas the 14 muscle tiles to one 2048 map.
    materials=[o.data.materials[0] for o in meshes]
    grid=4 if len(meshes)>1 else 1; tile=512 if grid==4 else 2048; edge=4; size=grid*tile
    atlas=np.zeros((size,size,4),dtype=np.float32); atlas[:,:,3]=1
    for index,o in enumerate(meshes):
        mat=o.data.materials[0]; bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        links=bs.inputs['Base Color'].links
        assert links and links[0].from_node.type=='TEX_IMAGE', mat.name
        img=links[0].from_node.image; img.pixels[0]; img.scale(tile-edge*2,tile-edge*2)
        pixels=np.empty((tile-edge*2)**2*4,dtype=np.float32); img.pixels.foreach_get(pixels)
        pixels=pixels.reshape((tile-edge*2,tile-edge*2,4))
        pixels=np.pad(pixels,((edge,edge),(edge,edge),(0,0)),mode='edge')
        x=index%grid; y=index//grid; atlas[y*tile:(y+1)*tile,x*tile:(x+1)*tile]=pixels
        uv=o.data.uv_layers.active
        for loop in uv.data:
            loop.uv=((loop.uv.x*(tile-2*edge)+edge+x*tile)/size,(loop.uv.y*(tile-2*edge)+edge+y*tile)/size)
    image=bpy.data.images.new(name+'_Albedo',width=size,height=size,alpha=False)
    image.pixels.foreach_set(atlas.ravel()); image.file_format='PNG'; image.filepath_raw=str(out/(name+'_Albedo.png')); image.save()
    material=bpy.data.materials.new('Rack_'+name); material.use_nodes=True
    bs=material.node_tree.nodes.get('Principled BSDF'); bs.inputs['Roughness'].default_value=.6
    tex=material.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=image; material.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    before=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes:
        o.data.materials.clear(); o.data.materials.append(material)
        for p in o.data.polygons: p.material_index=0
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Presentation mesh budget','DECIMATE'); mod.ratio=ratio
        bpy.ops.object.modifier_apply(modifier=mod.name)
        o.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]; bpy.ops.object.join()
    result=bpy.context.object; result.name='Rack_'+name
    for o in list(bpy.context.scene.objects):
        if o != result: bpy.data.objects.remove(o,do_unlink=True)
    # Registration landmarks measured from the source front/side views, in the extracted metre frame.
    eye_z, eye_y, half_ipd = (1.654, -.047, .032) if name=='Skeleton' else (1.637, -.067, .0315)
    for side,x in [('L',half_ipd),('R',-half_ipd)]:
        marker=bpy.data.objects.new('EyeSocket_'+side,None); marker.location=(x,eye_y,eye_z)
        bpy.context.scene.collection.objects.link(marker); marker.select_set(True)
    front=bpy.data.objects.new('FaceDirection',None); front.location=(0,-1,eye_z)
    bpy.context.scene.collection.objects.link(front); front.select_set(True)
    # Feet at 0, head at 1.75; source faces Blender -Y / Unity -Z like the scan subject.
    after=sum(len(p.vertices)-2 for p in result.data.polygons)
    assert after<180000 and len(result.data.materials)==1
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,
        object_types={'MESH','EMPTY'},mesh_smooth_type='FACE',path_mode='STRIP',bake_anim=False)
    reports.append(dict(source=source,output=name,source_triangles=before,triangles=after,materials=1,atlas=size,rigged=False,yaw_correction=yaw))
    print('RACK_EXPORTED',reports[-1])
(out/'ImportReport.json').write_text(json.dumps(reports,indent=2))

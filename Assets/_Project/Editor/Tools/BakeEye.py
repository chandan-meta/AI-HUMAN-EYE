import bpy,json,os,sys,argparse
parser=argparse.ArgumentParser(description='Bake the supplied FitnessMan eye rig into independently selectable meshes.')
parser.add_argument('--source',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
from mathutils import Vector,Matrix
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=args.source)
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
mapping={'Eye_Ball':'Sclera','Cornea2':'Cornea','Cornea3':'AqueousHumour','Iris':'Iris','Pupil':'Pupil','Lense':'CrystallineLens','Vitreous':'VitreousHumour','Retina':'Retina','Choroid':'Choroid'}
selected=[]
for o in list(bpy.data.objects):
 if o.type!='MESH':continue
 name=next((v for k,v in mapping.items() if k in o.name),None)
 if not name:continue
 evaluated=o.evaluated_get(deps); mesh=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=deps)
 mesh.transform(evaluated.matrix_world)
 vs=[v.co for v in mesh.vertices];center=Vector([(min(v[i] for v in vs)+max(v[i] for v in vs))/2 for i in range(3)])
 print('EVALUATED_EYE',o.name,name,list(center))
 if center.x>0:selected.append((name,mesh,center))
 else:bpy.data.meshes.remove(mesh)
assert {'Sclera','Cornea','Iris','Pupil'}.issubset({n for n,m,c in selected}), 'Four eye parts are required'
sclera=next(m for n,m,c in selected if n=='Sclera');vs=[v.co for v in sclera.vertices]
center=Vector([(min(v[i] for v in vs)+max(v[i] for v in vs))/2 for i in range(3)])
center.y=min(v.y for v in vs)+(max(v.x for v in vs)-min(v.x for v in vs))/2
scene=bpy.data.scenes.new('EyeExport');bpy.context.window.scene=scene
for name,mesh,c in selected:
 mesh.transform(Matrix.Translation(-center));obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj);obj.select_set(True)
bpy.context.view_layer.objects.active=scene.objects[0]
bpy.ops.export_scene.fbx(filepath=args.output,use_selection=True,bake_anim=False,object_types={'MESH'},axis_forward='-Z',axis_up='Y')
print('EYE_BAKED',len(selected))
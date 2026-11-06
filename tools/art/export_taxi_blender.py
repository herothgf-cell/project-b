"""Export evaluated Blender surfaces to a deterministic Unity mesh interchange.
No Blender installation is needed for Unity import or the shipped game.
"""
import bpy,json,os
ROOT = globals().get('TAXI_REPO_ROOT') or os.environ.get('TAXI_REPO_ROOT')
if not ROOT or not os.path.isfile(os.path.join(ROOT,'unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt')):
 raise ValueError('Set TAXI_REPO_ROOT to the intended project-b repository root')
ROOT=os.path.abspath(ROOT).replace(chr(92),'/')
s=bpy.context.scene
assert s.name.startswith('TaxiConcept_Blender')
dg=bpy.context.evaluated_depsgraph_get();groups={};materials={};total=0
for o in s.objects:
 group=o.get('taxi_group','')
 if group not in ['body','wheel0']:continue
 evaluated=o.evaluated_get(dg);me=evaluated.to_mesh();me.calc_loop_triangles()
 matrix=o.matrix_world;nm=matrix.to_3x3().inverted().transposed()
 for tri in me.loop_triangles:
  material=me.materials[tri.material_index];key=material.name.split('.')[0];node=material.node_tree.nodes.get('Principled BSDF')
  materials[key]={'name':key,'color':list(node.inputs['Base Color'].default_value),'metallic':node.inputs['Metallic'].default_value,'smoothness':1-node.inputs['Roughness'].default_value}
  bucket=groups.setdefault((group,key),{'name':group+'_'+key,'group':'wheel' if group=='wheel0' else 'body','material':key,'vertices':[],'normals':[],'triangles':[],'lookup':{}})
  ids=[]
  for loop in tri.loops:
   p=matrix@me.vertices[me.loops[loop].vertex_index].co;n=(nm@me.corner_normals[loop].vector).normalized()
   pos=(p.x,p.z,-p.y);normal=(n.x,n.z,-n.y)
   if group=='wheel0':
    x,y,z=pos[0]+.85,pos[1],pos[2]-1.3;pos=(y,-x,z);normal=(normal[1],-normal[0],normal[2])
   value=tuple(round(v,6) for v in (*pos,*normal));idx=bucket['lookup'].get(value)
   if idx is None:
    idx=len(bucket['vertices'])//3;bucket['lookup'][value]=idx;bucket['vertices']+=list(value[:3]);bucket['normals']+=list(value[3:])
   ids.append(idx)
  bucket['triangles']+=ids;total+=1
 evaluated.to_mesh_clear()
out=[]
for g in groups.values():g.pop('lookup');out.append(g)
path=ROOT+'/art-source/taxi/unity-meshes.json'
with open(path,'w',encoding='utf-8') as f:json.dump({'version':1,'materials':list(materials.values()),'meshes':out},f,separators=(',',':'))
# Portable source exchange, separate from Unity runtime resources.
bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:
 if o.get('taxi_group','') in ['body','wheel0','wheel1','wheel2','wheel3']:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=ROOT+'/art-source/taxi/TaxiConcept.fbx',use_selection=True,object_types={'MESH','OTHER'},bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/art-source/taxi/TaxiConcept.blend')
result={'triangles_body_plus_one_wheel':total,'mesh_groups':len(out),'materials':len(materials),'interchange':path}

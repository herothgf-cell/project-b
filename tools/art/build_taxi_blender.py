"""Run through Blender MCP. Owns only the named TaxiConcept scene; preserves other scenes.
Unity coordinates are used by helpers (X right, Y up, Z forward), meters.
"""
import bpy, math, json, os
from mathutils import Vector
from math import sin, cos, pi, copysign
ROOT = globals().get('TAXI_REPO_ROOT') or os.environ.get('TAXI_REPO_ROOT')
if not ROOT or not os.path.isfile(os.path.join(ROOT,'unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt')):
 raise ValueError('Set TAXI_REPO_ROOT to the intended project-b repository root')
ROOT=os.path.abspath(ROOT).replace(chr(92),'/')
scene = bpy.data.scenes.new('TaxiConcept_Blender')
bpy.context.window.scene = scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
parts=[]
def xyz(p): return (p[0],-p[2],p[1])
def mat(name,color,metal=0,rough=.3):
 m=bpy.data.materials.new('Taxi_'+name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if name=='Paint': p.inputs['Coat Weight'].default_value=.45;p.inputs['Coat Roughness'].default_value=.18
 return m
paint=mat('Paint',(1,.48,.006),.08,.3);glass=mat('Glass',(.023,.055,.095),.35,.16)
rubber=mat('Rubber',(.018,.023,.031),0,.72);chrome=mat('Chrome',(.65,.72,.78),.85,.22)
lamp=mat('Lamp',(.85,.94,1),.2,.19);red=mat('Red',(.65,.012,.018),.2,.22)
amber=mat('Amber',(1,.28,.008),.1,.24);dark=mat('Trim',(.035,.047,.061),.1,.42)
def finish(o,name,m,group='body'):
 o.name=name;o.data.materials.append(m);o['taxi_group']=group;parts.append(o)
 if o.type=='MESH':
  for p in o.data.polygons:p.use_smooth=True
 return o
def mesh(name,vs,fs,m,group='body'):
 data=bpy.data.meshes.new(name);data.from_pydata([xyz(v) for v in vs],[],fs);data.update()
 o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);return finish(o,name,m,group)
def box(name,p,size,r,m,group='body'):
 if size[2]<min(size[0],size[1])*.65 and r>.03:
  w,h,d=size;rad=min(r,w*.49,h*.49);ring=[]
  for cx,cy,start in [(w/2-rad,h/2-rad,0),(-w/2+rad,h/2-rad,90),(-w/2+rad,-h/2+rad,180),(w/2-rad,-h/2+rad,270)]:
   for i in range(9):a=(start+i*90/8)*pi/180;ring.append((cx+rad*cos(a),cy+rad*sin(a)))
  n=len(ring);vs=[(p[0]+x,p[1]+y,p[2]+z) for z in [-d/2,d/2] for x,y in ring]
  fs=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))]
  o=mesh(name,vs,fs,m,group);bevel=o.modifiers.new('Lens edge roll','BEVEL');bevel.width=min(.007,d*.2);bevel.segments=3;o.modifiers.new('Weighted lens normals','WEIGHTED_NORMAL');return o
 bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p));o=bpy.context.object;o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bevel=o.modifiers.new('Soft manufactured edges','BEVEL');bevel.width=r;bevel.segments=4
 normal=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');normal.keep_sharp=True
 return finish(o,name,m,group)
def ellipsoid(name,p,size,m,group='body'):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=1,location=xyz(p));o=bpy.context.object;o.scale=(size[0]/2,size[2]/2,size[1]/2);return finish(o,name,m,group)
def tube(name,points,r,m,group='body'):
 d=bpy.data.curves.new(name,'CURVE');d.dimensions='3D';d.bevel_depth=r;d.bevel_resolution=2
 s=d.splines.new('POLY');s.points.add(len(points)-1)
 for a,p in zip(s.points,points):a.co=(*xyz(p),1)
 o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);return finish(o,name,m,group)
def interp(z,profile,index):
 for idx,(a,b) in enumerate(zip(profile,profile[1:])):
  if z<=b[0]:
   f=max(0,min(1,(z-a[0])/(b[0]-a[0])));prev=profile[max(0,idx-1)];nxt=profile[min(len(profile)-1,idx+2)];m0=(b[index]-prev[index])/(b[0]-prev[0]);m1=(nxt[index]-a[index])/(nxt[0]-a[0]);h=b[0]-a[0];value=(2*f**3-3*f*f+1)*a[index]+(f**3-2*f*f+f)*h*m0+(-2*f**3+3*f*f)*b[index]+(f**3-f*f)*h*m1;return max(min(a[index],b[index]),min(max(a[index],b[index]),value))
 return profile[-1][index]
bodyprofile=[(-1.99,.77,.88),(-1.86,.82,1.01),(-1.5,.9,.94),(-.9,.9,.84),(.7,.9,.84),(1.3,.89,.80),(1.75,.83,.77),(1.99,.76,.74)]
vs=[];fs=[];nr=80;ns=48
for i in range(nr+1):
 z=-1.99+3.98*i/nr;w=interp(z,bodyprofile,1);top=interp(z,bodyprofile,2)
 for j in range(ns):
  a=2*pi*j/ns;c=cos(a);s=sin(a);x=w*copysign(abs(c)**.48,c);y=.43+copysign(abs(s)**.55,s)*(top-.43 if s>0 else .34)
  dz=min(abs(z-1.3),abs(z+1.3))
  if dz<.425 and abs(x)>.52:
   arc=math.sqrt(.425**2-dz**2);f=min(1,(abs(x)-.52)/.20);f=f*f*(3-2*f);y=max(y,y+(arc-y)*f)
  vs.append((x,y,z))
for i in range(nr):
 for j in range(ns):a=i*ns+j;b=i*ns+(j+1)%ns;fs.append((a,b,b+ns,a+ns))
fs += [tuple(range(ns-1,-1,-1)),tuple(nr*ns+j for j in range(ns))]
body=mesh('Sculpted monocoque',vs,fs,paint)
profile=[(-1.82,.67,.86),(-1.65,.75,1.12),(-1.25,.77,1.48),(-.8,.77,1.55),(.15,.76,1.54),(.48,.75,1.41),(.75,.74,1.15),(1.03,.70,.83)]
def cabin(z,a,offset=0):
 w=interp(z,profile,1);top=interp(z,profile,2);c=cos(a)
 return (w*copysign(abs(c)**.58,c)+offset*c,.78+(top-.78)*sin(a)**.68+offset*sin(a),z)
vs=[];fs=[];nz=64;na=32
for i in range(nz+1):
 z=profile[0][0]+(profile[-1][0]-profile[0][0])*i/nz
 for j in range(na+1):vs.append(cabin(z,pi*j/na))
for i in range(nz):
 for j in range(na):a=i*(na+1)+j;fs.append((a,a+1,a+na+2,a+na+1))
mesh('Soft continuous roof and pillars',vs,fs,paint)
def patch(name,z0,z1,a0,a1):
 vs=[];fs=[];nz=20;na=14
 for i in range(nz+1):
  z=z0+(z1-z0)*i/nz
  for j in range(na+1):vs.append(cabin(z,a0+(a1-a0)*j/na,.009))
 for i in range(nz):
  for j in range(na):a=i*(na+1)+j;fs.append((a,a+1,a+na+2,a+na+1))
 mesh(name,vs,fs,glass)
 border=[cabin(z0,a0+(a1-a0)*j/na,.012) for j in range(na+1)]+[cabin(z0+(z1-z0)*i/nz,a1,.012) for i in range(1,nz+1)]+[cabin(z1,a1-(a1-a0)*j/na,.012) for j in range(1,na+1)]+[cabin(z1-(z1-z0)*i/nz,a0,.012) for i in range(1,nz+1)]
 tube(name+' rubber seal',border,.008,dark)
patch('Curved windshield',.45,.96,.8,pi-.8);patch('Curved rear hatch glass',-1.74,-1.22,.8,pi-.8)
for side in [-1,1]:
 a0,a1=(.055,.70) if side==1 else (pi-.70,pi-.055)
 patch('Front door glass',-.13,.77,a0,a1);patch('Rear door glass',-1.5,-.23,a0,a1)
 tube('Window beltline',[(side*.77,.81,z) for z in [-1.34,-.8,-.2,.4,.82]],.009,dark)
 for z in [-1.3,1.3]:tube('Painted wheel arch lip',[(side*.903,.433*sin(a),z+.433*cos(a)) for a in [(.08+.84*i/40)*pi for i in range(41)]],.012,paint)
 tube('Door split',[(side*.903,.24,-.19),(side*.905,.5,-.19),(side*.89,.77,-.19)],.004,dark)
 tube('Door lower seam',[(side*.884,.23,z) for z in [-.83,-.3,.3,.83]],.004,dark)
 box('Rocker sill',(side*.887,.15,0),(.035,.055,1.55),.018,paint)
 for z in [-.58,.39]:
  ellipsoid('Handle inset',(side*.898,.70,z),(.02,.055,.19),dark)
  box('Painted door handle',(side*.917,.715,z),(.035,.035,.16),.015,paint)
 box('Mirror mount',(side*.805,.97,.58),(.15,.05,.08),.02,dark)
 ellipsoid('Mirror housing',(side*.93,1.0,.58),(.25,.17,.24),paint)
 ellipsoid('Mirror glass',(side*.93,1.0,.46),(.19,.115,.018),glass)
 # Layered capsule lamps, restrained chrome, projector bowl and bright ring.
 box('Headlamp seal',(side*.56,.565,2.002),(.36,.27,.025),.11,dark)
 box('Headlamp rim',(side*.56,.565,2.02),(.33,.24,.018),.105,chrome)
 box('Headlamp bowl',(side*.56,.565,2.033),(.29,.20,.012),.085,glass)
 ellipsoid('Projector lens',(side*.56,.565,2.048),(.16,.16,.022),lamp)
 
 tube('Daylight ring',[(side*.56+.108*cos(a),.565+.089*sin(a),2.051) for a in [i*2*pi/48 for i in range(49)]],.01,lamp)
 box('Rear lamp gasket',(side*.64,.70,-2.002),(.22,.40,.025),.09,dark)
 box('Rear ruby lens',(side*.64,.74,-2.02),(.175,.28,.018),.07,red)
 box('Rear reversing lens',(side*.64,.55,-2.024),(.17,.07,.02),.035,lamp)
 ellipsoid('Fog lamp bezel',(side*.65,.27,1.96),(.17,.17,.045),dark)
 ellipsoid('Fog lamp',(side*.65,.27,1.99),(.105,.105,.018),lamp)
box('Grille chrome surround',(0,.43,1.985),(1.03,.36,.07),.16,chrome)
box('Recessed grille',(0,.43,2.025),(.95,.29,.035),.13,dark)
for i in range(4):box('Grille blade',(0,.335+i*.061,2.049),(.79,.014,.018),.007,chrome)
box('Front bumper lower',(0,.17,1.94),(1.25,.09,.09),.04,dark)
box('Rear bumper lower',(0,.19,-1.96),(1.4,.13,.08),.045,dark)
for side in [-1,1]:box('Rear reflector',(side*.57,.22,-2.008),(.18,.035,.012),.012,red)
box('Rear plate recess',(0,.54,-1.991),(.64,.22,.035),.04,dark)
box('Rear yellow plate',(0,.54,-2.013),(.58,.17,.016),.025,paint)
box('Front yellow plate',(0,.24,2.041),(.55,.15,.022),.02,paint)
box('Taxi sign rubber foot',(0,1.565,-.25),(.65,.055,.32),.025,dark)
box('Taxi roof sign',(0,1.69,-.25),(.6,.21,.29),.065,paint)
ellipsoid('Hood badge',(0,.763,1.55),(.095,.015,.055),chrome)
tube('Rear wiper',[(-.3,1.03,-1.70),(0,1.035,-1.70),(.22,1.06,-1.68)],.01,dark)
for side in [-1,1]:tube('Front wiper',[(side*.5,.93,.93),(side*.16,.96,.91)],.009,dark)
# Model wheels separately, using the same physical .34m radius and .85/1.3m mounts.
wheelparts=[]
for i in range(4):
 start=len(parts);x=-.85 if i%2==0 else .85;z=1.3 if i<2 else -1.3
 bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=12,location=xyz((x,0,z)),major_radius=.255,minor_radius=.085,rotation=(0,pi/2,0))
 finish(bpy.context.object,'Rounded tyre',rubber,'wheel'+str(i))
 for side in [-1,1]:
  xx=x+side*.085
  bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.225,depth=.022,location=xyz((xx,0,z)),rotation=(0,pi/2,0));finish(bpy.context.object,'Alloy barrel',chrome,'wheel'+str(i))
  bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.191,depth=.025,location=xyz((xx+side*.014,0,z)),rotation=(0,pi/2,0));finish(bpy.context.object,'Wheel cavity',dark,'wheel'+str(i))
  for k in range(5):
   a=k*2*pi/5;points=[(xx+side*.031,.04*cos(a),z+.04*sin(a)),(xx+side*.031,.19*cos(a+.13),z+.19*sin(a+.13))];tube('Sculpted alloy spoke',points,.027,chrome,'wheel'+str(i))
  ellipsoid('Hub cap',(xx+side*.04,0,z),(.035,.10,.10),chrome,'wheel'+str(i))
  for k in range(5):
   a=k*2*pi/5;ellipsoid('Lug',(xx+side*.063,.033*cos(a),z+.033*sin(a)),(.012,.012,.012),dark,'wheel'+str(i))
 wheelparts.extend(parts[start:])
# Studio belongs to this new scene only.
floor=box('Studio floor',(0,-.42,0),(200,.08,200),.01,mat('Studio',(.49,.60,.73),0,.65),'studio')
world=bpy.data.worlds.new('Taxi studio world');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.42,.6,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45;scene.world=world
def area(name,at,power,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=xyz(at);o.rotation_euler=(Vector(xyz((0,.5,0)))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key softbox',(3,6,4),1100,5);area('Fill softbox',(-4,3,1),700,4);area('Rear rim',(1,4,-4),1100,3)
data=bpy.data.cameras.new('Taxi review camera');cam=bpy.data.objects.new('Taxi review camera',data);scene.collection.objects.link(cam);scene.camera=cam;cam.location=xyz((4,2.3,5.8));cam.rotation_euler=(Vector(xyz((0,.65,0)))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=6.5
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1440;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
bpy.context.view_layer.update()
os.makedirs(ROOT+'/art-source/taxi',exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/art-source/taxi/TaxiConcept.blend')
result={'scene':scene.name,'objects':len(parts),'blend':ROOT+'/art-source/taxi/TaxiConcept.blend','status':'model ready for render/export'}

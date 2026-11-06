using System.Collections.Generic;
using UnityEngine;

// Visual geometry only. Never adds colliders or changes the vehicle's physical dimensions.
public static partial class TaxiArt {
 static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
 static Material yellow,glass,rubber,silver,red,white,trim;
 static Material Material(string name,Color color,float gloss){var m=WorldBuilder.Mat(name,color);m.SetFloat("_Glossiness",gloss);return m;}
 static void Materials(){if(yellow)return;yellow=Material("Taxi sunflower",new Color(1,.72f,.025f),.5f);glass=Material("Taxi blue glass",new Color(.055f,.14f,.23f),.72f);rubber=Material("Taxi rubber",new Color(.025f,.035f,.05f),.12f);silver=Material("Taxi alloy",new Color(.65f,.73f,.8f),.55f);red=Material("Taxi tail lamp",new Color(.82f,.025f,.04f),.45f);white=Material("Taxi lamp ivory",new Color(1,.97f,.8f),.45f);trim=Material("Cockpit navy",new Color(.045f,.075f,.12f),.18f);}
 static void RemoveCollider(GameObject g){var c=g.GetComponent<Collider>();c.enabled=false;if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c);}
 public static Mesh RoundedMesh(Vector3 size,float radius){
  radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.49f);
  string key=size.ToString("F4")+radius.ToString("F4");Mesh cached;if(meshes.TryGetValue(key,out cached)&&cached)return cached;
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();Vector3 half=size*.5f,core=half-Vector3.one*radius;const int segments=8;
  for(int face=0;face<6;face++){
   int axis=face/2;float sign=face%2==0?1:-1;int start=vertices.Count;
   for(int y=0;y<=segments;y++)for(int x=0;x<=segments;x++){
    Vector3 p=Vector3.zero;p[axis]=half[axis]*sign;p[(axis+1)%3]=Mathf.Lerp(-half[(axis+1)%3],half[(axis+1)%3],x/(float)segments);p[(axis+2)%3]=Mathf.Lerp(-half[(axis+2)%3],half[(axis+2)%3],y/(float)segments);
    Vector3 c=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));Vector3 n=(p-c).normalized;vertices.Add(c+n*radius);normals.Add(n);
   }
   for(int y=0;y<segments;y++)for(int x=0;x<segments;x++){int a=start+y*(segments+1)+x,b=a+1,c=a+segments+1,d=c+1;bool outward=Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a])>0;triangles.Add(a);triangles.Add(outward?b:c);triangles.Add(outward?c:b);triangles.Add(b);triangles.Add(outward?d:c);triangles.Add(outward?c:d);}
  }
  var mesh=new Mesh{name="Rounded taxi surface"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
 }
 public static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,float radius,Material material){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;g.AddComponent<MeshFilter>().sharedMesh=RoundedMesh(size,radius);g.AddComponent<MeshRenderer>().sharedMaterial=material;return g;}
 static GameObject Lens(Transform parent,string name,Vector3 position,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;RemoveCollider(g);return g;}
 static void Text(Transform parent,string name,string value,Vector3 at,float width,Color color,bool front){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=at;g.transform.localRotation=Quaternion.Euler(0,front?180:0,0);var t=g.AddComponent<TextMesh>();t.text=value;t.color=color;KoreanText.Fit(t,width*10,width*2.4f);}
 static void Window(Transform parent,string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outside){var g=new GameObject(name);g.transform.SetParent(parent,false);var m=new Mesh{name=name};m.vertices=new[]{a,b,c,d};bool forward=Vector3.Dot(Vector3.Cross(b-a,c-a),outside)>0;m.triangles=forward?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};m.RecalculateNormals();m.RecalculateBounds();g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=glass;}
 static void Cabin(Transform parent){
  float[] zs={-1.5f,-1.05f,-.75f,.25f,.48f,1.03f};float[] tops={.81f,1.34f,1.41f,1.41f,1.34f,.81f};var vs=new List<Vector3>();var ts=new List<int>();
  for(int i=0;i<zs.Length;i++){float y=tops[i],z=zs[i];vs.AddRange(new[]{new Vector3(-.75f,.72f,z),new Vector3(-.75f,y-.06f,z),new Vector3(-.65f,y,z),new Vector3(.65f,y,z),new Vector3(.75f,y-.06f,z),new Vector3(.75f,.72f,z)});}
  for(int i=0;i<zs.Length-1;i++)for(int j=0;j<6;j++){int a=i*6+j,b=i*6+(j+1)%6,c=a+6,d=b+6;ts.AddRange(new[]{a,b,c,b,d,c});}
  ts.AddRange(new[]{0,2,1,0,3,2,0,4,3,0,5,4,30,31,32,30,32,33,30,33,34,30,34,35});for(int i=0;i<ts.Count;i+=3){int swap=ts[i+1];ts[i+1]=ts[i+2];ts[i+2]=swap;}var mesh=new Mesh{name="Sloping taxi cabin"};mesh.SetVertices(vs);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var shell=new GameObject("Sculpted cabin");shell.transform.SetParent(parent,false);shell.AddComponent<MeshFilter>().sharedMesh=mesh;shell.AddComponent<MeshRenderer>().sharedMaterial=yellow;
  Window(parent,"Windshield",new Vector3(-.62f,.85f,1.006f),new Vector3(.62f,.85f,1.006f),new Vector3(.6f,1.285f,.54f),new Vector3(-.6f,1.285f,.54f),Vector3.forward);
  Window(parent,"Rear window",new Vector3(-.6f,.85f,-1.48f),new Vector3(.6f,.85f,-1.48f),new Vector3(.6f,1.28f,-1.11f),new Vector3(-.6f,1.28f,-1.11f),Vector3.back);
  for(int side=-1;side<=1;side+=2){float x=side*.757f;Window(parent,"Front side window",new Vector3(x,.82f,-.15f),new Vector3(x,.82f,.89f),new Vector3(x,1.23f,.46f),new Vector3(x,1.29f,-.15f),Vector3.right*side);Window(parent,"Rear side window",new Vector3(x,.82f,-1.36f),new Vector3(x,.82f,-.24f),new Vector3(x,1.29f,-.24f),new Vector3(x,1.24f,-1.01f),Vector3.right*side);}
 }
 static void ProceduralExterior(Transform parent){
  Materials();var root=new GameObject("Taxi exterior");root.transform.SetParent(parent,false);var t=root.transform;
  Part(t,"Rounded body",new Vector3(0,.43f,0),new Vector3(1.79f,.69f,3.96f),.25f,yellow);
  Cabin(t);
  Part(t,"Hood",new Vector3(0,.72f,1.27f),new Vector3(1.66f,.16f,1.22f),.075f,yellow);
  Part(t,"Tailgate",new Vector3(0,.77f,-1.48f),new Vector3(1.67f,.25f,.91f),.12f,yellow);
  Part(t,"Front grille",new Vector3(0,.35f,1.982f),new Vector3(.91f,.22f,.045f),.02f,rubber);
  Part(t,"Rear lower bumper",new Vector3(0,.2f,-1.98f),new Vector3(1.35f,.14f,.07f),.03f,rubber);
  Part(t,"Taxi sign base",new Vector3(0,1.43f,-.25f),new Vector3(.65f,.08f,.33f),.035f,rubber);
  Part(t,"Taxi sign",new Vector3(0,1.56f,-.25f),new Vector3(.61f,.22f,.3f),.07f,yellow);
  Text(t,"TAXI front","TAXI",new Vector3(0,1.56f,-.091f),.055f,Color.black,true);Text(t,"TAXI rear","TAXI",new Vector3(0,1.56f,-.409f),.055f,Color.black,false);
  for(int side=-1;side<=1;side+=2){
   Part(t,"Rear lamp surround",new Vector3(side*.67f,.63f,-1.93f),new Vector3(.29f,.48f,.12f),.055f,rubber);
   Part(t,"Red tail lamp",new Vector3(side*.67f,.68f,-2.001f),new Vector3(.23f,.32f,.045f),.022f,red);
   Part(t,"Reverse lamp",new Vector3(side*.67f,.46f,-2.003f),new Vector3(.22f,.09f,.045f),.02f,white);
   Part(t,"Headlight surround",new Vector3(side*.64f,.62f,1.92f),new Vector3(.4f,.29f,.13f),.06f,silver);
   Lens(t,"Headlight",new Vector3(side*.64f,.62f,1.993f),new Vector3(.34f,.235f,.065f),white);
   Part(t,"Window pillar",new Vector3(side*.746f,1.035f,-.22f),new Vector3(.035f,.49f,.095f),.016f,rubber);
   Part(t,"Mirror",new Vector3(side*.94f,.99f,.62f),new Vector3(.25f,.18f,.24f),.075f,yellow);
   for(int door=0;door<2;door++)Part(t,"Door handle",new Vector3(side*.892f,.73f,.38f-door*.95f),new Vector3(.035f,.055f,.19f),.016f,silver);
   Part(t,"Door sill",new Vector3(side*.9f,.16f,0),new Vector3(.04f,.09f,1.72f),.018f,rubber);
  }
  Part(t,"Rear plate",new Vector3(0,.49f,-1.998f),new Vector3(.6f,.18f,.035f),.015f,yellow);Text(t,"Rear registration","서울 1234",new Vector3(0,.49f,-2.021f),.035f,Color.black,false);
  Part(t,"Front plate",new Vector3(0,.27f,2.012f),new Vector3(.56f,.15f,.035f),.015f,yellow);Text(t,"Front registration","서울 1234",new Vector3(0,.27f,2.034f),.032f,Color.black,true);
 }
 static Transform ProceduralWheel(Transform parent){Materials();var g=new GameObject("Tyre visual");g.transform.SetParent(parent,false);var tyre=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tyre.transform.SetParent(g.transform,false);tyre.transform.localScale=new Vector3(.68f,.12f,.68f);tyre.GetComponent<Renderer>().sharedMaterial=rubber;RemoveCollider(tyre);for(int side=-1;side<=1;side+=2){var hub=GameObject.CreatePrimitive(PrimitiveType.Cylinder);hub.name="Alloy wheel";hub.transform.SetParent(g.transform,false);hub.transform.localPosition=new Vector3(0,side*.124f,0);hub.transform.localScale=new Vector3(.43f,.012f,.43f);hub.GetComponent<Renderer>().sharedMaterial=silver;RemoveCollider(hub);Lens(g.transform,"Wheel recess",new Vector3(0,side*.143f,0),new Vector3(.35f,.02f,.35f),rubber);for(int spoke=0;spoke<5;spoke++){float a=spoke*Mathf.PI*2/5;var bar=Part(g.transform,"Alloy spoke",new Vector3(Mathf.Sin(a)*.11f,side*.16f,Mathf.Cos(a)*.11f),new Vector3(.045f,.018f,.18f),.008f,silver);bar.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);}Part(g.transform,"Hub cap",new Vector3(0,side*.17f,0),new Vector3(.1f,.025f,.1f),.012f,silver);}return g.transform;}
 public static GameObject Cockpit(Transform parent,out Transform steering){
  Materials();var root=new GameObject("Taxi cockpit");root.transform.SetParent(parent,false);root.transform.localPosition=new Vector3(-.43f,1.07f,.6f);var t=root.transform;
  Part(t,"Soft dashboard",new Vector3(.35f,-.62f,.68f),new Vector3(1.84f,.32f,.65f),.14f,trim);
  Part(t,"Yellow dashboard seam",new Vector3(.35f,-.445f,.64f),new Vector3(1.75f,.017f,.04f),.008f,yellow);
  Part(t,"Instrument hood",new Vector3(-.13f,-.4f,.64f),new Vector3(.58f,.17f,.25f),.075f,trim);
  for(int i=0;i<2;i++)Part(t,"Air vent",new Vector3(.35f+i*.4f,-.57f,.335f),new Vector3(.26f,.11f,.025f),.012f,rubber);
  var wheel=new GameObject("Steering wheel");wheel.transform.SetParent(t,false);wheel.transform.localPosition=new Vector3(-.13f,-.39f,.4f);steering=wheel.transform;
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();const int ring=64,tube=10;
  for(int i=0;i<=ring;i++)for(int j=0;j<=tube;j++){float a=i*2*Mathf.PI/ring,b=j*2*Mathf.PI/tube;var n=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));vertices.Add(new Vector3(Mathf.Cos(a)*.205f,Mathf.Sin(a)*.205f,0)+n*.027f);normals.Add(n);}
  for(int i=0;i<ring;i++)for(int j=0;j<tube;j++){int a=i*(tube+1)+j,b=a+tube+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
  var mesh=new Mesh{name="Continuous steering rim"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();wheel.AddComponent<MeshFilter>().sharedMesh=mesh;wheel.AddComponent<MeshRenderer>().sharedMaterial=rubber;
  Part(steering,"Steering hub",Vector3.zero,new Vector3(.19f,.13f,.07f),.032f,trim);Part(steering,"Horizontal spokes",Vector3.zero,new Vector3(.36f,.04f,.035f),.017f,silver);Part(steering,"Lower spoke",new Vector3(0,-.11f,0),new Vector3(.045f,.17f,.035f),.017f,silver);
  foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=10;return root;
 }
}

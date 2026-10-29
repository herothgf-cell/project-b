using System.Collections.Generic;
using UnityEngine;
namespace Pvp.Map {
// PVP-only procedural scenery; authority and campaign facilities never depend on it.
public static class PvpDistrictScenery {
 public static void Build(PvpMapRuntime map,Transform root,PvpMapVisualResources resources){new Builder(map,root,resources).Build();}
 sealed class Builder {
  readonly PvpMapRuntime map;readonly Transform root;readonly PvpMapVisualResources resources;
  readonly Dictionary<Color,Material> palette=new Dictionary<Color,Material>();
  readonly List<Vector3> occupied=new List<Vector3>();
  readonly Color concrete=new Color(.64f,.66f,.64f),white=new Color(.93f,.93f,.84f),yellow=new Color(1,.76f,.14f),green=new Color(.28f,.46f,.24f),dark=new Color(.12f,.17f,.21f);
  public Builder(PvpMapRuntime m,Transform p,PvpMapVisualResources r){map=m;root=p;resources=r;}
  Material Mat(Color color){Material value;if(!palette.TryGetValue(color,out value)){value=resources.CreateMaterial("Standard",color);palette.Add(color,value);}return value;}
  void Shape(string name,PrimitiveType type,Vector3 at,Vector3 size,Color color,Quaternion rotation=default(Quaternion),bool floor=false){
   var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=at;go.transform.localRotation=rotation==default(Quaternion)?Quaternion.identity:rotation;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=Mat(color);
   if(!floor){var collider=go.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);}
  }
  void Box(string name,Vector3 at,Vector3 size,Color color,Quaternion rotation=default(Quaternion)){Shape(name,PrimitiveType.Cube,at,size,color,rotation);}
  void Label(string text,Vector3 at,float size){var go=new GameObject("Sign "+text);go.transform.SetParent(root,false);go.transform.localPosition=at;var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontSize=48;mesh.characterSize=size;mesh.color=white;KoreanText.Apply(mesh);go.AddComponent<PvpSceneryBillboard>();}
  bool Clear(Vector3 at,float radius,int except=-1,bool stops=true){
   at.y=0;for(int i=0;i<map.Connections.Count;i++){if(i==except)continue;var edge=map.Connections[i];var a=map.Nodes[edge.From];var delta=map.Nodes[edge.To]-a;var nearest=a+delta*Mathf.Clamp01(Vector3.Dot(at-a,delta)/delta.sqrMagnitude);if((at-nearest).sqrMagnitude<Mathf.Pow(map.RoadWidth*.5f+radius+1.3f,2))return false;}
   if(stops)foreach(var stop in map.Stops)if((at-stop.Position).sqrMagnitude<Mathf.Pow(radius+8,2))return false;return true;
  }
  public void Build(){
   var min=map.BoundsMin;var max=map.BoundsMax;var center=(min+max)*.5f;
   Box("District terrain",new Vector3(center.x,-.39f,center.z),new Vector3(max.x-min.x+240,.3f,max.z-min.z+240),new Color(.43f,.53f,.37f));
   foreach(var district in map.Districts){Box("District ground "+district.Name,district.Center+Vector3.down*.21f,new Vector3(district.Size.x,.08f,district.Size.y),Color.Lerp(district.Color,green,.72f));Label(district.Name,district.Center+Vector3.up*18,.16f);}
   Roads();Hubs();Facilities();Blocks(min,max);Border(min,max);
  }
  void Roads(){
   for(int i=0;i<map.Connections.Count;i++){
    var edge=map.Connections[i];var a=map.Nodes[edge.From];var b=map.Nodes[edge.To];var forward=(b-a).normalized;var right=Vector3.Cross(Vector3.up,forward);var rotation=Quaternion.LookRotation(forward);float length=Vector3.Distance(a,b);
    // Retain the original road floor collider. All added decoration is visual only.
    Shape("Lane "+edge.From+" > "+edge.To,PrimitiveType.Cube,(a+b)*.5f+Vector3.down*.15f,new Vector3(map.RoadWidth,.25f,length),new Color(.19f,.22f,.25f),rotation,true);
    for(float d=10;d<length-9;d+=14){
     var at=a+forward*d;if(!Clear(at,0,i,false))continue;
     Box("Lane dash",at+Vector3.up*.015f,new Vector3(.14f,.025f,3.4f),white,rotation);
     foreach(int sign in new[]{-1,1}){
      var side=at+right*(map.RoadWidth*.5f+.95f)*sign;
      if(Clear(side,5.6f,i)){Box("Sidewalk",side+Vector3.up*.04f,new Vector3(1.8f,.18f,11),concrete,rotation);Box("Curb",at+right*(map.RoadWidth*.5f+.12f)*sign+Vector3.up*.07f,new Vector3(.2f,.25f,11),white,rotation);}
      Box("Road edge",at+right*(map.RoadWidth*.5f-.25f)*sign+Vector3.up*.015f,new Vector3(.1f,.025f,8),yellow,rotation);
     }
    }
    for(float d=22;d<length-16;d+=56){
     var at=a+forward*d;
     if(Clear(at,0,i,false)){Box("Direction shaft",at+Vector3.up*.035f,new Vector3(.28f,.025f,2.5f),white,rotation);foreach(int sign in new[]{-1,1})Box("Direction arrow",at+forward*.65f+right*.42f*sign+Vector3.up*.035f,new Vector3(.22f,.025f,1.35f),white,rotation*Quaternion.Euler(0,-sign*43,0));}
     var lamp=at+right*(map.RoadWidth*.5f+3.5f);if(Clear(lamp,1.3f)){Box("Street lamp",lamp+Vector3.up*3.8f,new Vector3(.17f,7.6f,.17f),dark);Box("Lamp head",lamp+Vector3.up*7.5f,new Vector3(.9f,.18f,.45f),white,rotation);}
    }
   }
   foreach(var signal in map.SignalZones){var p=new PvpRoadPosition(signal.ApproachEdge,signal.StopFraction);var forward=map.Forward(p);var right=Vector3.Cross(Vector3.up,forward);var rotation=Quaternion.LookRotation(forward);var at=map.Position(p);Box("Stop line "+signal.Id,at+Vector3.up*.045f,new Vector3(map.RoadWidth-.5f,.035f,.42f),white,rotation);for(float x=-map.RoadWidth*.5f+.8f;x<map.RoadWidth*.5f;x+=1.25f)Box("Crosswalk "+signal.Id,at+forward*3+right*x+Vector3.up*.045f,new Vector3(.65f,.035f,2.8f),white,rotation);}
  }
  bool FindSite(Vector3 origin,Vector3 outward,float radius,float start,float end,out Vector3 result){for(float distance=start;distance<=end;distance+=4)for(int turn=0;turn<8;turn++){var at=origin+Quaternion.Euler(0,turn*45,0)*outward*distance;if(Clear(at,radius)&&Away(at,radius)){result=at;return true;}}result=Vector3.zero;return false;}
  bool Away(Vector3 at,float radius){foreach(var p in occupied)if((p-at).sqrMagnitude<Mathf.Pow(radius+18,2))return false;return true;}
  void Hubs(){
   Color[] colors={new Color(.2f,.65f,.48f),new Color(.2f,.55f,.85f),new Color(.93f,.5f,.22f),new Color(.64f,.4f,.75f)};
   for(int i=0;i<map.Stops.Count;i++){
    var stop=map.Stops[i];var color=colors[i%colors.Length];Shape("Passenger bay "+stop.Id,PrimitiveType.Cylinder,stop.Position+Vector3.up*.035f,new Vector3(5.5f,.025f,5.5f),color);
    Vector3 outward=stop.Position.sqrMagnitude>.01f?stop.Position.normalized:Vector3.forward;Vector3 at;
    // Passenger circles stay empty. Shelters sit beyond the road and bay clearance.
    if(!FindSite(stop.Position,outward,3.5f,14,30,out at))continue;var rotation=Quaternion.LookRotation(-outward);
    Box("Hub platform "+stop.Id,at,new Vector3(5.5f,.18f,3),concrete,rotation);
    foreach(int sign in new[]{-1,1})Box("Shelter column",at+rotation*new Vector3(sign*2.2f,1.5f,.85f),new Vector3(.18f,3,.18f),dark,rotation);
    Box("Shelter roof",at+Vector3.up*3,new Vector3(5.8f,.28f,3.3f),color,rotation);Box("Bench",at+Vector3.up*.6f,new Vector3(3.4f,.2f,.65f),new Color(.49f,.32f,.19f),rotation);
    Label((i+1)+"  "+stop.Name+"\n승객 승하차",at+Vector3.up*4.6f,.075f);occupied.Add(at);
   }
  }
  void Facilities(){
   foreach(var service in map.Services){
    var stop=map.Stops[service.StopIndex];var outward=stop.Position.sqrMagnitude>.01f?stop.Position.normalized:Vector3.forward;Vector3 at;
    if(!FindSite(stop.Position,outward,16.5f,30,90,out at))continue;var rotation=Quaternion.LookRotation(-outward);bool garage=service.Id.ToLowerInvariant().Contains("garage")||service.Name.Contains("차고");var color=garage?new Color(.23f,.47f,.72f):new Color(.89f,.28f,.18f);
    Box("Facility forecourt "+service.Id,at+Vector3.down*.08f,new Vector3(24,.12f,22),concrete,rotation);var rear=at+rotation*new Vector3(0,0,6);
    Box("Facility building "+service.Id,rear+Vector3.up*2.5f,new Vector3(18,5,8),new Color(.8f,.8f,.72f),rotation);Box("Facility fascia",rear+Vector3.up*4.4f+rotation*Vector3.back*4.05f,new Vector3(18,.9f,.18f),color,rotation);
    if(garage){for(int n=-1;n<=1;n++){Box("Garage roller door",rear+Vector3.up*1.9f+rotation*new Vector3(n*5.5f,0,-4.1f),new Vector3(4.5f,3.5f,.15f),dark,rotation);for(int bar=0;bar<5;bar++)Box("Garage door slat",rear+Vector3.up*(.7f+bar*.55f)+rotation*new Vector3(n*5.5f,0,-4.22f),new Vector3(4.25f,.06f,.05f),concrete,rotation);}}
    else{Box("Fuel canopy",at+Vector3.up*4.7f+rotation*Vector3.back*3,new Vector3(18,.55f,8),color,rotation);foreach(int side in new[]{-1,1}){var pump=at+rotation*new Vector3(side*5,0,-3);Box("Fuel canopy post",pump+Vector3.up*2.2f,new Vector3(.25f,4.4f,.25f),white);Box("Fuel pump",pump+Vector3.up*.9f,new Vector3(1.25f,1.8f,.85f),white,rotation);Box("Fuel display",pump+Vector3.up*1.25f+rotation*Vector3.back*.46f,new Vector3(.85f,.45f,.08f),dark,rotation);}}
    Label(service.Name+"\n주유 "+(service.DurationTicks/50f).ToString("0.#")+"초 · "+service.CostPoints+"점",rear+Vector3.up*7.2f,.095f);
    Label("주유 정차 구역",stop.Position+Vector3.up*4.5f,.065f);occupied.Add(at);
   }
  }
  void Blocks(Vector3 min,Vector3 max){
   // Fixed arithmetic leaves Unity Random state untouched across repeated sessions.
   int index=0;for(float x=min.x+22;x<max.x-15;x+=31)for(float z=min.z+22;z<max.z-15;z+=31){
    index++;var at=new Vector3(x,0,z);float width=12+index%3*2,depth=12+index%4;float radius=Mathf.Sqrt(width*width+depth*depth)*.5f;if(!Clear(at,radius)||!Away(at,radius))continue;
    if(index%7==0){Box("Pocket park",at+Vector3.down*.1f,new Vector3(24,.12f,24),green);Box("Park walk",at,new Vector3(3,.04f,23),concrete);foreach(int sign in new[]{-1,1}){var p=at+new Vector3(sign*7,0,6);if(Clear(p,2.5f))Tree(p,index);p=at+new Vector3(sign*7,0,-6);if(Clear(p,2.5f))Tree(p,index+1);}continue;}
    float height=6+index%6*3;Color body=Color.Lerp(new Color(.57f,.59f,.57f),new Color(.84f,.75f,.59f),(index%5)/4f);foreach(var district in map.Districts)if(Mathf.Abs(x-district.Center.x)<district.Size.x*.5f&&Mathf.Abs(z-district.Center.z)<district.Size.y*.5f){body=Color.Lerp(body,district.Color,.25f);break;}
    Box("City block "+index,at+Vector3.up*height*.5f,new Vector3(width,height,depth),body);Box("Building roof",at+Vector3.up*(height+.18f),new Vector3(width+.5f,.36f,depth+.5f),dark);Box("Roof equipment",at+new Vector3(2,height+.8f,1),new Vector3(3,1.3f,3),concrete);
    for(float y=2.6f;y<height-1;y+=3)foreach(int sign in new[]{-1,1}){var glass=new Color(.32f,.49f,.56f);Box("Front windows",at+new Vector3(0,y,sign*(depth*.5f+.03f)),new Vector3(width-2,.85f,.06f),glass);Box("Side windows",at+new Vector3(sign*(width*.5f+.03f),y,0),new Vector3(.06f,.85f,depth-2),glass);}
    var tree=at+new Vector3(width*.5f+3,0,depth*.5f+3);if(Clear(tree,2.5f)&&Away(tree,2.5f))Tree(tree,index);
   }
  }
  void Tree(Vector3 at,int variant){Shape("Tree trunk",PrimitiveType.Cylinder,at+Vector3.up*1.6f,new Vector3(.5f,1.6f,.5f),new Color(.35f,.25f,.16f));Shape("Tree crown",PrimitiveType.Sphere,at+Vector3.up*4,new Vector3(4.5f,5,4.5f),variant%2==0?new Color(.24f,.42f,.21f):new Color(.34f,.52f,.24f));}
  void Border(Vector3 min,Vector3 max){for(float x=min.x-25;x<=max.x+25;x+=20){var a=new Vector3(x,0,min.z-32);var b=new Vector3(x,0,max.z+32);if(Clear(a,2.5f)&&Away(a,2.5f))Tree(a,0);if(Clear(b,2.5f)&&Away(b,2.5f))Tree(b,1);}for(float z=min.z-12;z<=max.z+12;z+=20){var a=new Vector3(min.x-32,0,z);var b=new Vector3(max.x+32,0,z);if(Clear(a,2.5f)&&Away(a,2.5f))Tree(a,1);if(Clear(b,2.5f)&&Away(b,2.5f))Tree(b,0);}}
 }
}
// Camera-facing signs only; no campaign camera dependency or gameplay callbacks.
public sealed class PvpSceneryBillboard:MonoBehaviour {Camera view;void LateUpdate(){if(view==null){var owner=GetComponentInParent<PvpBootstrap>();view=owner==null?Camera.main:owner.GetComponentInChildren<Camera>();}if(view!=null)transform.rotation=view.transform.rotation;}}
}

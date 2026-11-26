using UnityEngine;

// Play feedback round 8 (2026-11-26): "파출소/응급실 건물 디자인에서 작업해줘 · 딸랑 판떼기만 있으면" — the police box
// and the emergency room are now buildings, not a sign on a pole.
//  - 은행파출소: a two-storey police box built over the map building's footprint (slightly larger, so the plain map
//    block disappears inside it): white walls, navy fascia with "POLICE · 은행파출소", a blue stripe, a glass entrance
//    with a canopy and an emblem toward the road, upper windows, a red/blue light bar and an antenna on the roof, and a
//    patrol car parked beside it.
//  - 은계 응급의료센터 (game-only): the ground floor of the building face beside its drop stop becomes the emergency
//    entrance: white and red panelling, an "응급의료센터 EMERGENCY" band, a lit red cross, sliding glass doors, a
//    drive-through canopy on posts over the walk to the stop with "응급실" on its edge, and a parked ambulance.
// Visual only: nothing here has a collider, so driving and accidents are unchanged.
public static class CareBuildings {
 static Material M(string name,Color c){return WorldBuilder.Mat(name,c);}
 static readonly Color White=new Color(.93f,.94f,.95f),Navy=new Color(.09f,.18f,.42f),Blue=new Color(.16f,.38f,.80f),Glass=new Color(.16f,.26f,.36f),Red=new Color(.80f,.13f,.15f),Grey=new Color(.55f,.57f,.60f),Gold=new Color(.86f,.68f,.18f);
 public static GameObject PoliceRoot,EmergencyRoot;
 static float Ground(Vector3 p){RaycastHit hit;return Physics.Raycast(new Vector3(p.x,p.y+30,p.z),Vector3.down,out hit,80,~(1<<8),QueryTriggerInteraction.Ignore)?hit.point.y:p.y;}
 static GameObject Box(Transform parent,string name,Vector3 local,Vector3 size,Material m){return WorldBuilder.Box(name,local,size,m,parent,false);}
 static void Text(Transform parent,string name,Vector3 local,string text,Color color,float width,float height){
  var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localRotation=Quaternion.identity;
  var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;KoreanText.Apply(t);KoreanText.Fit(t,width,height);
 }
 // A frame whose local -z faces the viewer outside the facade (TextMesh reads from -z).
 static Transform Frame(string name,Vector3 position,Vector3 outward){var t=new GameObject(name).transform;outward.y=0;t.position=position;t.rotation=Quaternion.LookRotation(-outward.normalized);return t;}

 // ---- police box ----
 public static void BuildPolice(MapData map,Vector3 roadPoint){
  if(PoliceRoot||map==null||map.buildings==null||LostItemRules.Police==null)return;
  MapBuilding b=null;foreach(var x in map.buildings)if(x!=null&&x.name==LostItemRules.Police.name){b=x;break;}if(b==null||b.points==null||b.points.Length<3)return;
  // Oriented box from the footprint: the longest edge sets the long axis.
  Vector3 centre=Vector3.zero;foreach(var p in b.points)centre+=p.V();centre/=b.points.Length;centre.y=0;
  Vector3 u=Vector3.right;float longest=0;for(int i=0;i<b.points.Length;i++){var e=b.points[(i+1)%b.points.Length].V()-b.points[i].V();e.y=0;if(e.sqrMagnitude>longest){longest=e.sqrMagnitude;u=e.normalized;}}
  var v=new Vector3(-u.z,0,u.x);float minU=float.MaxValue,maxU=float.MinValue,minV=float.MaxValue,maxV=float.MinValue;
  foreach(var p in b.points){var d=p.V()-centre;d.y=0;float a=Vector3.Dot(d,u),c=Vector3.Dot(d,v);minU=Mathf.Min(minU,a);maxU=Mathf.Max(maxU,a);minV=Mathf.Min(minV,c);maxV=Mathf.Max(maxV,c);}
  centre+=u*(minU+maxU)*.5f+v*(minV+maxV)*.5f;float width=maxU-minU+.4f,depth=maxV-minV+.4f;
  // The face toward the road is the front.
  var toRoad=roadPoint-centre;toRoad.y=0;Vector3 outward;float frontWidth,frontDepth;
  if(Mathf.Abs(Vector3.Dot(toRoad,u))>Mathf.Abs(Vector3.Dot(toRoad,v))){outward=u*Mathf.Sign(Vector3.Dot(toRoad,u));frontWidth=depth;frontDepth=width;}else{outward=v*Mathf.Sign(Vector3.Dot(toRoad,v));frontWidth=width;frontDepth=depth;}
  float ground=Ground(centre),height=Mathf.Max(b.height,6)+1.2f;
  var root=Frame("파출소 건물 · "+LostItemRules.Police.name,new Vector3(centre.x,ground,centre.z),outward);PoliceRoot=root.gameObject;
  Material white=M("파출소 외벽",White),navy=M("파출소 남색",Navy),blue=M("파출소 청색 띠",Blue),glass=M("파출소 유리",Glass),grey=M("파출소 회색",Grey),gold=M("파출소 휘장",Gold);
  float front=-frontDepth*.5f; // local -z is the front face
  Box(root,"본관",new Vector3(0,height*.5f,0),new Vector3(frontWidth,height,frontDepth),white);
  Box(root,"지붕 테",new Vector3(0,height+.15f,0),new Vector3(frontWidth+.3f,.3f,frontDepth+.3f),grey);
  Box(root,"남색 간판 띠",new Vector3(0,height-.75f,front-.06f),new Vector3(frontWidth+.1f,1.1f,.14f),navy);
  foreach(float side in new[]{-1f,1f})Box(root,"측면 남색 띠",new Vector3(side*(frontWidth*.5f+.06f),height-.75f,0),new Vector3(.14f,1.1f,frontDepth+.1f),navy);
  Box(root,"뒷면 남색 띠",new Vector3(0,height-.75f,-front+.06f),new Vector3(frontWidth+.1f,1.1f,.14f),navy);
  Text(root,"간판 글자",new Vector3(0,height-.75f,front-.15f),"POLICE  "+LostItemRules.Police.name,Color.white,frontWidth*.86f,.75f);
  Box(root,"청색 띠",new Vector3(0,3.15f,front-.05f),new Vector3(frontWidth+.1f,.28f,.12f),blue);
  // Entrance: glass doors, canopy and emblem.
  Box(root,"출입문 유리",new Vector3(0,1.25f,front-.04f),new Vector3(2.6f,2.5f,.1f),glass);
  Box(root,"출입문 틀",new Vector3(0,2.55f,front-.05f),new Vector3(2.8f,.12f,.12f),grey);
  Box(root,"출입문 가운데",new Vector3(0,1.25f,front-.1f),new Vector3(.06f,2.5f,.04f),grey);
  Box(root,"차양",new Vector3(0,2.85f,front-.9f),new Vector3(3.6f,.16f,1.8f),navy);
  var emblem=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Object.Destroy(emblem.GetComponent<Collider>());emblem.name="경찰 휘장";emblem.transform.SetParent(root,false);emblem.transform.localPosition=new Vector3(0,3.75f,front-.08f);emblem.transform.localRotation=Quaternion.Euler(90,0,0);emblem.transform.localScale=new Vector3(.8f,.03f,.8f);emblem.GetComponent<Renderer>().sharedMaterial=gold;
  // Windows: ground floor beside the door, the whole upper floor.
  for(float x=-frontWidth*.5f+1.2f;x<=frontWidth*.5f-1.2f;x+=1.8f){
   if(Mathf.Abs(x)>2.2f)Box(root,"1층 창",new Vector3(x,1.55f,front-.04f),new Vector3(1.2f,1.3f,.08f),glass);
   Box(root,"2층 창",new Vector3(x,4.4f,front-.04f),new Vector3(1.3f,1.3f,.08f),glass);}
  for(float z=-frontDepth*.5f+1.4f;z<=frontDepth*.5f-1.4f;z+=2.2f)foreach(float side in new[]{-1f,1f})Box(root,"측면 창",new Vector3(side*(frontWidth*.5f+.04f),4.4f,z),new Vector3(.08f,1.3f,1.3f),glass);
  // Roof: light bar and antenna.
  Box(root,"경광등 적색",new Vector3(-.45f,height+.5f,front+1.2f),new Vector3(.8f,.35f,.4f),M("경광등 적색",new Color(.95f,.1f,.1f)));
  Box(root,"경광등 청색",new Vector3(.45f,height+.5f,front+1.2f),new Vector3(.8f,.35f,.4f),M("경광등 청색",new Color(.1f,.3f,1f)));
  Box(root,"안테나",new Vector3(frontWidth*.35f,height+1.6f,frontDepth*.25f),new Vector3(.06f,3f,.06f),grey);
  // Patrol car beside the building, nose to the road.
  PatrolCar(root,new Vector3(frontWidth*.5f+1.8f,0,front-1.5f));
 }
 static void PatrolCar(Transform parent,Vector3 local){
  var car=new GameObject("순찰차").transform;car.SetParent(parent,false);car.localPosition=local;car.localRotation=Quaternion.identity;
  Material white=M("순찰차 흰색",new Color(.95f,.95f,.96f)),blue=M("순찰차 청색",Blue),glass=M("순찰차 유리",new Color(.1f,.13f,.18f)),tyre=M("순찰차 바퀴",new Color(.08f,.08f,.09f));
  Box(car,"차체",new Vector3(0,.6f,0),new Vector3(1.8f,.7f,4.5f),white);Box(car,"실내",new Vector3(0,1.15f,.2f),new Vector3(1.6f,.55f,2.3f),glass);
  foreach(float side in new[]{-1f,1f})Box(car,"청색 줄",new Vector3(side*.91f,.62f,0),new Vector3(.02f,.18f,4.3f),blue);
  Box(car,"경광등 적",new Vector3(-.3f,1.5f,.2f),new Vector3(.5f,.14f,.25f),M("경광등 적색",new Color(.95f,.1f,.1f)));Box(car,"경광등 청",new Vector3(.3f,1.5f,.2f),new Vector3(.5f,.14f,.25f),M("경광등 청색",new Color(.1f,.3f,1f)));
  foreach(float x in new[]{-.85f,.85f})foreach(float z in new[]{-1.4f,1.4f})Box(car,"바퀴",new Vector3(x,.32f,z),new Vector3(.25f,.64f,.64f),tyre);
 }

 // ---- emergency room ----
 public static void BuildEmergency(MapData map,MapStop dropStop){
  var h=LostItemRules.Hospital;if(EmergencyRoot||map==null||map.buildings==null||h==null||dropStop==null)return;
  // The building face nearest the emergency room point, and its outward normal toward the stop.
  var target=h.V();MapBuilding best=null;Vector3 a=Vector3.zero,c=Vector3.zero;float bd=float.MaxValue;
  foreach(var b in map.buildings){if(b==null||b.points==null||b.points.Length<3)continue;for(int i=0;i<b.points.Length;i++){var p=b.points[i].V();var q=b.points[(i+1)%b.points.Length].V();p.y=q.y=0;var e=q-p;if(e.sqrMagnitude<9)continue;float t=Mathf.Clamp01(Vector3.Dot(target-p,e)/e.sqrMagnitude);float d=(p+e*t-target).sqrMagnitude;if(d<bd){bd=d;best=b;a=p;c=q;}}}
  if(best==null||bd>15*15)return;
  var along=(c-a).normalized;var normal=new Vector3(-along.z,0,along.x);var kerb=dropStop.Kerb;kerb.y=0;if(Vector3.Dot(kerb-a,normal)<0)normal=-normal;
  var foot=a+along*Mathf.Clamp(Vector3.Dot(target-a,along),8,Vector3.Distance(a,c)-8);
  float ground=Ground(foot+normal*1.5f),toKerb=Mathf.Max(3,Vector3.Dot(kerb-foot,normal)-1.2f);
  var root=Frame("응급실 건물 · "+EmergencyRules.HospitalName,new Vector3(foot.x,ground,foot.z),normal);EmergencyRoot=root.gameObject;
  Material white=M("응급센터 백색",White),red=M("응급센터 적색",Red),glass=M("응급센터 유리",Glass),grey=M("응급센터 회색",Grey),lit=M("응급센터 십자 조명",new Color(1f,.92f,.92f));
  // Root local -z points out of the building toward the stop.
  const float W=18;
  Box(root,"1층 외장",new Vector3(0,2.6f,-.18f),new Vector3(W,5.2f,.3f),white);
  Box(root,"적색 띠",new Vector3(0,5.75f,-.22f),new Vector3(W+.2f,1.3f,.36f),red);
  Text(root,"간판",new Vector3(0,5.75f,-.42f),"응급의료센터  EMERGENCY",Color.white,W*.8f,.95f);
  Box(root,"자동문",new Vector3(0,1.3f,-.36f),new Vector3(4.2f,2.6f,.1f),glass);Box(root,"자동문 틀",new Vector3(0,2.66f,-.38f),new Vector3(4.4f,.14f,.12f),grey);Box(root,"자동문 가운데",new Vector3(0,1.3f,-.42f),new Vector3(.06f,2.6f,.04f),grey);
  foreach(float x in new[]{-6.5f,-4.2f,4.2f,6.5f})Box(root,"창",new Vector3(x,2.1f,-.36f),new Vector3(1.8f,2.2f,.08f),glass);
  // Red cross light box at the corner, high on the face.
  var cross=new GameObject("적십자 표지").transform;cross.SetParent(root,false);cross.localPosition=new Vector3(W*.5f-1.2f,8.2f,-.4f);cross.localRotation=Quaternion.identity;
  Box(cross,"바탕",Vector3.zero,new Vector3(2.2f,2.2f,.25f),lit);Box(cross,"가로",new Vector3(0,0,-.14f),new Vector3(1.6f,.5f,.06f),red);Box(cross,"세로",new Vector3(0,0,-.14f),new Vector3(.5f,1.6f,.06f),red);
  // Drive-through canopy over the walk, on four posts, with 응급실 on its front edge.
  float depthOut=Mathf.Min(toKerb,6.5f);
  Box(root,"차양",new Vector3(0,4.4f,-depthOut*.5f-.3f),new Vector3(9f,.35f,depthOut),white);Box(root,"차양 적색 테",new Vector3(0,4.4f,-depthOut-.32f),new Vector3(9.1f,.5f,.1f),red);
  Text(root,"차양 글자",new Vector3(0,4.4f,-depthOut-.4f),"+ 응급실",Color.white,4f,.42f);
  foreach(float x in new[]{-4.2f,4.2f})Box(root,"기둥",new Vector3(x,2.2f,-depthOut+.2f),new Vector3(.3f,4.4f,.3f),grey);
  // Parked ambulance along the face, clear of the doors.
  var amb=new GameObject("구급차").transform;amb.SetParent(root,false);amb.localPosition=new Vector3(-W*.5f+3.2f,0,-2.2f);amb.localRotation=Quaternion.Euler(0,90,0);
  Box(amb,"차체",new Vector3(0,1.15f,0),new Vector3(2.0f,1.9f,5.4f),M("구급차 흰색",new Color(.97f,.97f,.97f)));Box(amb,"앞유리",new Vector3(0,1.5f,2.55f),new Vector3(1.8f,.7f,.3f),glass);
  foreach(float side in new[]{-1f,1f})Box(amb,"적색 줄",new Vector3(side*1.01f,1.0f,0),new Vector3(.02f,.25f,5.2f),red);
  Box(amb,"경광등",new Vector3(0,2.2f,2.0f),new Vector3(1.4f,.18f,.3f),red);
  foreach(float x in new[]{-.9f,.9f})foreach(float z in new[]{-1.8f,1.8f})Box(amb,"바퀴",new Vector3(x,.35f,z),new Vector3(.26f,.7f,.7f),M("구급차 바퀴",new Color(.08f,.08f,.09f)));
 }
}

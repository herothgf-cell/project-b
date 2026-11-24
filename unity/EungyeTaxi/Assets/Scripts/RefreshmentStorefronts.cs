using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

// Eungye snack and coffee shops sit in a building beside their stop pocket, not on a street stall.
// A mapped commercial facade facing the road within reach gets a ground-floor shop front; otherwise a
// small two-storey shop building is placed on clear land behind the sidewalk. Either way a paved path
// leads from the pocket's waiting spot to the shop door. Placement is derived from the map so a moved
// stop keeps a matching building. Local +z of a placement points out of the shop toward the road.
public static class RefreshmentStorefronts {
 public sealed class Placement {
  public string shop;public bool facade;public string buildingId,buildingName;
  public Vector3 front,outward,along,waiting;public float width,depth;
  public Vector3 Door {get{return front;}}
  public Vector3 P(float x,float y,float z){return front+along*x+Vector3.up*y+outward*z;}
 }
 static readonly string[] Excluded={"유치원","어린이집","학교","초등","중학","고등","교회","성당","주민센터","파출소","소방"};
 const float SearchRadius=70,FacadeWidth=6.4f,NewWidth=9,NewDepth=8;
 sealed class Cache {public readonly Dictionary<string,Placement> items=new Dictionary<string,Placement>();}
 static readonly ConditionalWeakTable<MapData,Cache> cache=new ConditionalWeakTable<MapData,Cache>();

 sealed class Local {
  public readonly List<MapRoad> roads=new List<MapRoad>();
  public readonly List<MapPoint[]> areas=new List<MapPoint[]>();
  public readonly List<MapBuilding> buildings=new List<MapBuilding>();
  public readonly List<Vector3[]> lanes=new List<Vector3[]>();
 }
 static bool Near(MapPoint[] points,Vector3 p){if(points==null)return false;foreach(var q in points)if(Mathf.Abs(q.x-p.x)<SearchRadius&&Mathf.Abs(q.z-p.z)<SearchRadius)return true;return false;}
 static Local Gather(MapData map,Vector3 p){
  var local=new Local();
  foreach(var r in map.roads??new MapRoad[0])if(Near(r.points,p))local.roads.Add(r);
  foreach(var a in map.areas??new MapArea[0])if(a.kind!="land"&&a.points!=null&&a.points.Length>=3&&Near(a.points,p))local.areas.Add(a.points);
  foreach(var b in map.buildings??new MapBuilding[0])if(b.points!=null&&b.points.Length>=3&&Near(b.points,p)&&!SceneBuildingVisibility.Hidden(map,b.id))local.buildings.Add(b);
  return local;
 }
 static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){a.y=b.y=p.y=0;var d=b-a;float t=d.sqrMagnitude<1e-6f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);return Vector3.Distance(p,a+d*t);}
 static float RingDistance(MapPoint[] ring,Vector3 p){float best=float.MaxValue;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++)best=Mathf.Min(best,SegmentDistance(p,ring[j].V(),ring[i].V()));return best;}
 static bool Blocked(Local local,Vector3 p,float margin){
  foreach(var r in local.roads)for(int i=1;i<r.points.Length;i++)if(SegmentDistance(p,r.points[i-1].V(),r.points[i].V())<r.width*.5f+margin)return true;
  foreach(var a in local.areas)if(ProjectMapScene.Contains(a,p))return true;
  foreach(var b in local.buildings)if(ProjectMapScene.Contains(b.points,p)||RingDistance(b.points,p)<margin)return true;
  foreach(var lane in local.lanes)for(int i=1;i<lane.Length;i++)if(SegmentDistance(p,lane[i-1],lane[i])<2+margin)return true;
  return false;
 }
 public static Placement For(MapData map,MapStop stop,string shop){
  if(map==null||stop==null)return null;
  var items=cache.GetOrCreateValue(map).items;
  Placement placement;
  if(items.TryGetValue(shop,out placement))return placement;
  var forward=Flat(stop.Direction);var waiting=stop.Passenger;waiting.y=Ground(stop);
  placement=Facade(map,waiting,TrafficSystem.Right(forward),shop)??NewBuilding(map,waiting,forward,TrafficSystem.Right(forward),shop,null);
  return Remember(items,shop,placement);
 }
 // A driver restaurant has a parking bay in its own lot instead of a kerb pocket. Its mapped building
 // beside the lot gets the shop front on the wall that faces the bay. Without such a building the shop
 // goes on clear land next to the lot (ahead of the parked taxi first, then either side), keeping the
 // lot and its service lanes clear.
 public static Placement ForLot(MapData map,ProjectMapObject lot,string shop){
  if(map==null||lot==null)return null;
  var items=cache.GetOrCreateValue(map).items;
  Placement placement;
  if(items.TryGetValue(shop,out placement))return placement;
  var forward=Flat(lot.Direction);var right=TrafficSystem.Right(forward);var waiting=lot.Position;
  placement=Facade(map,waiting,Vector3.zero,shop,true);
  if(placement==null)foreach(var side in new[]{new[]{-right,forward},new[]{forward,right},new[]{-forward,-right}}){
   placement=NewBuilding(map,waiting,side[0],side[1],shop,lot.points);
   if(placement!=null)break;
  }
  return Remember(items,shop,placement);
 }
 static Placement Remember(Dictionary<string,Placement> items,string shop,Placement placement){
  items[shop]=placement;
  if(placement==null)Debug.LogWarning("REFRESHMENT_STOREFRONT shop="+shop+" placement=none");
  else Debug.Log("REFRESHMENT_STOREFRONT shop="+shop+" mode="+(placement.facade?"facade building="+placement.buildingId+" "+placement.buildingName:"new")+" door="+placement.Door.ToString("F1")+" path="+Vector3.Distance(placement.waiting,placement.Door).ToString("F1"));
  return placement;
 }
 static Vector3 Flat(Vector3 v){v.y=0;return v.normalized;}
 static float Ground(MapStop stop){return stop.V().y;}
 // A road-facing wall of a non-civic building close behind the waiting spot.
 // facing: accept any wall that looks at the waiting spot, and prefer a building mapped as the restaurant.
 static readonly string[] Diner={"밥집","식당"};
 static Placement Facade(MapData map,Vector3 waiting,Vector3 outward,string shop,bool facing=false){
  Placement best=null;float bestScore=float.MaxValue;
  var buildings=Gather(map,waiting).buildings;
  if(facing&&buildings.Any(b=>Diner.Any(word=>(b.name??"").Contains(word))))buildings=buildings.Where(b=>Diner.Any(word=>(b.name??"").Contains(word))).ToList();
  foreach(var b in buildings){
   if(b.height<3.5f||Excluded.Any(word=>(b.name??"").Contains(word)))continue;
   var ring=b.points;
   for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){
    var a=ring[j].V();var c=ring[i].V();a.y=c.y=waiting.y;float length=Vector3.Distance(a,c);
    if(length<FacadeWidth+1.1f)continue;
    var tangent=(c-a)/length;var normal=TrafficSystem.Right(tangent);
    if(ProjectMapScene.Contains(ring,(a+c)*.5f+normal*.3f))normal=-normal;
    if(!facing&&Vector3.Dot(normal,outward)>-.75f)continue;
    float u=Mathf.Clamp(Vector3.Dot(waiting-a,tangent),FacadeWidth*.5f+.4f,length-FacadeWidth*.5f-.4f);
    var front=a+tangent*u;var offset=waiting-front;
    float distance=Vector3.Dot(offset,normal),lateral=Mathf.Abs(Vector3.Dot(offset,tangent));
    if(distance<1||distance>(facing?22:16)||lateral>8||distance+lateral>=bestScore)continue;
    bestScore=distance+lateral;
    best=new Placement{shop=shop,facade=true,buildingId=b.id,buildingName=b.name,front=front,outward=normal,along=TrafficSystem.Right(normal),waiting=waiting,width=FacadeWidth,depth=0};
   }
  }
  return best;
 }
 // A compact shop building on the first clear land behind the sidewalk, shifted along the road if needed.
 static Placement NewBuilding(MapData map,Vector3 waiting,Vector3 forward,Vector3 outward,string shop,MapPoint[] lot){
  var local=Gather(map,waiting);
  if(lot!=null){
   local.areas.Add(lot);
   foreach(var e in map.hdEdges??new HdEdge[0])if(!e.disabled&&Near(e.points,waiting))local.lanes.Add(e.points.Select(q=>q.V()).ToArray());
  }
  foreach(float push in new[]{0f,1,2,3,4,5,6})foreach(float shift in new[]{0f,2,-2,4,-4,6,-6,8,-8,10,-10}){
   var origin=waiting+forward*shift;float clear=-1;
   for(int k=4;k<40;k++)if(!Blocked(local,origin+outward*(k*.5f),.6f)){clear=k*.5f;break;}
   if(clear<0)continue;
   float frontDistance=clear+.8f+push;bool ok=true;
   for(int i=0;i<=10&&ok;i++)for(int j=0;j<=8&&ok;j++)
    if(Blocked(local,origin+forward*((i/10f-.5f)*NewWidth)+outward*(frontDistance+j/8f*NewDepth),.4f))ok=false;
   if(!ok)continue;
   return new Placement{shop=shop,facade=false,front=origin+outward*frontDistance,outward=-outward,along=forward,waiting=waiting,width=NewWidth,depth=NewDepth};
  }
  return null;
 }

 public static bool Draw(MapData map,Transform parent,MapStop stop,string shop,bool food){
  var p=For(map,stop,shop);
  if(p==null)return false;
  Build(parent,p,shop,food,"[F] 주문",p.waiting);return true;
 }
 // The driver restaurant uses the same shop-front design as the snack shop (test feedback 2026-11-24).
 public static bool DrawDiner(MapData map,Transform parent,ProjectMapObject lot,string shop){
  var p=ForLot(map,lot,shop);
  if(p==null)return false;
  // The path starts at the lot edge nearest the door, not under the parked taxi.
  var toDoor=Flat(p.P(0,0,2.9f)-p.waiting);
  Build(parent,p,shop,true,null,p.waiting+toDoor*3.4f);return true;
 }
 static void Build(Transform parent,Placement p,string shop,bool food,string caption,Vector3 pathStart){
  var root=new GameObject(shop+" 상가");root.transform.SetParent(parent,false);
  root.transform.SetPositionAndRotation(p.front,Quaternion.LookRotation(p.outward));
  var t=root.transform;
  var accent=WorldBuilder.Mat(shop+" 간판",food?new Color(.74f,.20f,.12f):new Color(.10f,.36f,.29f));
  var cream=WorldBuilder.Mat(shop+" 간판 글자판",new Color(.97f,.91f,.78f));
  var frame=WorldBuilder.Mat("상가 창틀",new Color(.17f,.18f,.19f));
  var glass=WorldBuilder.Mat(shop+" 실내 조명",food?new Color(.95f,.80f,.52f):new Color(.92f,.78f,.58f));
  glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",food?new Color(.55f,.40f,.20f):new Color(.50f,.36f,.20f));glass.SetFloat("_Glossiness",.8f);
  var doorGlass=WorldBuilder.Mat("상가 출입문 유리",new Color(.22f,.30f,.33f));doorGlass.SetFloat("_Glossiness",.85f);
  var paving=WorldBuilder.Mat("상가 진입로 보도블록",new Color(.62f,.55f,.48f));
  var step=WorldBuilder.Mat("상가 출입 계단",new Color(.74f,.73f,.70f));
  float w=p.facade?p.width:7.4f,half=w*.5f;
  if(!p.facade){
   var wall=WorldBuilder.Mat(shop+" 건물 외벽",food?new Color(.86f,.80f,.70f):new Color(.80f,.78f,.72f));
   var roof=WorldBuilder.Mat("상가 지붕 난간",new Color(.45f,.44f,.42f));
   WorldBuilder.Box("상가 건물",new Vector3(0,3.5f,-p.depth*.5f),new Vector3(p.width,7,p.depth),wall,t);
   WorldBuilder.Box("옥상 난간",new Vector3(0,7.15f,-p.depth*.5f),new Vector3(p.width+.2f,.3f,p.depth+.2f),roof,t,false);
   foreach(float x in new[]{-2.4f,2.4f}){
    WorldBuilder.Box("2층 창문",new Vector3(x,5.0f,.03f),new Vector3(2.6f,1.4f,.06f),frame,t,false);
    WorldBuilder.Box("2층 창유리",new Vector3(x,5.0f,.07f),new Vector3(2.4f,1.2f,.04f),doorGlass,t,false);
   }
  }
  // Shop front: bulkhead, two lit display windows, a glazed double door, fascia sign and awning.
  WorldBuilder.Box("하단 벽",new Vector3(0,.25f,.05f),new Vector3(w,.5f,.1f),accent,t,false);
  foreach(float sign in new[]{-1f,1f}){
   float x=sign*(half+1.05f)*.5f,pane=half-1.05f;
   WorldBuilder.Box("진열창",new Vector3(x,1.55f,.06f),new Vector3(pane,2.1f,.04f),glass,t,false);
   WorldBuilder.Box("진열창 틀",new Vector3(sign*(half-.04f),1.5f,.08f),new Vector3(.08f,3,.1f),frame,t,false);
   WorldBuilder.Box("문틀",new Vector3(sign*1.0f,1.25f,.09f),new Vector3(.1f,2.5f,.12f),frame,t,false);
   WorldBuilder.Box("출입문",new Vector3(sign*.48f,1.15f,.05f),new Vector3(.9f,2.3f,.04f),doorGlass,t,false);
   WorldBuilder.Box("문 손잡이",new Vector3(sign*.1f,1.1f,.1f),new Vector3(.04f,.6f,.05f),frame,t,false);
  }
  WorldBuilder.Box("진열창 상단 틀",new Vector3(0,2.64f,.08f),new Vector3(w,.08f,.1f),frame,t,false);
  WorldBuilder.Box("간판",new Vector3(0,3.15f,.12f),new Vector3(w,.85f,.2f),accent,t,false);
  WorldBuilder.Box("간판 글자판",new Vector3(0,3.15f,.23f),new Vector3(w*.72f,.6f,.03f),cream,t,false);
  var nameplate=new GameObject("간판 상호");nameplate.transform.SetParent(t,false);
  nameplate.transform.localPosition=new Vector3(0,3.15f,.26f);nameplate.transform.localRotation=Quaternion.Euler(0,180,0);
  var signText=nameplate.AddComponent<TextMesh>();signText.text=shop;signText.anchor=TextAnchor.MiddleCenter;signText.color=food?new Color(.55f,.10f,.05f):new Color(.05f,.25f,.20f);
  KoreanText.Fit(signText,w*.66f,.5f);
  var awning=WorldBuilder.Box("차양",new Vector3(0,2.78f,.62f),new Vector3(w-.2f,.08f,1.25f),accent,t,false);
  awning.transform.localRotation=Quaternion.Euler(-14,0,0);
  for(float x=-half+.5f;x<half-.2f;x+=1){var stripe=WorldBuilder.Box("차양 줄무늬",new Vector3(x,2.79f,.62f),new Vector3(.45f,.085f,1.26f),cream,t,false);stripe.transform.localRotation=Quaternion.Euler(-14,0,0);}
  // Entrance: a step and landing at the door, a forecourt and a path back to the waiting spot.
  WorldBuilder.Box("출입 계단",new Vector3(0,.07f,.75f),new Vector3(2.4f,.14f,1.4f),step,t,false);
  WorldBuilder.Box("상가 앞 포장",new Vector3(0,.04f,1.6f),new Vector3(w,.08f,2.6f),paving,t,false);
  var board=WorldBuilder.Box("입간판",new Vector3(half-.7f,.55f,2.0f),new Vector3(.6f,1.0f,.08f),accent,t,false);board.transform.localRotation=Quaternion.Euler(-8,25,0);
  var entry=p.P(0,0,2.9f);
  PathSegment(t,pathStart,entry,paving);
  if(caption==null)return;
  var label=new GameObject("주문 안내");label.transform.SetParent(t,false);
  label.transform.position=p.P(0,4.3f,.6f);
  var text=label.AddComponent<TextMesh>();text.text=caption;
  text.fontSize=44;text.characterSize=.052f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
  KoreanText.Apply(text);label.AddComponent<FaceCamera>().hideBeyond=45;
 }
 static void PathSegment(Transform parent,Vector3 a,Vector3 b,Material material){
  a.y=b.y=parent.position.y;float length=Vector3.Distance(a,b);if(length<.2f)return;
  var path=WorldBuilder.Box("상가 진입로",Vector3.zero,new Vector3(1.6f,.08f,length+.8f),material,parent,false);
  path.transform.SetPositionAndRotation((a+b)*.5f+Vector3.up*.04f,Quaternion.LookRotation(b-a));
 }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Guarded reviewed-map authoring. Preview never saves; Apply is explicit only.
// User confirmed L+U / S / S / S / separated R at map31, both approaches.
// Both U conditions user-confirmed: left-turn OR pedestrian signal green.
// New geometry is explicitly supplied game geometry, never restored NGII data.
public static class Green31MapEdit {
 public const string Prefix="draft:green31:";
 public const string Junction="J2145_15970";
 public const string RequiredHash="38bad072a26d23185eb9f902ea347eb84dbe34c7e554b22637916ee52645f8af";
 public const int RequiredRevision=24;
 public static readonly string[][] Incoming={
  new[]{"local:4258:13644:2301:15817:part:2","local:4302:13688:2349:15857:part:3","local:4347:13732:2398:15897:part:2"},
  new[]{"local:967:17150:1989:16125:part:2","local:929:17101:1939:16087:part:3","local:891:17051:1890:16049:part:2"}};
 static readonly string[] Left={"join:local:4258:13644:2301:15817:-1","join:local:967:17150:1989:16125:-1"};
 static readonly string[] Right={"join:local:4347:13732:2398:15897:1","join:local:891:17051:1890:16049:1"};
 [Serializable] public class Parameters {public Approach A,B;}
 [Serializable] public class Curve {public MapPoint[] points;}
 [Serializable] public class Approach {
  // Absolute directed-lineage source stations, one per existing terminal lane.
  // All must be strictly inside that terminal segment. No inferred default.
  public float[] sourceStations;
  public Curve[] throughTails;
  public Curve leftPocket,rightSlip,leftJoin,rightJoin;
  public MapPoint[] island;
  public RoadMapFacility crosswalk;
  public float laneWidth,islandHeight;
  public string anchorEvidence,geometryEstimateNote;
  // Explicit scoped scene replacement permits exact sidewalk clipping/ribbon
  // correction. Supply complete replacements/fragments; no blanket disable.
  public string[] replaceSceneIds=new string[0];
  public ProjectMapObject[] replacementSceneObjects=new ProjectMapObject[0];
  public ProjectMapObject[] roadSurfaces=new ProjectMapObject[0];
  public bool requestUturn; // Deliberately rejected until condition authoring is reviewed.
  public string uturnCondition=""; // Later evidence must identify a supported conditional rule.
 }
 static T Copy<T>(T v){return JsonUtility.FromJson<T>(JsonUtility.ToJson(v));}
 static void Require(bool value,string why){if(!value)throw new ArgumentException("GREEN31_ANCHORS: "+why);}
 static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
 static void Points(MapPoint[] p,int count,string label){Require(p!=null&&p.Length>=count,label+" missing");Require(p.All(x=>x!=null&&Finite(x.x)&&Finite(x.z)&&Finite(x.elevation)),label+" nonfinite");}
 static float Length(RoadMapLane l){float n=0;for(int i=1;i<l.points.Length;i++)n+=Vector3.Distance(l.points[i-1].V(l.heights!=null&&l.heights.Length>0?l.heights[i-1]:0),l.points[i].V(l.heights!=null&&l.heights.Length>0?l.heights[i]:0));return n;}
 static void Near(MapPoint a,Vector3 b,string label){Require(Vector3.Distance(a.V(a.elevation),b)<.02f,label+" endpoint mismatch");}
 static RoadMapEvidence Evidence(Approach a){return new RoadMapEvidence{state="observed",url=a.anchorEvidence,month="2026-05",checkedAt="2026-11-13",appearanceConfirmed=true,ruleConfirmed=a.requestUturn,note="User confirmed both L+U/S/S/S/separated R and U permitted on left-turn OR pedestrian green on2026-11-13. Small supplementary text was not independently legible in panorama; user confirmation is the primary condition evidence. Supplied game geometry: "+a.geometryEstimateNote};}
 static void Geometry(RoadMapLane l,Curve c,Approach a){l.points=Copy(c).points;l.heights=l.points.Select(p=>p.elevation).ToArray();l.width=a.laneWidth;l.evidence=Evidence(a);}
 static void Validate(RoadMapDocument d,Approach a,int side){
  string name=side==0?"A (from map⑤)":"B (NW boundary)";
  Require(a!=null,name+" required");Require(a.requestUturn?a.uturnCondition==UturnMovement.LeftOrPedestrianGreen:string.IsNullOrEmpty(a.uturnCondition),name+" only user-confirmed conditional U rule is supported");
  Require(!string.IsNullOrWhiteSpace(a.anchorEvidence)&&!string.IsNullOrWhiteSpace(a.geometryEstimateNote),name+" anchor evidence and game estimate note required");
  Require(a.sourceStations!=null&&a.sourceStations.Length==3&&a.throughTails!=null&&a.throughTails.Length==3,name+" three directed stations and tails required");
  Require(Finite(a.laneWidth)&&a.laneWidth>=3&&a.laneWidth<=4&&Finite(a.islandHeight)&&a.islandHeight>0&&a.islandHeight<=.3f,name+" explicit game dimensions required");
  for(int i=0;i<3;i++){
   var l=d.lanes.Single(x=>x.id==Incoming[side][i]&&!x.disabled);float local=a.sourceStations[i]-l.sourceOffset;
   Require(Finite(local)&&local>.02f&&local<Length(l)-.02f,name+" station must lie inside terminal lane "+l.id);
   Require(a.throughTails[i]!=null,name+" tail required");Points(a.throughTails[i].points,2,name+" tail");
   Near(a.throughTails[i].points[0],LaneStationAuthoring.SampleAt(l,local),name+" split");
  }
  foreach(var c in new[]{a.leftPocket,a.rightSlip,a.leftJoin,a.rightJoin}){Require(c!=null,name+" pocket/slip/join curves required");Points(c.points,2,name+" curve");}
  Near(a.leftPocket.points[0],a.throughTails[0].points[0].V(a.throughTails[0].points[0].elevation),name+" left split");
  Near(a.rightSlip.points[0],a.throughTails[2].points[0].V(a.throughTails[2].points[0].elevation),name+" slip split");
  for(int i=0;i<2;i++){
   var join=d.lanes.Single(l=>l.id==(i==0?Left[side]:Right[side])&&!l.disabled);var curve=i==0?a.leftJoin:a.rightJoin;var approach=i==0?a.leftPocket:a.rightSlip;
   Near(curve.points[0],approach.points.Last().V(approach.points.Last().elevation),name+" join start");
   var target=d.lanes.Single(l=>l.id==join.next.Single());Near(curve.points.Last(),LaneStationAuthoring.SampleAt(target,0),name+" retained exit");
  }
  Points(a.island,3,name+" physical island");Require(a.crosswalk!=null&&a.crosswalk.kind=="crosswalk"&&a.crosswalk.width>0&&a.crosswalk.length>0,name+" slip crosswalk required");
  Require(a.replaceSceneIds!=null&&a.replacementSceneObjects!=null&&a.roadSurfaces!=null,name+" scene arrays cannot be null");
  foreach(string id in a.replaceSceneIds)Require(d.scene.objects.Count(o=>o.id==id)==1,name+" replacement object missing "+id);
  foreach(var road in a.roadSurfaces){Require(road.kind=="road"&&road.enabled&&road.width>0,name+" explicit road surface required");Points(road.points,2,name+" surface");}
 }
 public static RoadMapDocument Preview(ProjectMapPackage p,Parameters anchors){
  Require(p!=null&&p.document!=null&&p.document.scene!=null,"package/scene required");Require(anchors!=null,"A/B parameters required");
  Require(!p.document.lanes.Any(l=>l.id.StartsWith(Prefix)),"already authored");Validate(p.document,anchors.A,0);Validate(p.document,anchors.B,1);
  Require(!anchors.A.replaceSceneIds.Intersect(anchors.B.replaceSceneIds).Any(),"A/B scene replacement overlap");
  var d=p.document.Copy();
  for(int side=0;side<2;side++){
   var a=side==0?anchors.A:anchors.B;string prefix=Prefix+(side==0?"A":"B")+":";
   var tails=new RoadMapLane[3];
   for(int i=0;i<3;i++){
    string tail=LaneStationAuthoring.SplitLineageAt(d,Incoming[side][i],a.sourceStations[i]);tails[i]=d.lanes.Single(l=>l.id==tail);
    string stable="editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Incoming[side][i])).TrimEnd('=').Replace('+','-').Replace('/','_')+":green31-through";string oldNode=tails[i].fromNode;tails[i].id=stable;tails[i].fromNode=prefix+"split:"+i;
    foreach(var lane in d.lanes){lane.next=lane.next.Select(id=>id==tail?stable:id).ToArray();if(lane.toNode==oldNode)lane.toNode=tails[i].fromNode;}
    foreach(var f in d.facilities)if(f.laneId==tail)f.laneId=stable;foreach(var o in d.scene.objects)if(o.laneId==tail)o.laneId=stable;
    foreach(var program in d.signals)foreach(var group in program.groups)group.laneIds=group.laneIds.Select(id=>id==tail?stable:id).ToArray();
    Geometry(tails[i],a.throughTails[i],a);tails[i].number=i+2;
    tails[i].next=tails[i].next.Where(id=>id!=Left[side]&&id!=Right[side]).ToArray();
    foreach(string id in tails[i].next){var join=d.lanes.Single(l=>l.id==id);Require(join.turn==0,"through lane has unexpected movement "+id);Geometry(join,Join(a.throughTails[i],d.lanes.Single(l=>l.id==join.next.Single())),a);}
   }
   var added=new List<RoadMapLane>();
   for(int k=0;k<2;k++){
    int i=k==0?0:2;var pocket=Copy(tails[i]);pocket.id=prefix+(k==0?"left":"right");pocket.sourceId=pocket.id;pocket.sourceOffset=0;pocket.number=k==0?1:5;pocket.toNode=pocket.id+":end";
    pocket.next=new[]{k==0?Left[side]:Right[side]};pocket.turn=0;pocket.change=0;Geometry(pocket,k==0?a.leftPocket:a.rightSlip,a);
    var head=d.lanes.Single(l=>l.id==Incoming[side][i]);head.next=head.next.Concat(new[]{pocket.id}).ToArray();
    var join=d.lanes.Single(l=>l.id==pocket.next[0]);join.fromNode=pocket.toNode;Geometry(join,k==0?a.leftJoin:a.rightJoin,a);added.Add(pocket);
   }
   d.lanes=d.lanes.Concat(added).ToArray();
   var island=new ProjectMapObject{id=prefix+"island",kind="sidewalk",name="Green31 pedestrian island (game estimate)",points=Copy(new Curve{points=a.island}).points,height=a.islandHeight,enabled=true};
   var extra=a.replacementSceneObjects.Concat(a.roadSurfaces).Select(Copy).ToList();extra.Add(island);
   foreach(var tail in tails)foreach(string id in tail.next){var join=d.lanes.Single(l=>l.id==id);extra.Add(new ProjectMapObject{id=prefix+"road:"+id,kind="road",name="Green31 adjusted straight connector pavement",enabled=true,oneWay=true,suppressPaint=true,width=4.2f,lanes=1,points=join.points.Select(p=>P(p.V())).ToArray()});}
   Require(extra.Select(o=>o.id).Distinct().Count()==extra.Count,"duplicate supplied scene IDs");
   var retained=d.scene.objects.Where(o=>!a.replaceSceneIds.Contains(o.id)).ToArray();Require(!retained.Select(o=>o.id).Intersect(extra.Select(o=>o.id)).Any(),"scene ID collision");
   d.scene.objects=retained.Concat(extra).ToArray();var crossing=Copy(a.crosswalk);crossing.id=prefix+"crosswalk";crossing.junctionId="";crossing.laneId=prefix+"right";crossing.evidence=Evidence(a);
   d.facilities=d.facilities.Concat(new[]{crossing}).ToArray();
  }
  for(int side=0;side<2;side++){var a=side==0?anchors.A:anchors.B;if(a.requestUturn)AddConditionalUturn(p,d,a,side);}
  return d;
 }
 static void AddConditionalUturn(ProjectMapPackage p,RoadMapDocument d,Approach a,int side){
  string prefix=Prefix+(side==0?"A":"B")+":";var from=d.lanes.Single(l=>l.id==prefix+"left");
  // The inner exit is only6.25m across in this imported map, below the
  // existing9.9m U-turn constraint. The unchanged second exit is12.5m across;
  // this receiving lane and curve are explicit game geometry estimates.
  string targetId=side==0?"local:2205:15737:4168:13557":"local:2089:16200:1044:17249";var target=d.lanes.Single(l=>l.id==targetId);var points=UturnMovement.Geometry(from,target);
  var join=Copy(from);join.id="join:"+prefix+"uturn";join.fromNode=from.toNode;join.toNode=target.fromNode;join.turn=UturnMovement.Turn;join.change=0;join.number=target.number;join.points=points.Select(P).ToArray();join.heights=points.Select(q=>q.y).ToArray();join.next=new[]{target.id};join.evidence=Evidence(a);join.evidence.note+=" U curve uses unchanged second opposing exit,12.5m across, because imported inner exit6.25m fails minimum turning geometry. Receiving lane and curve are game estimates.";from.next=from.next.Concat(new[]{join.id}).ToArray();d.lanes=d.lanes.Concat(new[]{join}).ToArray();
  var heading=(from.points.Last().V()-from.points[from.points.Length-2].V()).normalized;var rule=p.baseline.junctionRules.Single(j=>j.id==Junction);var axisDirection=new Vector3(rule.dx,0,rule.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(heading,axisDirection))>.7f?0:1,sign=Vector3.Dot(heading,axis==0?axisDirection:TrafficSystem.Right(axisDirection))>=0?1:-1;var position=from.points.Last().V()-heading*7-TrafficSystem.Right(heading)*2.3f;
  d.facilities=d.facilities.Concat(new[]{new RoadMapFacility{id=prefix+"uturn-condition",kind="sign",type=a.uturnCondition,laneId=from.id,junctionId=Junction,rule=true,axis=axis,sign=sign,x=position.x,z=position.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,height=3,text="유턴 · 좌회전 또는 보행 신호 시",evidence=Evidence(a)}}).ToArray();
  d.scene.objects=d.scene.objects.Concat(new[]{new ProjectMapObject{id=prefix+"uturn-pavement",kind="road",name="Green31 conditional U-turn swept pavement (game estimate)",enabled=true,width=4.2f,lanes=1,oneWay=true,suppressPaint=true,points=points.Select(P).ToArray()}}).ToArray();
 }
 public static Parameters DefaultParameters(ProjectMapPackage p){
  Require(p.document.revision==RequiredRevision&&p.contentHash==RequiredHash,"requires exact reviewed r"+RequiredRevision+" before generating estimated approach geometry");
  return new Parameters{A=Estimate(p.document,0),B=Estimate(p.document,1)};
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var before=ProjectMapStore.Load(root);
  // DefaultParameters owns the exact revision/hash gate; no generated geometry
  // is written against a newer or independently edited map.
  var anchors=DefaultParameters(before);string untouched=JsonUtility.ToJson(before);var after=Preview(before,anchors);
  Require(untouched==JsonUtility.ToJson(before),"preview mutated original package");Green31MapTests.Validate(before,after,anchors);
  Require(untouched==JsonUtility.ToJson(before),"validation mutated original package");after.revision=before.document.revision+1;after.projectRevision="";
  var result=ProjectMapPackage.Create(before.baseline,after);Require(result.baselineHash==before.baselineHash,"baseline hash changed");
  ProjectMapStore.Save(root,result,token);var reloaded=ProjectMapStore.Load(root);Require(reloaded.contentHash==result.contentHash&&reloaded.baselineHash==before.baselineHash&&reloaded.document.revision==before.document.revision+1,"saved/reloaded package identity mismatch");
  Green31MapTests.Validate(before,reloaded.document,anchors);ProjectMapBuild.Prepare();
  Debug.Log("GREEN31_MAP_APPLIED revision="+reloaded.document.revision+" contentHash="+reloaded.contentHash+" baselineHash="+reloaded.baselineHash+" before-and-reload-validated");
 }
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z,elevation=p.y};}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());}
 static Curve OffsetTail(AssistedRoadNetwork.Path path,float start,float end,float shift){
  var pts=new List<MapPoint>();for(float at=start;at<end-.001f;at+=1){float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((at-start)/25));pts.Add(P(path.Position(at)+TrafficSystem.Right(path.Tangent(at))*shift*blend));}
  pts.Add(P(path.Position(end)+TrafficSystem.Right(path.Tangent(end))*shift*Mathf.SmoothStep(0,1,Mathf.Clamp01((end-start)/25))));return new Curve{points=pts.ToArray()};
 }
 static Curve Join(Curve from,RoadMapLane target){var start=from.points.Last().V();var f=(start-from.points[from.points.Length-2].V()).normalized;var end=target.points[0].V();var exit=(target.points[1].V()-end).normalized;float handle=Vector3.Distance(start,end)/3;var pts=new MapPoint[65];for(int i=0;i<pts.Length;i++){float t=i/(float)(pts.Length-1),u=1-t;pts[i]=P(u*u*u*start+3*u*u*t*(start+f*handle)+3*u*t*t*(end-exit*handle)+t*t*t*end);}return new Curve{points=pts};}
 static Approach Estimate(RoadMapDocument d,int side){
  string prefix=Prefix+(side==0?"A":"B")+":";var originals=Incoming[side].Select(id=>d.lanes.Single(l=>l.id==id)).ToArray();var paths=originals.Select(Path).ToArray();
  var a=new Approach{sourceStations=new float[3],throughTails=new Curve[3],laneWidth=3.5f,islandHeight=.14f,requestUturn=true,uturnCondition=UturnMovement.LeftOrPedestrianGreen,anchorEvidence="https://map.naver.com/p?c=19,0,0,0,adh&p="+(side==0?"tU91p2FO7I175J4EaFO9aw,-33,7,28":"HDZ7JI96yIhjVH-QWwJdmQ,162,5,28")+",Float",geometryEstimateNote="User-confirmed map31 both approaches L+U/S/S/S/separated R with pedestrian island; exact roadview taper stations not measured. Local75m approach,25m transition,3.5m lanes,12x1.5m pedestrian island and10m earlier slip endpoint are game estimates. Historical2024 comparisons A GtUf0IAYpOPUEXSioSZxjQ / B fMQq_Djewex12cLwMdUoug; U condition primary evidence is user confirmation."};
  for(int i=0;i<3;i++){a.sourceStations[i]=originals[i].sourceOffset+paths[i].length-75;a.throughTails[i]=OffsetTail(paths[i],paths[i].length-75,paths[i].length,new[]{3.5f,.75f,-2f}[i]);}
  a.leftPocket=OffsetTail(paths[0],paths[0].length-75,paths[0].length,0);a.rightSlip=OffsetTail(paths[2],paths[2].length-75,paths[2].length-10,3.5f);
  a.leftJoin=Join(a.leftPocket,d.lanes.Single(l=>l.id==d.lanes.Single(j=>j.id==Left[side]).next.Single()));a.rightJoin=Join(a.rightSlip,d.lanes.Single(l=>l.id==d.lanes.Single(j=>j.id==Right[side]).next.Single()));
  var islandCentre=paths[2].Position(paths[2].length-23);var islandDir=paths[2].Tangent(paths[2].length-23);islandCentre+=TrafficSystem.Right(islandDir)*.75f;a.island=ProjectMapScene.Rectangle(islandCentre,islandDir,1.5f,12);
  var slipPath=new AssistedRoadNetwork.Path(a.rightSlip.points.Select(p=>p.V()).ToArray());var cross=slipPath.Position(slipPath.length-13);var heading=slipPath.Tangent(slipPath.length-13);a.crosswalk=new RoadMapFacility{kind="crosswalk",width=5.5f,length=3,scale=1,x=cross.x,z=cross.z,yaw=Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg,rule=false};
  var surfaces=new List<ProjectMapObject>();int index=0;foreach(var curve in a.throughTails.Concat(new[]{a.leftPocket,a.rightSlip,a.leftJoin,a.rightJoin}))surfaces.Add(new ProjectMapObject{id=prefix+"road:"+(index++),kind="road",name="Green31 local estimated approach pavement",enabled=true,oneWay=true,suppressPaint=true,width=4.2f,lanes=1,points=curve.points.Select(p=>P(p.V())).ToArray()});
  a.roadSurfaces=surfaces.ToArray();RelocateCurbs(d,a,paths[2],prefix);return a;
 }
 static void RelocateCurbs(RoadMapDocument d,Approach a,AssistedRoadNetwork.Path outer,string prefix){
  var paths=a.throughTails.Concat(new[]{a.leftPocket,a.rightSlip,a.leftJoin,a.rightJoin}).Select(c=>new AssistedRoadNetwork.Path(c.points.Select(p=>p.V()).ToArray())).ToArray();var ids=new List<string>();var replacements=new List<ProjectMapObject>();
  foreach(var o in d.scene.objects.Where(o=>o.enabled&&o.kind=="sidewalk"&&o.id.StartsWith("sidewalk:generated:")&&o.points.Length>=3)){
   bool touches=o.points.Any(p=>paths.Any(path=>{float e;path.Project(p.V(),out e);return e<2.2f;}));if(!touches)continue;
   var mean=o.points.Aggregate(Vector3.zero,(s,p)=>s+p.V())/o.points.Length;float error;float at=outer.Project(mean,out error);if(at<outer.length-80)continue;var right=TrafficSystem.Right(outer.Tangent(at));
   ProjectMapObject replacement=null;for(float offset=.5f;offset<=10;offset+=.5f){var points=o.points.Select(p=>P(p.V()+right*offset)).ToArray();if(points.Any(p=>paths.Any(path=>{float e;path.Project(p.V(),out e);return e<2.3f;})))continue;
    if(points.Any(p=>d.scene.objects.Any(b=>b.enabled&&(b.kind=="building"||b.kind=="water")&&ProjectMapScene.Contains(b.points,p.V()))))continue;
    replacement=Copy(o);replacement.points=points;break;
   }Require(replacement!=null,"specific curb needs field-aligned design "+o.id);ids.Add(o.id);replacements.Add(replacement);
  }a.replaceSceneIds=ids.ToArray();a.replacementSceneObjects=replacements.ToArray();
 }
}

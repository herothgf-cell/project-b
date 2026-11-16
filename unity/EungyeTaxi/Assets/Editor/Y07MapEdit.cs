using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Explicit preview: E3 through lanes, a right-entry merge auxiliary and an
// inner left pocket; W4. Camera stations approximate, dimensions game estimates.
public static class Y07MapEdit {
 public const string Prefix="reviewed:Y07:";
 public const string RequiredHash="aadeeb2f24c026a000b393aa938b9f625bd3f0d9db4a8a85d79a5ac13408aa45";
 public const string WInner="local:10455:15872:7443:14880",WOuter="local:10442:15920:7425:14927",EInner="local:7461:14834:10469:15824",EOuter="local:7479:14787:10482:15776";
 public const string W2=Prefix+"west:2",W3=Prefix+"west:3",E2=Prefix+"east:2";
 public const string Aux=Prefix+"east:right-merge",Pocket=Prefix+"east:left-pocket",NorthRight="join:local:7335:13923:7310:14570:1";
 public const float AuxMergeStation=64,PocketStation=230,PocketTaperLength=25;
 public static string Tail(string chain){return "editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(chain+":part:2")).TrimEnd('=').Replace('+','-').Replace('/','_')+":Y07-pocket-through";}
 public static string ThroughSegment(string chain,string label){return Tail(chain).Replace("pocket-through",label);}
 public static string Terminal(string chain){return chain==EInner||chain==E2||chain==EOuter?Tail(chain):chain+":part:2";}
 static float PocketShift(float station){return 3.5f*Mathf.SmoothStep(0,1,Mathf.Clamp01((station-PocketStation)/PocketTaperLength));}
 static float AuxShift(float station){return 3.5f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((station-(AuxMergeStation-25))/25)));}
 static float Curb(float station,float side){return 13.35f+(side>0?Mathf.Max(PocketShift(station),station<AuxMergeStation?AuxShift(station):0):0);}
 public const string PickupPlatform="sidewalk:stop:1";
 public const float PickupAt=15.9274759f;
 public const string NorthWestEntry="local:5978:17124:10508:15987";
 // Four curb tiles on the inside of the revised J10587 north-west turn.
 // They are outside the mainline's 16m selection band, but intersect its
 // eight-metre-earlier junction entry. Keep every polygon and its area.
 public static readonly string[] EntryCornerSidewalks={"sidewalk:generated:8604","sidewalk:generated:12595","sidewalk:generated:12596","sidewalk:generated:12597"};
 public static readonly string[] SourceIds={"37612023:A0020000:100:0","37612023:A0020000:101:0","37612023:A0020000:131:0","37612023:A0020000:1234:0","37612023:A0020000:364:0","37612023:A0020000:1512:0","37612023:A0020000:774:0"};
 public static string Family {get{return string.Join(",",SourceIds);}}
 public static readonly string[] OldChains={WInner,WOuter,EInner,EOuter};
 public static readonly string[] Chains={WInner,W2,W3,WOuter,EInner,E2,EOuter};
 public static string[] Pieces(string chain){return new[]{chain,chain+":part:1",chain+":part:2"};}
 public static int Number(string chain){return chain==WInner||chain==EInner?1:chain==W2||chain==E2?2:chain==WOuter?4:3;}
 public static int Mask(string chain){return chain==WInner||chain==Pocket?LaneDirectionPaint.Left:chain==WOuter?LaneDirectionPaint.Right:LaneDirectionPaint.Straight;}
 static float Offset(string chain){return chain==WInner?.5f:chain==W2||chain==EInner?4:chain==W3||chain==E2?7.5f:11;}
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("Y07_EDIT: "+reason);}
 static T Copy<T>(T value){return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));}
 static MapPoint Point(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());}
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,checkedAt="2026-11-13",month="2026-05",url="https://map.naver.com/p?c=19,0,0,0,adh&p=2KNmktAUh_JEdCM0rmWkyQ,70,-20,80,Float",note="Y07 reviewed three east through lanes and four west lanes. User-confirmed west J7254 L/S/S/R; east J10587 L/S/S/S with inner pocket beginning under overpass. Northbound right-entry outer auxiliary merges at approximate centre station64m (pano6o9n0Vawfz-KerD6vhtTXA); inner pocket begins approximate230m (panorgr44aiZkfzorjzqYdnmYw). Camera-derived stations are approximate; 25m taper lengths are game estimates. Road26m, lanes3.5m, median1m, curb shoulders0.25m and shifted generated sidewalks are game geometry estimates, not measured dimensions."};}
 public static AssistedRoadNetwork.Path Centre(RoadMapDocument document){
  var remaining=document.scene.objects.Where(o=>o.kind=="road"&&o.id.StartsWith("road:")&&SourceIds.Contains(o.id.Substring(5))).ToList();Check(remaining.Count==7,"seven Y07 source ribbons");
  var points=new List<Vector3>();Vector3 end=new Vector3(725.3499755859375f,0,1478.0179443359375f);
  while(remaining.Count>0){var road=remaining.OrderBy(r=>Mathf.Min(Vector3.Distance(end,r.points.First().V()),Vector3.Distance(end,r.points.Last().V()))).First();var seq=road.points.Select(p=>p.V()).ToArray();if(Vector3.Distance(end,seq.Last())<Vector3.Distance(end,seq.First()))Array.Reverse(seq);Check(Vector3.Distance(end,seq[0])<.03f,"disconnected source centre "+road.id);foreach(var v in seq)if(points.Count==0||Vector3.Distance(points.Last(),v)>.001f)points.Add(v);end=points.Last();remaining.Remove(road);}
  return new AssistedRoadNetwork.Path(points.ToArray());
 }
 static bool ExistingConnector(RoadMapDocument d,RoadMapLane lane){
  if(lane.disabled||!(lane.id.StartsWith("join:")||lane.change!=0))return false;
  var ids=OldChains.SelectMany(Pieces).ToArray();return lane.sourceId==Family||lane.next.Any(ids.Contains)||d.lanes.Any(l=>(ids.Contains(l.id)||l.id==NorthWestEntry)&&l.next.Contains(lane.id));
 }
 public static bool ChangedConnector(RoadMapLane lane){return !lane.disabled&&(lane.id.StartsWith("join:")||lane.change!=0)&&lane.evidence!=null&&lane.evidence.note.StartsWith("Y07 reviewed");}
 static Vector3 SidewalkShift(ProjectMapObject o,AssistedRoadNetwork.Path centre){
  if(!o.enabled||o.kind!="sidewalk"||!o.id.StartsWith("sidewalk:generated:")||o.points.Length<3)return Vector3.zero;
  var position=o.points.Aggregate(Vector3.zero,(v,p)=>v+p.V())/o.points.Length;float error;float at=centre.Project(position,out error);var heading=centre.Tangent(at);var delta=position-centre.Position(at);
  if(Mathf.Abs(Vector3.Dot(delta,heading))>2.5f)return Vector3.zero;
  var right=TrafficSystem.Right(heading);float side=Vector3.Dot(delta,right);if(Mathf.Abs(side)<7||Mathf.Abs(side)>20)return Vector3.zero;
  float sign=Mathf.Sign(side),inside=float.MaxValue;
  foreach(var p in o.points){float e;float station=centre.Project(p.V(),out e);inside=Mathf.Min(inside,Vector3.Dot(p.V()-centre.Position(station),TrafficSystem.Right(centre.Tangent(station)))*sign);}
  float target=Curb(at,side);if(inside>=target||inside<6)return Vector3.zero;
  return right*sign*(target-inside);
 }
 public static HashSet<string> SceneChangeIds(RoadMapDocument d){
  var ids=new HashSet<string>(EntryCornerSidewalks){PickupPlatform};var centre=Centre(d);var affected=OldChains.SelectMany(Pieces).Concat(d.lanes.Where(l=>ExistingConnector(d,l)).Select(l=>l.id)).ToArray();
  foreach(var o in d.scene.objects)if(o.kind=="road"&&o.id.StartsWith("road:")&&SourceIds.Contains(o.id.Substring(5))||o.id.StartsWith("road:polish:")&&affected.Contains(o.id.Substring("road:polish:".Length))||SidewalkShift(o,centre).sqrMagnitude>0)ids.Add(o.id);
  return ids;
 }
 public static RoadMapDocument Preview(ProjectMapPackage package){
  var d=package.document.Copy();Check(!d.lanes.Any(l=>l.id.StartsWith(Prefix)),"already authored/partial Y07");var centre=Centre(d);var original=d.lanes.ToDictionary(l=>l.id);var lanes=new Dictionary<string,RoadMapLane>(original);var added=new List<RoadMapLane>();
  foreach(string chain in OldChains)foreach(string id in Pieces(chain))Check(lanes.ContainsKey(id)&&!lanes[id].disabled&&lanes[id].sourceId==Family&&lanes[id].change==0,"expected old three-piece chain "+id);
  foreach(var pair in new[]{new[]{W2,WInner},new[]{W3,WInner},new[]{E2,EInner}}){
   for(int i=0;i<3;i++){var lane=Copy(lanes[Pieces(pair[1])[i]]);lane.id=Pieces(pair[0])[i];lane.fromNode=pair[0]+(i==0?":start":":split:"+i);lane.toNode=pair[0]+(i==2?":end":":split:"+(i+1));lane.next=i<2?new[]{Pieces(pair[0])[i+1]}:new string[0];lanes.Add(lane.id,lane);added.Add(lane);}
  }
  foreach(string chain in Chains)foreach(string id in Pieces(chain)){
   var lane=lanes[id];var path=Path(lane);float walked=0;var points=new MapPoint[lane.points.Length];
   for(int i=0;i<points.Length;i++){if(i>0)walked+=Vector3.Distance(path.points[i-1],path.points[i]);var p=path.points[i];var right=TrafficSystem.Right(path.Tangent(walked));float error;float at=centre.Project(p,out error);float oldOffset=Vector3.Dot(p-centre.Position(at),right);float extra=chain==EInner||chain==E2||chain==EOuter?PocketShift(at):0;points[i]=Point(p+right*(Offset(chain)+extra-oldOffset));}
   lane.points=points;lane.width=3.5f;lane.number=Number(chain);lane.evidence=Evidence();
  }
  // Snap adjacent imported pieces to identical nodes/positions without
  // changing their original IDs or imported sourceOffset conventions.
  foreach(string chain in Chains)for(int i=0;i<2;i++){var a=lanes[Pieces(chain)[i]];var b=lanes[Pieces(chain)[i+1]];b.points[0]=Point(a.points.Last().V());b.fromNode=a.toNode;}
  // Move only the lane/junction boundary eight metres upstream on the
  // existing approach. The widened W4 curb otherwise leaves a tight hook
  // between the imported stop-line endpoint and the relocated exit.
  // Both existing turn connectors retain their identities and permissions.
  var entry=lanes[NorthWestEntry];var entryPath=Path(entry);float cutoff=entryPath.length-8,walk=0;var retained=new List<MapPoint>{entry.points[0]};
  for(int i=1;i<entry.points.Length;i++){walk+=Vector3.Distance(entry.points[i-1].V(),entry.points[i].V());if(walk>=cutoff)break;retained.Add(entry.points[i]);}
  retained.Add(Point(entryPath.Position(cutoff)));entry.points=retained.ToArray();entry.heights=new float[entry.points.Length];
  Debug.Log("Y07_JUNCTION_BOUNDARY id="+entry.id+" trim=8 old="+entryPath.Position(entryPath.length).ToString("F3")+" new="+entry.points.Last().V().ToString("F3"));
  // Every existing connector touching a moved lane follows those endpoints.
  foreach(var join in d.lanes.Where(l=>ExistingConnector(package.document,l))){var input=d.lanes.Where(l=>!l.disabled&&l.next.Contains(join.id)).ToArray();Check(input.Length==1&&join.next.Length==1,"single original connector endpoints "+join.id);Anchor(join,lanes[input[0].id],lanes[join.next[0]]);}
  // Move the two existing west straight connectors onto the two middle lanes.
  var west1=lanes[WInner+":part:2"];var west4=lanes[WOuter+":part:2"];
  string westStraight1="join:"+WInner+":0",westStraight2="join:"+WOuter+":0";
  foreach(var pair in new[]{new[]{W2,westStraight1},new[]{W3,westStraight2}}){var tail=lanes[pair[0]+":part:2"];var join=lanes[pair[1]];Anchor(join,tail,lanes[join.next.Single()]);tail.next=new[]{join.id};}
  west1.next=new[]{"join:"+WInner+":-1"};west4.next=new[]{"join:"+WOuter+":1"};
  var eastMiddle=lanes[E2+":part:2"];var eastStraight=Copy(lanes["join:"+EInner+":0"]);eastStraight.id="join:"+E2+":straight";Anchor(eastStraight,eastMiddle,lanes[lanes["join:"+EOuter+":0"].next.Single()]);eastMiddle.next=new[]{eastStraight.id};added.Add(eastStraight);lanes.Add(eastStraight.id,eastStraight);
  // Existing change IDs now target their adjacent inserted lane, not jump
  // across two or three lanes. Added middle changes use the same 10→42m window.
  var neighbour=new Dictionary<string,string>{{WInner,W2},{WOuter,W3},{EInner,E2},{EOuter,E2}};
  foreach(var change in d.lanes.Where(l=>l.sourceId==Family&&l.change!=0)){
   var input=d.lanes.Single(l=>!l.disabled&&l.next.Contains(change.id));Check(neighbour.ContainsKey(input.id),"unexpected lane-change origin "+change.id);var target=lanes[neighbour[input.id]+":part:2"];Anchor(change,input,target);change.change=target.number-input.number;Check(Math.Abs(change.change)==1,"existing change adjacency");
  }
  foreach(var pair in new[]{new[]{W2,WInner},new[]{W2,W3},new[]{W3,W2},new[]{W3,WOuter},new[]{E2,EInner},new[]{E2,EOuter}}){
   var from=lanes[pair[0]];var to=lanes[pair[1]+":part:2"];var change=Copy(lanes["change:"+WInner+":"+WOuter]);change.id="change:"+Prefix+pair[0]+":"+pair[1];change.turn=0;change.change=to.number-from.number;Anchor(change,from,to);from.next=from.next.Concat(new[]{change.id}).ToArray();added.Add(change);lanes.Add(change.id,change);
  }
  // Entry cloning preserves existing movement permissions at both junctions.
  foreach(var pair in new[]{new[]{W2,WInner},new[]{W3,WOuter},new[]{E2,EInner}})foreach(var prior in package.document.lanes.Where(l=>!l.disabled&&l.id.StartsWith("join:")&&l.next.Contains(pair[1]))){
   var originalEntry=package.document.lanes.Single(l=>!l.disabled&&l.next.Contains(prior.id));var input=lanes[originalEntry.id];var join=Copy(lanes[prior.id]);join.id="join:"+Prefix+"entry:"+pair[0]+":"+prior.id;Anchor(join,input,lanes[pair[0]]);input.next=input.next.Concat(new[]{join.id}).Distinct().ToArray();added.Add(join);lanes.Add(join.id,join);
  }
  EastTransitions(lanes,added,centre);
  d.lanes=d.lanes.Concat(added).ToArray();AddMergePaint(d,lanes[Aux]);var scene=new List<ProjectMapObject>(d.scene.objects);var relocations=new List<string>();
  var laneSweeps=LaneSweeps(d);var hardObstacles=scene.Where(o=>o.enabled&&(o.kind=="building"||o.kind=="water")).ToArray();
  var pickupShift=Path(lanes[WOuter+":part:1"]).Position(PickupAt)-Path(package.document.lanes.Single(l=>l.id==WOuter+":part:1")).Position(PickupAt);
  Check(scene.Count(o=>o.id==PickupPlatform&&o.enabled&&o.kind=="sidewalk"&&o.name=="승하차 보도 · 은계 승하차 2")==1,"expected bound pickup platform");
  foreach(var o in scene){
   if(o.kind=="road"&&o.id.StartsWith("road:")&&SourceIds.Contains(o.id.Substring(5))){o.width=26;o.lanes=7;}
   if(o.id.StartsWith("road:polish:")){string id=o.id.Substring("road:polish:".Length);if(OldChains.SelectMany(Pieces).Contains(id))o.enabled=false;else if(lanes.ContainsKey(id)&&ChangedConnector(lanes[id]))o.points=lanes[id].points.Select(p=>Point(p.V())).ToArray();}
   var shift=SidewalkShift(o,centre);if(shift.sqrMagnitude>0||EntryCornerSidewalks.Contains(o.id)){var resolved=ResolveSidewalk(o,shift,centre,laneSweeps,hardObstacles);o.points=o.points.Select(p=>Point(p.V()+resolved)).ToArray();relocations.Add(o.id);}
   if(o.id==PickupPlatform){
    var path=Path(lanes[WOuter+":part:1"]);var outward=TrafficSystem.Right(path.Tangent(PickupAt));
    var translated=o.points.Select(p=>Point(p.V()+pickupShift)).ToArray();Debug.Log("Y07_PICKUP_INITIAL_CLEARANCE "+WalkConflict(translated,laneSweeps,hardObstacles));
    // Keep the pickup's longitudinal station and platform shape. Move only
    // outward until its inner edge clears both the road curb and vehicle sweep.
    bool placed=false;for(float extra=0;extra<=3;extra+=.05f){
     var candidate=o.points.Select(p=>Point(p.V()+pickupShift+outward*extra)).ToArray();
     bool curb=candidate.All(p=>{float error;float at=centre.Project(p.V(),out error);return Vector3.Dot(p.V()-centre.Position(at),-TrafficSystem.Right(centre.Tangent(at)))>=13.35f;});
     if(!curb||!WalkClear(candidate,laneSweeps,hardObstacles))continue;
     o.points=candidate;placed=true;Debug.Log("Y07_PICKUP_PLATFORM_RELOCATED id="+o.id+" laneAt="+PickupAt+" boundShift="+pickupShift.ToString("F3")+" extraOutward="+extra);break;
    }Check(placed,"pickup platform has no accessible clear curb position within3m outward");relocations.Add(o.id);
   }
  }
  // Match the existing authored swept-ribbon allowance: this is junction
  // pavement for the vehicle rectangle, not an additional driving lane.
  foreach(var join in added.Where(ChangedConnector))scene.Add(new ProjectMapObject{id="road:"+Prefix+join.id,kind="road",name="Y07 reviewed connector pavement",width=join.turn==0?3.8f:4.2f,lanes=1,oneWay=true,suppressPaint=true,points=join.points.Select(p=>Point(p.V())).ToArray()});
  foreach(string id in new[]{Aux,Tail(EOuter)})scene.Add(new ProjectMapObject{id="road:"+Prefix+id,kind="road",name="Y07 scoped east auxiliary/pocket pavement",width=4,lanes=1,oneWay=true,suppressPaint=true,points=lanes[id].points.Select(p=>Point(p.V())).ToArray()});
  d.scene.objects=scene.ToArray();Check(relocations.Count>0,"expected scoped generated sidewalks to relocate");Debug.Log("Y07_SIDEWALK_RELOCATIONS count="+relocations.Count+" ids="+string.Join(",",relocations));return d;
 }
 internal static MapPoint[] Slice(AssistedRoadNetwork.Path path,float start,float end){
  var result=new List<MapPoint>();for(float at=start;at<end;at+=1){var p=path.Position(at);if(result.Count==0||Vector3.Distance(result.Last().V(),p)>=.001f)result.Add(Point(p));}
  // Floating-point projection can leave a sub-millimetre residual after the
  // final one-metre sample. Keep the exact cut endpoint once, not a duplicate.
  var endpoint=path.Position(end);if(result.Count>1&&Vector3.Distance(result.Last().V(),endpoint)<.001f)result[result.Count-1]=Point(endpoint);else result.Add(Point(endpoint));return result.ToArray();
 }
 static void AddMergePaint(RoadMapDocument d,RoadMapLane aux){
  var path=Path(aux);var features=new List<RoadMapFacility>(d.facilities);
  foreach(var spec in new[]{new[]{18f,1.4f,4f},new[]{30f,1.6f,2.6f}}){
   bool merge=spec[0]==18;var p=path.Position(spec[0]);var direction=path.Tangent(spec[0]);
   features.Add(new RoadMapFacility{id=Prefix+(merge?"merge-arrow":"yield-triangle"),kind="lanePresentation",type=merge?LaneMergePaint.MergeLeftType:LaneMergePaint.YieldType,laneId=Aux,startAt=spec[0],width=spec[1],length=spec[2],scale=1,rule=false,groupId="",targetId="",x=p.x,z=p.z,yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,ruleConfirmed=false,month="2026-05",checkedAt="2026-11-13",url="https://map.naver.com/p?c=19,0,0,0,adh&p=6o9n0Vawfz-KerD6vhtTXA,70,-20,80,Float",note="User photographs and May2026 panorama confirm outer auxiliary lane reduction/left merge arrow and yield triangle near J7254 east exit. Paint appearance only; no added navigation or yield-law permission. Glyph centre stations18m/30m along authored auxiliary and dimensions1.4x4m/1.6x2.6m are game placement estimates, not field measurements."}});
  }
  d.facilities=features.ToArray();
 }
 static void EastTransitions(Dictionary<string,RoadMapLane> lanes,List<RoadMapLane> added,AssistedRoadNetwork.Path centre){
  // Approximate visible panorama anchors: merge end~64m, pocket starts~230m
  // under the overpass. The 25m tapers and 3.5m offsets are game estimates.
  foreach(string chain in new[]{EInner,E2,EOuter}){
   var head=lanes[chain+":part:2"];var path=Path(head);float error;float cut=path.Project(centre.Position(PocketStation)+TrafficSystem.Right(centre.Tangent(PocketStation))*Offset(chain),out error);
   Check(cut>40&&cut<path.length-50,"pocket split station inside long through segment "+chain);
   var tail=Copy(head);tail.id=Tail(chain);tail.sourceOffset=head.sourceOffset+cut;tail.fromNode=Prefix+"pocket-split:"+chain;tail.points=Slice(path,cut,path.length);tail.heights=new float[tail.points.Length];tail.number=head.number+1;
   head.points=Slice(path,0,cut);head.heights=new float[head.points.Length];head.toNode=tail.fromNode;head.next=new[]{tail.id};lanes.Add(tail.id,tail);added.Add(tail);
  }
  var innerHead=lanes[EInner+":part:2"];var innerTail=lanes[Tail(EInner)];var pocket=Copy(innerTail);pocket.id=Pocket;pocket.sourceOffset=0;pocket.number=1;pocket.toNode=Prefix+"left-pocket:end";
  var pocketPoints=new List<MapPoint>();float firstError,lastError;float first=centre.Project(innerTail.points[0].V(),out firstError),last=centre.Project(innerTail.points.Last().V(),out lastError);
  for(float at=first;at<last;at+=1)pocketPoints.Add(Point(centre.Position(at)+TrafficSystem.Right(centre.Tangent(at))*4));
  pocketPoints.Add(Point(centre.Position(last)+TrafficSystem.Right(centre.Tangent(last))*4));pocketPoints[0]=Point(innerHead.points.Last().V());pocket.points=pocketPoints.ToArray();pocket.heights=new float[pocket.points.Length];
  string leftId="join:"+EInner+":-1";pocket.next=new[]{leftId};innerTail.next=innerTail.next.Where(id=>id!=leftId).ToArray();innerHead.next=innerHead.next.Concat(new[]{Pocket}).ToArray();lanes.Add(Pocket,pocket);added.Add(pocket);Anchor(lanes[leftId],pocket,lanes[lanes[leftId].next.Single()]);
  var outer=lanes[EOuter];var aux=Copy(outer);aux.id=Aux;aux.number=4;aux.fromNode=Prefix+"right-merge:start";aux.toNode=lanes[EOuter+":part:2"].fromNode;aux.next=new[]{EOuter+":part:2"};
  var auxPoints=new List<MapPoint>();foreach(var point in outer.points.Concat(lanes[EOuter+":part:1"].points.Skip(1))){float error;float at=centre.Project(point.V(),out error);auxPoints.Add(Point(point.V()+TrafficSystem.Right(centre.Tangent(at))*AuxShift(at)));}
  auxPoints[auxPoints.Count-1]=Point(lanes[EOuter+":part:2"].points[0].V());aux.points=auxPoints.ToArray();aux.heights=new float[aux.points.Length];lanes.Add(Aux,aux);added.Add(aux);
  var right=lanes[NorthRight];var source=lanes.Values.Single(l=>!l.disabled&&l.next.Contains(NorthRight));Anchor(right,source,aux);
  // A vehicle merging at64m must be able to choose its later approach lane;
  // the imported10→42m change window is already behind that entry.
  foreach(string chain in new[]{EInner,E2,EOuter}){
   var middle=SplitThrough(lanes[chain+":part:2"],ThroughSegment(chain,"change-window"),100,centre,lanes,added);
   SplitThrough(middle,ThroughSegment(chain,"after-change"),130,centre,lanes,added);
  }
  foreach(var pair in new[]{new[]{EInner,E2},new[]{E2,EInner},new[]{E2,EOuter},new[]{EOuter,E2}}){
   var from=lanes[pair[0]+":part:2"];var to=lanes[ThroughSegment(pair[1],"after-change")];var change=Copy(lanes["change:"+EInner+":"+EOuter]);change.id="change:"+Prefix+"post-merge:"+pair[0]+":"+pair[1];change.change=to.number-from.number;change.turn=0;Anchor(change,from,to);from.next=from.next.Concat(new[]{change.id}).ToArray();lanes.Add(change.id,change);added.Add(change);
  }
  foreach(string chain in new[]{EInner,E2,EOuter}){var middle=SplitThrough(lanes[ThroughSegment(chain,"after-change")],ThroughSegment(chain,"second-window"),160,centre,lanes,added);SplitThrough(middle,ThroughSegment(chain,"after-second"),190,centre,lanes,added);}
  foreach(var pair in new[]{new[]{EInner,E2},new[]{E2,EInner},new[]{E2,EOuter},new[]{EOuter,E2}}){
   var from=lanes[ThroughSegment(pair[0],"after-change")];var to=lanes[ThroughSegment(pair[1],"after-second")];var change=Copy(lanes["change:"+EInner+":"+EOuter]);change.id="change:"+Prefix+"second-window:"+pair[0]+":"+pair[1];change.change=to.number-from.number;change.turn=0;Anchor(change,from,to);from.next=from.next.Concat(new[]{change.id}).ToArray();lanes.Add(change.id,change);added.Add(change);
  }
  Debug.Log("Y07_EAST_PROFILE through=3 auxMergeApprox="+AuxMergeStation+" pocketStartApprox="+PocketStation+" gameTaper="+PocketTaperLength+" terminal=L/S/S/S");
 }
 static RoadMapLane SplitThrough(RoadMapLane head,string id,float station,AssistedRoadNetwork.Path centre,Dictionary<string,RoadMapLane> lanes,List<RoadMapLane> added){
  var path=Path(head);float error;float cut=path.Project(centre.Position(station),out error);Check(cut>10&&cut<path.length-10,"post-merge window bounded "+id);
  var tail=Copy(head);tail.id=id;tail.sourceOffset=head.sourceOffset+cut;tail.fromNode=id+":node";tail.points=Slice(path,cut,path.length);tail.heights=new float[tail.points.Length];head.points=Slice(path,0,cut);head.heights=new float[head.points.Length];head.toNode=tail.fromNode;head.next=new[]{tail.id};lanes.Add(id,tail);added.Add(tail);return tail;
 }
 sealed class SweepSegment {public string id;public Vector3 a,b;public float half,minX,maxX,minZ,maxZ;}
 static List<SweepSegment> LaneSweeps(RoadMapDocument d){
  var result=new List<SweepSegment>();foreach(var lane in d.lanes.Where(l=>!l.disabled))for(int i=1;i<lane.points.Length;i++){
   var a=lane.points[i-1].V();var b=lane.points[i].V();float half=lane.width*.5f+.15f;
   result.Add(new SweepSegment{id=lane.id,a=a,b=b,half=half,minX=Mathf.Min(a.x,b.x)-half,maxX=Mathf.Max(a.x,b.x)+half,minZ=Mathf.Min(a.z,b.z)-half,maxZ=Mathf.Max(a.z,b.z)+half});
  }return result;
 }
 static float PointSegment(Vector3 p,Vector3 a,Vector3 b){var delta=b-a;return (p-(a+delta*Mathf.Clamp01(Vector3.Dot(p-a,delta)/Mathf.Max(.000001f,delta.sqrMagnitude)))).sqrMagnitude;}
 static float Cross2(Vector3 a,Vector3 b,Vector3 c){return (b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);}
 static float SegmentDistance(Vector3 a,Vector3 b,Vector3 c,Vector3 e){
  if(Mathf.Max(a.x,b.x)>=Mathf.Min(c.x,e.x)&&Mathf.Max(c.x,e.x)>=Mathf.Min(a.x,b.x)&&Mathf.Max(a.z,b.z)>=Mathf.Min(c.z,e.z)&&Mathf.Max(c.z,e.z)>=Mathf.Min(a.z,b.z)&&Cross2(a,b,c)*Cross2(a,b,e)<=0&&Cross2(c,e,a)*Cross2(c,e,b)<=0)return 0;
  return Mathf.Min(Mathf.Min(PointSegment(a,c,e),PointSegment(b,c,e)),Mathf.Min(PointSegment(c,a,b),PointSegment(e,a,b)));
 }
 static bool WalkClear(MapPoint[] polygon,List<SweepSegment> sweeps,ProjectMapObject[] obstacles){
  return WalkConflict(polygon,sweeps,obstacles)=="clear";
 }
 static string WalkConflict(MapPoint[] polygon,List<SweepSegment> sweeps,ProjectMapObject[] obstacles){
  float minX=polygon.Min(p=>p.x),maxX=polygon.Max(p=>p.x),minZ=polygon.Min(p=>p.z),maxZ=polygon.Max(p=>p.z);
  foreach(var s in sweeps){if(s.maxX<minX||s.minX>maxX||s.maxZ<minZ||s.minZ>maxZ)continue;
   if(ProjectMapScene.Contains(polygon,s.a)||ProjectMapScene.Contains(polygon,s.b))return "lane containment "+s.id;
   for(int i=0;i<polygon.Length;i++)if(SegmentDistance(s.a,s.b,polygon[i].V(),polygon[(i+1)%polygon.Length].V())<=s.half*s.half)return "lane sweep "+s.id+" distance="+Mathf.Sqrt(SegmentDistance(s.a,s.b,polygon[i].V(),polygon[(i+1)%polygon.Length].V()))+" required="+s.half;
  }
  foreach(var obstacle in obstacles){var q=obstacle.points;if(q.Max(p=>p.x)<minX||q.Min(p=>p.x)>maxX||q.Max(p=>p.z)<minZ||q.Min(p=>p.z)>maxZ)continue;
   if(q.Any(p=>ProjectMapScene.Contains(polygon,p.V()))||polygon.Any(p=>ProjectMapScene.Contains(q,p.V())))return "obstacle containment "+obstacle.id;
   for(int i=0;i<polygon.Length;i++)for(int j=0;j<q.Length;j++)if(SegmentDistance(polygon[i].V(),polygon[(i+1)%polygon.Length].V(),q[j].V(),q[(j+1)%q.Length].V())<.000001f)return "obstacle edge "+obstacle.id;
  }return "clear";
 }
 static Vector3 ResolveSidewalk(ProjectMapObject o,Vector3 initial,AssistedRoadNetwork.Path centre,List<SweepSegment> sweeps,ProjectMapObject[] hardObstacles){
  var mean=o.points.Aggregate(Vector3.zero,(v,p)=>v+p.V())/o.points.Length;float error;float at=centre.Project(mean,out error);var tangent=centre.Tangent(at);var right=TrafficSystem.Right(tangent);float sign=Mathf.Sign(Vector3.Dot(mean-centre.Position(at),right));var outward=right*sign;
  // Restrict obstacle candidates to every position this bounded local search
  // can reach. The placement itself is tested against full swept lane widths,
  // including unchanged side approaches and curved junction connectors.
  var local=sweeps.Where(s=>s.maxX>=mean.x-32&&s.minX<=mean.x+32&&s.maxZ>=mean.z-32&&s.minZ<=mean.z+32).ToList();
  var obstacles=hardObstacles.Where(a=>a.points.Max(p=>p.x)>=mean.x-32&&a.points.Min(p=>p.x)<=mean.x+32&&a.points.Max(p=>p.z)>=mean.z-32&&a.points.Min(p=>p.z)<=mean.z+32).ToArray();
  var proposals=new List<Vector2>();for(float extra=0;extra<=12;extra+=.5f)for(float along=-12;along<=12;along+=.5f)proposals.Add(new Vector2(extra,along));
  foreach(var proposal in proposals.OrderBy(v=>v.sqrMagnitude).ThenBy(v=>v.x).ThenBy(v=>v.y)){
   var shift=initial+outward*proposal.x+tangent*proposal.y;var polygon=o.points.Select(p=>Point(p.V()+shift)).ToArray();
   if(polygon.Any(p=>{float e;float station=centre.Project(p.V(),out e);return Vector3.Dot(p.V()-centre.Position(station),TrafficSystem.Right(centre.Tangent(station)))*sign<Curb(station,sign)-.05f;}))continue;
   if(!WalkClear(polygon,local,obstacles))continue;
   if(proposal.sqrMagnitude>0)Debug.Log("Y07_CORNER_SIDEWALK_RESOLVED id="+o.id+" extraOutward="+proposal.x+" along="+proposal.y+" original="+mean.ToString("F2")+" shift="+shift.ToString("F2"));
   return shift;
  }
  throw new InvalidOperationException("Y07 sidewalk needs explicit junction design: "+o.id+" near="+mean.ToString("F2")+" no clear candidate within local12m search");
 }
 static void Anchor(RoadMapLane connector,RoadMapLane from,RoadMapLane to){
  var start=from.points.Last().V();var end=to.points.First().V();var forward=(start-from.points[from.points.Length-2].V()).normalized;var exit=(to.points[1].V()-end).normalized;float handle=Vector3.Distance(start,end)/3;var points=new MapPoint[49];
  for(int i=0;i<points.Length;i++){float t=i/(float)(points.Length-1),u=1-t;points[i]=Point(u*u*u*start+3*u*u*t*(start+forward*handle)+3*u*t*t*(end-exit*handle)+t*t*t*end);}
  connector.points=points;connector.heights=new float[points.Length];connector.fromNode=from.toNode;connector.toNode=to.fromNode;connector.next=new[]{to.id};connector.number=to.number;connector.ruleId=from.ruleId;connector.width=3.5f;connector.evidence=Evidence();
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);Check(p.document.revision==21&&p.contentHash==RequiredHash,"requires exact reviewed r21; reassess if another map edit was applied");
  var d=Preview(p);Y07MapTests.Validate(p,d);d.revision++;d.savedAt=DateTime.UtcNow.ToString("o");d.projectRevision="";var result=ProjectMapPackage.Create(p.baseline,d);Check(result.baselineHash==p.baselineHash,"baseline changed");ProjectMapStore.Save(root,result,token);ProjectMapBuild.Prepare();Y07MapTests.Verify();Debug.Log("Y07_MAP_APPLIED revision="+d.revision+" contentHash="+result.contentHash);
 }
}

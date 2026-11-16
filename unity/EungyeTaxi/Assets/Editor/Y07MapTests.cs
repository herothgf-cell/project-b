using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Y07MapTests {
 static void Check(bool value,string message){if(!value)throw new InvalidOperationException("Y07_TEST: "+message);}
 public static void Verify(){var p=ProjectMapStore.Load(ProjectMapBuild.Root);Validate(p,p.document);}
 public static void StopDiagnostics(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var before=RoadMapComposer.Compose(p.baseline,p.document);EungyeConnectivityRepair.Apply(before,p.baseline);
  var after=RoadMapComposer.Compose(p.baseline,Y07MapEdit.Preview(p));EungyeConnectivityRepair.Apply(after,p.baseline);
  LogStops("BASELINE",PassengerStopCatalog.For(before),before);LogStops("PREVIEW",PassengerStopCatalog.For(after),after);
 }
 static void LogStops(string label,PassengerStopCatalog catalog,MapData map){
  Debug.Log("Y07_STOPS_"+label+" mapId="+map.mapId+" activeScene="+ProjectMapScene.Active(map)+" legacy="+catalog.LegacyCount+" total="+catalog.Stops.Length+" ids="+string.Join(",",catalog.Entries.Select(e=>e.StopId+":"+e.Stop.name))+" rejected="+string.Join(" / ",catalog.Rejected));
 }
 public static void PreviewChecks(){
  var residualPath=new AssistedRoadNetwork.Path(new[]{Vector3.zero,new Vector3(10.0004f,0,0)});var residualSlice=Y07MapEdit.Slice(residualPath,0,residualPath.length);
  Check(residualSlice.Length==11&&Vector3.Distance(residualSlice.Last().V(),residualPath.Position(residualPath.length))<.00001f,"slice retains exact noninteger endpoint once");
  for(int i=1;i<residualSlice.Length;i++)Check(Vector3.Distance(residualSlice[i-1].V(),residualSlice[i].V())>=.001f,"slice has no projected endpoint duplicate");
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=p.document.ToJson();
  var baselineErrors=RoadMapValidator.Validate(p.baseline,p.document).Where(e=>e.error).ToArray();Check(baselineErrors.Length==0,"input map already invalid: "+string.Join(" / ",baselineErrors.Select(e=>e.ToString())));
  var d=Y07MapEdit.Preview(p);
  Check(before==p.document.ToJson(),"preview mutated input");Check(d.ToJson()==Y07MapEdit.Preview(p).ToJson(),"preview is not deterministic");
  for(int i=0;i<p.document.lanes.Length;i++){var a=p.document.lanes[i];var b=d.lanes[i];Check(a.id==b.id&&a.sourceId==b.sourceId&&a.sourceOffset==b.sourceOffset,"existing identity/order/offset "+a.id);}
  const string unrelated="join:reviewed:J5057-N:right";
  Check(JsonUtility.ToJson(p.document.lanes.Single(l=>l.id==unrelated))==JsonUtility.ToJson(d.lanes.Single(l=>l.id==unrelated)),"unrelated J5057 lane changed");
  Debug.Log("Y07_BASELINE_VALIDATION_OK unrelated-J5057-right=byte-identical");
  Check(JsonUtility.ToJson(new RoadMapDocument{id="",facilities=p.document.facilities,signals=p.document.signals})==JsonUtility.ToJson(new RoadMapDocument{id="",facilities=d.facilities.Take(p.document.facilities.Length).ToArray(),signals=d.signals}),"existing traffic facilities/signals changed");
  Check(d.facilities.Length==p.document.facilities.Length+2,"exactly two auxiliary appearance facilities appended");
  var oldObjects=p.document.scene.objects;var allowed=Y07MapEdit.SceneChangeIds(p.document);
  var originalEntry=p.document.lanes.Single(l=>l.id==Y07MapEdit.NorthWestEntry);var trimmedEntry=d.lanes.Single(l=>l.id==originalEntry.id);
  var originalEntryPath=new AssistedRoadNetwork.Path(originalEntry.points.Select(q=>q.V()).ToArray());var trimmedEntryPath=new AssistedRoadNetwork.Path(trimmedEntry.points.Select(q=>q.V()).ToArray());
  Check(Mathf.Abs(originalEntryPath.length-trimmedEntryPath.length-8)<.002f,"bounded8m junction entry trim actual="+(originalEntryPath.length-trimmedEntryPath.length));
  var originalRight=originalEntry.next.Single(id=>p.document.lanes.Single(l=>l.id==id).next.Contains(Y07MapEdit.WOuter));
  string addedMiddleEntry="join:"+Y07MapEdit.Prefix+"entry:"+Y07MapEdit.W3+":"+originalRight;
  Check(trimmedEntry.next.SequenceEqual(originalEntry.next.Concat(new[]{addedMiddleEntry})),"junction entry preserves original ordered L/R IDs and adds only authored W3 entry");
  var clonedMiddle=d.lanes.Single(l=>l.id==addedMiddleEntry);var originalMovement=p.document.lanes.Single(l=>l.id==originalRight);
  Check(clonedMiddle.turn==originalMovement.turn&&clonedMiddle.change==originalMovement.change&&clonedMiddle.next.SequenceEqual(new[]{Y07MapEdit.W3}),"added W3 entry retains original right permission and exact target");
  for(int i=0;i<trimmedEntry.points.Length-1;i++)Check(Vector3.Distance(originalEntry.points[i].V(),trimmedEntry.points[i].V())<.001f,"upstream approach geometry preserved");
  for(int i=0;i<oldObjects.Length;i++){
   var old=oldObjects[i];var current=d.scene.objects[i];Check(old.id==current.id,"scene order changed");
   if(!allowed.Contains(old.id))Check(JsonUtility.ToJson(old)==JsonUtility.ToJson(current),"unrelated scene object changed "+old.id);
   if(Y07MapEdit.EntryCornerSidewalks.Contains(old.id)){
    Check(current.enabled&&current.kind==old.kind&&current.name==old.name&&current.points.Length==old.points.Length,"junction curb polygon identity retained "+old.id);
    var delta=current.points[0].V()-old.points[0].V();Check(delta.magnitude<=17,"junction curb relocation bounded "+old.id);
    for(int j=0;j<old.points.Length;j++)Check(Vector3.Distance(current.points[j].V(),old.points[j].V()+delta)<.001f,"junction curb polygon shape/area preserved "+old.id);
   }
  }
  var oldPickup=p.document.lanes.Single(l=>l.id==Y07MapEdit.WOuter+":part:1");var newPickup=d.lanes.Single(l=>l.id==oldPickup.id);
  var pickupShift=new AssistedRoadNetwork.Path(newPickup.points.Select(q=>q.V()).ToArray()).Position(Y07MapEdit.PickupAt)-new AssistedRoadNetwork.Path(oldPickup.points.Select(q=>q.V()).ToArray()).Position(Y07MapEdit.PickupAt);
  var oldPlatform=oldObjects.Single(o=>o.id==Y07MapEdit.PickupPlatform);var platform=d.scene.objects.Single(o=>o.id==oldPlatform.id);
  Check(platform.name==oldPlatform.name&&platform.kind==oldPlatform.kind&&platform.points.Length==oldPlatform.points.Length,"pickup platform identity and shape retained");
  var platformShift=platform.points[0].V()-oldPlatform.points[0].V();
  var pickupPath=new AssistedRoadNetwork.Path(newPickup.points.Select(q=>q.V()).ToArray());var tangent=pickupPath.Tangent(Y07MapEdit.PickupAt);var extra=platformShift-pickupShift;
  Check(Mathf.Abs(Vector3.Dot(extra,tangent))<.002f&&Vector3.Dot(extra,TrafficSystem.Right(tangent))>=-.002f&&extra.magnitude<=3.002f,"pickup platform stays at bound station with bounded outward curb correction");
  var curbCentre=Y07MapEdit.Centre(d);
  for(int i=0;i<platform.points.Length;i++){
   Check(Vector3.Distance(platform.points[i].V(),oldPlatform.points[i].V()+platformShift)<.001f,"pickup platform shape preserved by translation");
   float error;float at=curbCentre.Project(platform.points[i].V(),out error);Check(Vector3.Dot(platform.points[i].V()-curbCentre.Position(at),-TrafficSystem.Right(curbCentre.Tangent(at)))>=13.348f,"pickup platform is outside widened curb");
  }
  Validate(p,d);Debug.Log("Y07_PREVIEW_OK deterministic input-immutable original-ids-offsets-order-preserved");
 }
 public static void Validate(ProjectMapPackage package,RoadMapDocument d){
  var errors=RoadMapValidator.Validate(package.baseline,d).Where(i=>i.error).ToArray();Check(errors.Length==0,"validator "+string.Join(" / ",errors.Take(8).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(package.baseline,d);EungyeConnectivityRepair.Apply(map,package.baseline);var graph=new HdRoadGraph(map);
  var centre=Y07MapEdit.Centre(package.document);var family=Y07MapEdit.Family;
  foreach(float station in new[]{35f,90f,170f,215f,280f,centre.length-30}){
   var point=centre.Position(station);var direction=centre.Tangent(station);
   var crossing=graph.edges.Where(e=>!e.disabled&&e.sourceId==family&&e.turn==0&&e.change==0&&!e.id.StartsWith("join:")&&Crosses(e.points,point,direction)).ToArray();
   int east=crossing.Count(e=>Vector3.Dot(e.points.Last().V()-e.points[0].V(),direction)>0);int expected=station<Y07MapEdit.AuxMergeStation||station>Y07MapEdit.PocketStation?4:3;
   Check(east==expected&&crossing.Length-east==4,"profile cross section expected E"+expected+"/W4 actual E"+east+"/W"+(crossing.Length-east)+" station="+station);
   Check(crossing.Count(e=>Vector3.Dot(e.points.Last().V()-e.points[0].V(),direction)>0&&e.id!=Y07MapEdit.Aux&&e.id!=Y07MapEdit.Pocket)==3,"three continuous east through lanes station="+station);
  }
  foreach(var chain in Y07MapEdit.Chains){
   int number=Y07MapEdit.Number(chain);
   foreach(string id in Y07MapEdit.Pieces(chain)){
    var edge=graph.edges.Single(e=>e.id==id);Check(!edge.disabled&&edge.number==number,"lane number "+id);
    Check(graph.edges.Any(e=>!e.disabled&&e.next.Contains(Array.IndexOf(graph.edges,edge))),"incoming "+id);Check(edge.next.Any(i=>!graph.edges[i].disabled),"outgoing "+id);
    Footprint(map,edge);
   }
  }
  var arrows=LaneDirectionPaint.Layout(map);
  foreach(string chain in Y07MapEdit.Chains.Concat(new[]{Y07MapEdit.Pocket})){var edge=graph.edges.Single(e=>e.id==(chain==Y07MapEdit.Pocket?chain:Y07MapEdit.Terminal(chain)));int index=Array.IndexOf(graph.edges,edge);int expected=Y07MapEdit.Mask(chain);Check(arrows.Any(a=>a.edgeIndex==index&&a.turns==expected),"approach paint mask "+chain+" expected="+expected);}
  foreach(string chain in new[]{Y07MapEdit.EInner,Y07MapEdit.E2,Y07MapEdit.EOuter}){
   var tail=graph.edges.Single(e=>e.id==Y07MapEdit.Tail(chain));Check(tail.number==Y07MapEdit.Number(chain)+1,"pocket approach through numbering "+chain);Footprint(map,tail);
   var head=d.lanes.Single(l=>l.id==chain+":part:2");var tailLane=d.lanes.Single(l=>l.id==tail.id);Check(RoadMapAuthoringOperations.Origin(tailLane)==head.id,"through split lineage "+chain);
   var beforePocket=d.lanes.Single(l=>l.id==Y07MapEdit.ThroughSegment(chain,"after-second"));Check(beforePocket.next.Contains(tail.id)&&Vector3.Distance(beforePocket.points.Last().V(),tail.points[0].V())<.001f,"continuous through split "+chain);
   foreach(string label in new[]{"change-window","after-change","second-window","after-second"})Footprint(map,graph.edges.Single(e=>e.id==Y07MapEdit.ThroughSegment(chain,label)));
  }
  var aux=graph.edges.Single(e=>e.id==Y07MapEdit.Aux);var pocket=graph.edges.Single(e=>e.id==Y07MapEdit.Pocket);Footprint(map,aux);Footprint(map,pocket);
  Check(d.lanes.Single(l=>l.id==Y07MapEdit.NorthRight).next.SequenceEqual(new[]{Y07MapEdit.Aux}),"northbound right enters only auxiliary");
  Check(d.lanes.Single(l=>l.id==Y07MapEdit.Aux).next.SequenceEqual(new[]{Y07MapEdit.EOuter+":part:2"}),"auxiliary merges into continuous outer lane");
  Check(d.lanes.Single(l=>l.id==Y07MapEdit.Pocket).next.SequenceEqual(new[]{"join:"+Y07MapEdit.EInner+":-1"}),"inner pocket owns preserved left connector only");
  int auxIndex=Array.IndexOf(graph.edges,aux),pocketIndex=Array.IndexOf(graph.edges,pocket);var pocketRoute=graph.Route(auxIndex,0,pocketIndex,5);Check(pocketRoute!=null&&pocketRoute.path.length<450,"right-entry auxiliary can reach left pocket through adjacent post-merge changes without a junction detour");
  var auxNext=graph.edges[aux.next.Single()];Check(Vector3.Distance(aux.points.Last().V(),auxNext.points[0].V())<.001f,"auxiliary merge seam");
  var auxPath=new AssistedRoadNetwork.Path(aux.points.Select(p=>p.V()).ToArray());var targetPath=new AssistedRoadNetwork.Path(auxNext.points.Select(p=>p.V()).ToArray());Check(Vector3.Dot(auxPath.Tangent(auxPath.length),targetPath.Tangent(0))>.98f,"auxiliary smooth merge tangent");
  var mergeFeatures=d.facilities.Where(f=>f.id==Y07MapEdit.Prefix+"merge-arrow"||f.id==Y07MapEdit.Prefix+"yield-triangle").OrderBy(f=>f.startAt).ToArray();
  Check(mergeFeatures.Length==2&&mergeFeatures[0].type==LaneMergePaint.MergeLeftType&&mergeFeatures[1].type==LaneMergePaint.YieldType,"merge arrow upstream and yield triangle downstream");
  Check(mergeFeatures.All(f=>f.laneId==Y07MapEdit.Aux&&!f.rule&&f.evidence.appearanceConfirmed&&!f.evidence.ruleConfirmed),"auxiliary paint bound with appearance-only evidence");
  Check(LaneMergePaint.ValidateAssignments(map,d).Count==0,"auxiliary full glyph dimensions/curve/clearance validated");
  int mergeStrokes=0;var paintDocument=new RoadMapDocument{lanes=d.lanes};var lanesBeforePaint=JsonUtility.ToJson(paintDocument);
  LaneMergePaint.Paint(map,(name,p,dir,width,length,yellow)=>{
   if(!mergeFeatures.Any(f=>name=="Lane merge paint "+f.id))return;
   foreach(float end in new[]{-length*.5f,0,length*.5f}){var q=p+dir*end;float error;auxPath.Project(q,out error);Check(error+width*.5f<=d.lanes.Single(l=>l.id==aux.id).width*.5f+.002f,"merge/yield stroke stays inside auxiliary");Check(OnRoad(map,q,width*.5f),"merge/yield stroke on physical pavement");}
   mergeStrokes++;
  });Check(mergeStrokes==7,"both explicit auxiliary glyphs rendered");Check(lanesBeforePaint==JsonUtility.ToJson(paintDocument),"merge/yield paint does not mutate navigation permissions");
  var changed=d.lanes.Where(Y07MapEdit.ChangedConnector).Select(l=>l.id).ToArray();
  foreach(var edge in graph.edges.Where(e=>changed.Contains(e.id))){
   int index=Array.IndexOf(graph.edges,edge);var incoming=graph.edges.Where(e=>!e.disabled&&e.next.Contains(index)).ToArray();Check(incoming.Length==1&&edge.next.Length==1,"single connector endpoints "+edge.id);
   var from=incoming[0];var to=graph.edges[edge.next[0]];
   Check(Vector3.Distance(from.points.Last().V(),edge.points[0].V())<.03f&&Vector3.Distance(to.points[0].V(),edge.points.Last().V())<.03f,"connector seam "+edge.id);
   if(edge.change!=0)Check(to.number-from.number==edge.change&&Math.Abs(edge.change)==1,"nonadjacent lane change "+edge.id);
   Footprint(map,edge);
  }
  foreach(var road in map.roads.Where(r=>Y07MapEdit.SourceIds.Contains(r.id)))Check(road.width==26&&road.DisplayLanes==7,"seven-lane physical surface "+road.id);
  var selectedPaths=map.roads.Where(r=>Y07MapEdit.SourceIds.Contains(r.id)).Select(r=>new AssistedRoadNetwork.Path(r.points.Select(p=>p.V()).ToArray())).ToArray();int paint=0;
  RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(name,p,dir,width,length,yellow)=>{
   if(name!="Road edge"&&name!="Lane divider"&&name!="Centre line")return;
   bool near=selectedPaths.Any(path=>{float error;float at=path.Project(p,out error);return error<18&&Mathf.Abs(Vector3.Dot(path.Tangent(at),dir))>.85f;});if(!near)return;
   foreach(float end in new[]{-length*.5f,0,length*.5f})Check(OnRoad(map,p+dir*end,width*.5f),"paint off roadway "+name+" "+p.ToString("F2"));paint++;
  });Check(paint>200,"Y07 world paint exercised");
  var catalog=PassengerStopCatalog.For(map);var stops=catalog.Stops;LogStops("VALIDATION",catalog,map);Check(stops.Length==39,"39 stop catalog preserved actual="+stops.Length+" rejected="+string.Join(" / ",catalog.Rejected));
  var stable=stops.Single(s=>s.name=="은계 승하차 2");Check(graph.edges[stable.laneEdge].id==Y07MapEdit.WOuter+":part:1"&&Mathf.Abs(stable.laneAt-15.9274759f)<.001f,"existing pickup lane identity/distance preserved");
  var game09=catalog.Entries.Single(e=>e.StopId=="EG-GAME-09");var game09b=catalog.Entries.Single(e=>e.StopId=="EG-GAME-09-B");
  Check(graph.edges[game09.Stop.laneEdge].id==Y07MapEdit.WOuter+":part:2"&&Mathf.Abs(game09.Stop.laneAt-40)<.001f,"game09 retains exact outer lane and station");
  Check(graph.edges[game09b.Stop.laneEdge].id==Y07MapEdit.NorthWestEntry&&Mathf.Abs(game09b.Stop.laneAt-398.4125977f)<.001f,"opposite game09 retains original binding");
  Check(catalog.OppositeIndex(game09.Index)==game09b.Index,"game09 opposite pair preserved");
  int pairs=0;foreach(var a in stops)foreach(var b in stops)if(a!=b){Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"route "+a.name+" -> "+b.name);pairs++;}
  Check(pairs==1482,"all directed stop pairs");Debug.Log("Y07_CHECKS_OK eastThrough3 aux4to3 pocket3to4 W7=L/S/S/R E8=L/S/S/S stops="+stops.Length+" pairs="+pairs+" paint="+paint+" stop-world-position-follows-reviewed-outer-curb");
 }
 static bool Crosses(MapPoint[] points,Vector3 p,Vector3 forward){for(int i=1;i<points.Length;i++){float a=Vector3.Dot(points[i-1].V()-p,forward),b=Vector3.Dot(points[i].V()-p,forward);if(a<=0&&b>0||b<=0&&a>0)return true;}return false;}
 static bool OnRoad(MapData map,Vector3 point,float margin){return map.roads.Any(road=>{var path=new AssistedRoadNetwork.Path(road.points.Select(p=>p.V()).ToArray());float error;path.Project(point,out error);return error+margin<=road.width*.5f+.002f;});}
 internal static void Footprint(MapData map,HdEdge edge){
  var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());
  string wholeReason;if(EungyeConnectivityRepair.FootprintClear(map,edge,out wholeReason))return;
  Check(wholeReason=="reverse/abrupt heading","vehicle footprint "+edge.id+" reason="+wholeReason);
  float step=edge.id.StartsWith("join:")?1:4,window=edge.id.StartsWith("join:")?2:6;
  for(float start=0;start<path.length;start+=step){float end=Mathf.Min(path.length,start+window);var points=new List<MapPoint>();for(float at=start;at<end;at+=.5f){var p=path.Position(at);points.Add(new MapPoint{x=p.x,z=p.z});}var last=path.Position(end);points.Add(new MapPoint{x=last.x,z=last.z});string reason;Check(EungyeConnectivityRepair.FootprintClear(map,new HdEdge{points=points.ToArray()},out reason),"vehicle footprint "+edge.id+" station="+start.ToString("F2")+" reason="+reason);}
 }
}

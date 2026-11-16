using System;
using System.Linq;
using UnityEngine;

// Commit only the reviewed P07 closure and P12/P21 paint metadata.
// Roundabout/Green31 candidates remain separate until their checks pass.
public static class PinkLivingRoadMapApply {
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("PINK_LIVING: "+reason);}
 public static void Validate(ProjectMapPackage p,RoadMapDocument d){
  var errors=RoadMapValidator.Validate(p.baseline,d).Where(e=>e.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(10).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);
  Check(!map.hdEdges.Any(e=>!e.disabled&&PinkPassageMapEdit.Family(e.sourceId)),"pedestrian passage reopened");
  var arrows=LaneDirectionPaint.Layout(map);
  Check(!arrows.Any(a=>PinkEastUnmarkedMapEdit.Lanes.Contains(map.hdEdges[a.edgeIndex].id)),"unmarked street arrows returned");
  int interiorLines=0,edges=0;
  RoadPaintGeometry.Paint(map,null,(name,pos,heading,width,length,yellow)=>{
   if(pos.x>872&&pos.x<881&&(pos.z>104&&pos.z<147||pos.z>183&&pos.z<237)&&Mathf.Abs(heading.z)>.85f){
    if(name=="Centre line"||name=="Lane divider")interiorLines++;
    if(name=="Road edge")edges++;
   }
  });
  Check(interiorLines==0&&edges>0,"scoped compiled paint mismatch");
  var before=p.Compose();EungyeConnectivityRepair.Apply(before,p.baseline);
  var oldStops=PassengerStopCatalog.For(before);var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Entries.Select(e=>e.StopId).SequenceEqual(oldStops.Entries.Select(e=>e.StopId)),"ordered stop identities changed");
  Check(catalog.Stops.Length==39,"stop count changed");
  var graph=new HdRoadGraph(map);int pairs=0;
  foreach(var a in catalog.Stops)foreach(var b in catalog.Stops)if(a!=b){var route=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);Check(route!=null,"missing route "+a.name+" -> "+b.name);Check(!route.edges.Any(i=>PinkPassageMapEdit.Family(graph.edges[i].sourceId)),"route uses pedestrian passage");pairs++;}
  Check(pairs==1482,"stop-pair coverage");
  Debug.Log("PINK_LIVING_CHECKS_OK passage-closed unmarked-P12-P21 stops39 pairs1482");
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);
  Check(p.document.revision==22&&p.contentHash==PinkPassageMapEdit.RequiredHash,"requires exact reviewed r22");
  var passage=PinkPassageMapEdit.Preview(p);
  var d=PinkEastUnmarkedMapEdit.Preview(ProjectMapPackage.Create(p.baseline,passage));
  Validate(p,d);d.revision++;d.savedAt=DateTime.UtcNow.ToString("o");d.projectRevision="";
  var next=ProjectMapPackage.Create(p.baseline,d);Check(next.baselineHash==p.baselineHash,"baseline changed");
  ProjectMapStore.Save(root,next,token);ProjectMapBuild.Prepare();var saved=ProjectMapStore.Load(root);
  Check(saved.contentHash==next.contentHash&&saved.document.revision==23,"saved package mismatch");Validate(p,saved.document);
  Debug.Log("PINK_LIVING_APPLIED revision="+saved.document.revision+" contentHash="+saved.contentHash);
 }
}

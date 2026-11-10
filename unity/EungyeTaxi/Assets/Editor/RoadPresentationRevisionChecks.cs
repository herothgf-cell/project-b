using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Presentation regressions: no signal authority or map inputs are changed.
public static class RoadPresentationRevisionChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception("ROAD_PRESENTATION_REVISION: "+reason);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 public static void Signals(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var host=new GameObject("four aperture signal check");
  try {
   var system=host.AddComponent<TrafficSystem>();system.Build(map,false);system.manualClock=true;
   int heads=0,closed=0,protectedHeads=0;
   foreach(var j in system.junctions)for(int i=0;i<j.lamps.Count;i++){
    var root=j.lamps[i][0].transform.parent;
    var lamps=root.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Signal lamp ")).OrderBy(r=>r.transform.localPosition.x).ToArray();
    Check(lamps.Length==4,j.name+" physical head has red/yellow/left/green apertures; actual="+lamps.Length);
    Check(lamps[0]==j.lamps[i][0]&&lamps[1]==j.lamps[i][1]&&lamps[3]==j.lamps[i][2],j.name+" physical order matches HUD four slots");
    bool authorized=system.HasLeftMovement(j,j.lampAxes[i],j.lampSigns[i]);
    if(!authorized){closed++;Check(j.leftLamps[i].Length==0,j.name+" closed approach has no permissive arrow");for(int t=0;t<180;t+=3){system.manualTime=t;system.SendMessage("Update");Check(lamps[2].sharedMaterial.name=="Signal off 2",j.name+" unavailable left aperture stays unlit");}}
    else if(j.leftTurnMode==LeftTurnMode.ProtectedShared){protectedHeads++;Check(j.leftLamps[i].Length>0,j.name+" authorized protected arrow remains visible");}
    heads++;
   }
   Check(heads>0&&closed>0&&protectedHeads>0,"actual map covers protected and no-left heads");
   Debug.Log("ROAD_PRESENTATION_SIGNALS_OK heads="+heads+" closed="+closed);
  } finally {UnityEngine.Object.DestroyImmediate(host);}
 }
 public static void Navigation(){
  var method=typeof(DrivingReadability).GetMethod("FollowingManeuverBounds",BindingFlags.Public|BindingFlags.Static);
  Check(method!=null,"following maneuver uses inspectable real drawing bounds");
  foreach(float x in new[]{0f,29f,500f}){
   var following=(Rect)method.Invoke(null,new object[]{x,411f});var card=new Rect(x-5,411,320,140);
   Check(following.xMin>=x+198&&following.xMax<=card.xMax-7&&following.yMin>=card.yMin+4&&following.yMax<=card.yMin+95,"small following cue stays inside right side of blue card "+following);
   Check(following.width<=104&&following.height<=72,"secondary maneuver reserves room for primary road and lane guidance");
  }
  Debug.Log("ROAD_PRESENTATION_NAVIGATION_OK");
 }
 public static void AuthoredSignals(){
  var host=new GameObject("authored four aperture check");
  try {
   var program=new RoadMapSignalProgram{junctionId="fixture",groups=new[]{new RoadMapSignalGroup{id="s"},new RoadMapSignalGroup{id="l",turn=-1},new RoadMapSignalGroup{id="w",pedestrian=true}},stages=new[]{new RoadMapStage{groups=new[]{"s","l","w"},green=10}}};
   var doc=new RoadMapDocument{signals=new[]{program},facilities=new[]{new RoadMapFacility{id="vehicle",kind="vehicleSignal",junctionId="fixture",groupId="s"},new RoadMapFacility{id="pedestrian",kind="pedestrianSignal",junctionId="fixture",groupId="w"}}};
   RoadMapFacilities.Build(doc,host.transform,null);
   var vehicle=host.transform.Find("사용자 시설 vehicle");var pedestrian=host.transform.Find("사용자 시설 pedestrian");
   Check(vehicle.Find("좌회전 등화")!=null,"authored vehicle head reserves separate left aperture");
   Check(vehicle.Find("적색").localPosition.x<vehicle.Find("황색").localPosition.x&&vehicle.Find("황색").localPosition.x<vehicle.Find("좌회전 등화").localPosition.x&&vehicle.Find("좌회전 등화").localPosition.x<vehicle.Find("녹색").localPosition.x,"authored vehicle aperture order matches HUD");
   Check(pedestrian.Find("좌회전 등화")==null&&pedestrian.Find("황색")==null,"pedestrian display retains red/green only");
   var display=vehicle.GetComponent<RoadMapLamp>();display.SendMessage("Update");Check(display.green.sharedMaterial.name=="Editor green","authored forward group still drives existing green output");
   var arrow=vehicle.Find("좌회전 화살표");Check(arrow!=null&&arrow.GetComponent<Renderer>().sharedMaterial.name=="Editor green","authored active left group drives separate arrow");
   program.stages[0].groups=new[]{"s","w"};display.SendMessage("Update");Check(arrow.GetComponent<Renderer>().sharedMaterial.name=="Editor lamp off","authored inactive left movement leaves arrow unlit while forward stays green");
   Debug.Log("ROAD_PRESENTATION_AUTHORED_SIGNALS_OK");
  } finally {UnityEngine.Object.DestroyImmediate(host);}
 }
 public static void CompiledUturn(){
  // This active graph has an authorized U-turn but deliberately no editor document.
  var incoming=new HdEdge{id="incoming",sourceId="approach",status="surveyed",points=new[]{P(0,-30),P(0,0)},next=new[]{1}};
  var join=new HdEdge{id="join:fixture:uturn",turn=UturnMovement.Turn,points=new[]{P(0,0),P(-6,6),P(-12,0)},next=new[]{2}};
  var outgoing=new HdEdge{id="outgoing",points=new[]{P(-12,0),P(-12,-30)},next=new int[0]};
  var map=new MapData{hybrid=true,hdEdges=new[]{incoming,join,outgoing},hdMarks=new HdMark[0]};
  var before=JsonUtility.ToJson(map);var zones=UturnRoadMarkings.Build(map);
  Check(zones.Count==1&&zones[0].incomingId=="incoming"&&zones[0].joinId==join.id,"compiled authorized U-turn receives median opening without editor document");
  Check(Math.Abs(zones[0].width-12)<.001f&&Math.Abs(zones[0].length-8)<.001f,"compiled median opening follows actual connector span");
  int median=0,arrows=0;UturnRoadMarkings.Paint(map,(n,p,d,w,l,y)=>{if(n.StartsWith("U-turn permitted median"))median++;else if(n.StartsWith("U-turn approach arrow"))arrows++;});
  Check(median==3&&arrows>0,"compiled authorized U-turn paints clear opening and approach arrow even outside provisional status");
  Check(JsonUtility.ToJson(map)==before,"paint never adds legal connections or changes compiled map");
  join.disabled=true;Check(UturnRoadMarkings.Build(map).Count==0,"disabled connector creates no opening");join.disabled=false;
  outgoing.disabled=true;Check(UturnRoadMarkings.Build(map).Count==0,"disabled U-turn exit creates no opening");outgoing.disabled=false;
  incoming.next=new int[0];Check(UturnRoadMarkings.Build(map).Count==0,"nearby connector without legal incoming connection creates no opening");
  Debug.Log("ROAD_PRESENTATION_COMPILED_UTURN_OK");
 }
 public static void AuthorizedUturnVisibility(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();string before=JsonUtility.ToJson(map);
  var zones=UturnRoadMarkings.Build(map);Check(zones.Count==9,"nine existing authored U-turn openings remain the only authored zones");
  int median=0;UturnRoadMarkings.Paint(map,(n,p,d,w,l,y)=>{if(!n.StartsWith("U-turn permitted median"))return;median++;Check(w>=.26f&&w<=.32f&&!y&&l<=1.5f,"actual authorized median opening has clearly visible white strokes, width="+w);});Check(median>=18,"actual six authorized openings emit separated median strokes");
  var arrows=LaneDirectionPaint.Layout(map);arrows.AddRange(UturnRoadMarkings.FallbackArrows(map));
  foreach(var zone in zones){var approach=arrows.FirstOrDefault(a=>map.hdEdges[a.edgeIndex].id==zone.incomingId&&(a.turns&LaneDirectionPaint.Uturn)!=0);Check(approach!=null,"existing authorized approach retains dedicated U-turn glyph "+zone.joinId);
   foreach(var glyph in LaneDirectionPaint.Glyphs(approach).Where(a=>a.turns==LaneDirectionPaint.Uturn))LaneDirectionPaint.Paint(glyph,"visibility",(n,p,d,w,l,y)=>Check(w>=.21f&&w<=.25f,"actual U-turn glyph has more visible strokes, width="+w));
  }
  Check(JsonUtility.ToJson(map)==before,"readability paint preserves exact authored map and movement permissions");Debug.Log("ROAD_PRESENTATION_AUTHORIZED_UTURN_VISIBILITY_OK zones="+zones.Count+" medianStrokes="+median);
 }
 public static void Run(){Signals();Navigation();CompiledUturn();AuthoredSignals();AuthorizedUturnVisibility();}
}

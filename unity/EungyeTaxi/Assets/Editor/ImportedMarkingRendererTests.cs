using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ImportedMarkingRendererTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("IMPORTED_MARKING_RENDERER: "+message);}
 static HdMark Mark(string id,string type,bool reverse){return new HdMark{id=id,type=type,kind="506",points=reverse?new[]{new MapPoint{x=10,z=0},new MapPoint{x=0,z=0}}:new[]{new MapPoint{x=0,z=0},new MapPoint{x=10,z=0}}};}
 static int Vertices(params HdMark[] marks){var root=new GameObject("marking test");try{HdRouteTrial.DrawMarkings(new MapData{hdMarks=marks},root.transform);var child=root.transform.Find("원본 노면선 1");return child.GetComponent<MeshFilter>().sharedMesh.vertexCount;}finally{UnityEngine.Object.DestroyImmediate(root);}}
 static Vector3[] YellowVertices(MapData map){var root=new GameObject("roadside marking test");try{HdRouteTrial.DrawMarkings(map,root.transform);return root.transform.Find("원본 노면선 1").GetComponent<MeshFilter>().sharedMesh.vertices;}finally{UnityEngine.Object.DestroyImmediate(root);}}
 static Vector3[] WhiteVertices(MapData map){var root=new GameObject("divider marking test");try{HdRouteTrial.DrawMarkings(map,root.transform);return root.transform.Find("원본 노면선 0").GetComponent<MeshFilter>().sharedMesh.vertices;}finally{UnityEngine.Object.DestroyImmediate(root);}}
 static int QuadsNear(Vector3[] vertices,Vector3 point,float radius){int count=0;for(int i=0;i<vertices.Length;i+=4){var centre=(vertices[i]+vertices[i+1]+vertices[i+2]+vertices[i+3])*.25f;if(Vector3.Distance(centre,point)<radius)count++;}return count;}
 static bool SpansX(Vector3[] vertices,float x){for(int i=0;i<vertices.Length;i+=4){float min=float.MaxValue,max=float.MinValue;for(int j=0;j<4;j++){min=Mathf.Min(min,vertices[i+j].x);max=Mathf.Max(max,vertices[i+j].x);}if(min<x&&max>x)return true;}return false;}
 public static void Run(){
  SharedLongitudinalRegressions();
  GeneratedCrosswalkRegressions();
  ZeroReachCrosswalkRegressions();
  ReplacedStopLineRegressions();
  UturnMedianRegressions();
  UturnArrowFallbackRegressions();
  int single=Vertices(Mark("single","111",false));
  Check(single==20,"single solid control uses five strips");
  Check(Vertices(Mark("double","121",false))==2*single,"121 draws two separate solid strokes");
  Check(Vertices(Mark("first","111",false),Mark("reverse","111",true))==single,"reverse duplicate geometry is rendered once");
  var roadside=new HdMark{id="roadside",type="111",kind="515",points=new[]{new MapPoint{x=-20,z=0},new MapPoint{x=20,z=0}}};
  var crossing=new MapRoad{id="local:cross",width=10,points=new[]{new MapPoint{x=0,z=-20},new MapPoint{x=0,z=20}}};
  var clipped=YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}});
  Check(clipped.Length>0&&QuadsNear(clipped,new Vector3(-15,0,0),2)>0&&QuadsNear(clipped,new Vector3(15,0,0),2)>0,"transverse roadside marking remains on both sides of crossing road");
  Check(QuadsNear(clipped,Vector3.zero,4.5f)==0,"transverse roadside marking is absent from local drivable surface");
  roadside.kind="501";Check(QuadsNear(YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}}),Vector3.zero,2)>0,"imported 501 centerline survives a crossing road");
  roadside.kind="515";roadside.points=new[]{new MapPoint{x=5,z=-20},new MapPoint{x=5,z=20}};
  Check(QuadsNear(YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}}),new Vector3(5,0,0),2)>0,"parallel roadside marking survives beside its road");
  roadside.kind="505";roadside.points=new[]{new MapPoint{x=-20,z=0},new MapPoint{x=20,z=0}};
  Check(QuadsNear(YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}}),Vector3.zero,4.5f)==0,"road-edge kind 505 clips at a transverse local crossing");
  roadside.kind="515";roadside.type="112";var dashed=YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}});
  Check(!SpansX(dashed,4.5f)&&SpansX(dashed,6)&&!SpansX(dashed,8.5f),"dashed 515 clips its road-facing end and keeps the source dash pattern");
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var actual=package.baseline;
  var realRoad=actual.roads.First(r=>r.id=="37612023:A0020000:232:0");
  var realMarks=actual.hdMarks.Where(m=>m.id=="B2218G610306"||m.id=="B2218G610454").ToArray();
  Check(realMarks.Length==2,"J10580 imported kind-515 source marks exist");
  var actualVertices=YellowVertices(new MapData{roads=new[]{realRoad},hdMarks=realMarks});
  Check(QuadsNear(actualVertices,new Vector3(1066.10f,0,162.93f),1.5f)==0&&QuadsNear(actualVertices,new Vector3(1051.55f,0,123.94f),1.5f)==0,"J10580 transverse 515 paint is absent from composed local approach");
  var north=realMarks.First(m=>m.id=="B2218G610306");var south=realMarks.First(m=>m.id=="B2218G610454");
  Check(QuadsNear(actualVertices,north.points[0].V(),2)>0&&QuadsNear(actualVertices,south.points[south.points.Length-1].V(),2)>0,"J10580 source roadside fragments outside the crossing survive");
  var authored=new MapData{
   hdMarks=new[]{new HdMark{id="old-center",type="111",kind="501",points=new[]{new MapPoint{x=0,z=-15},new MapPoint{x=0,z=10}}},new HdMark{id="valid-edge",type="111",kind="505",points=new[]{new MapPoint{x=2,z=-15},new MapPoint{x=2,z=10}}}},
   hdEdges=new[]{new HdEdge{id="edited-lane",points=new[]{new MapPoint{x=0,z=-20},new MapPoint{x=0,z=20}}}},
   junctionRules=new[]{new HybridJunctionRule{id="edited-junction",x=0,z=-10,approaches=new[]{new HybridApproach{width=10}}}},
   editorDocument=new RoadMapDocument{lanes=new[]{new RoadMapLane{id="edited-lane",width=3.5f}},facilities=new[]{new RoadMapFacility{id="edited-crossing",kind="crosswalk",junctionId="edited-junction",x=0,z=0,width=10,length=2.4f,yaw=0}}}
  };
  var authoredVertices=YellowVertices(authored);
  Check(QuadsNear(authoredVertices,new Vector3(0,0,0),.6f)==0,"authored crosswalk has no imported yellow paint");
  Check(QuadsNear(authoredVertices,new Vector3(0,0,-6),.6f)==0,"outdated centerline inside edited junction lane is clipped");
  Check(QuadsNear(authoredVertices,new Vector3(0,0,7),.6f)>0,"source centerline beyond crosswalk on approach survives");
  Check(QuadsNear(authoredVertices,new Vector3(2,0,-6),.6f)>0,"actual lane edge outside lane interior survives junction mask");
  authored.hdMarks[0].type="112";var dashedEdited=YellowVertices(authored);
  Check(QuadsNear(dashedEdited,new Vector3(0,0,4),.6f)>0&&QuadsNear(dashedEdited,new Vector3(0,0,7),.6f)==0,"authored-junction clipping retains imported dashed centreline cadence");
  var composed=package.Compose();var j5057Marks=composed.hdMarks.Where(m=>m.id=="B2218G612626"||m.id=="B2218G610908"||m.id=="B2218G610911"||m.id=="B2218G612624").ToArray();
  Check(j5057Marks.Length==4,"J5057 source yellow centerline, V and roadside marks exist");
  var reviewedVertices=YellowVertices(new MapData{roads=composed.roads,hdEdges=composed.hdEdges,hdMarks=j5057Marks,junctionRules=composed.junctionRules,editorDocument=composed.editorDocument});
  Check(QuadsNear(reviewedVertices,new Vector3(508.7f,0,474.5f),.6f)==0,"J5057 imported 501 does not cross authored north crosswalk");
  Check(QuadsNear(reviewedVertices,new Vector3(508.7f,0,469.5f),.6f)==0&&QuadsNear(reviewedVertices,new Vector3(502.9f,0,469.5f),.6f)==0,"J5057 north centerline and V are absent from active junction lanes");
  Check(QuadsNear(reviewedVertices,new Vector3(508.8f,0,476.8f),.6f)>0,"J5057 imported centerline remains north of authored crosswalk");
  var rule=composed.junctionRules.First(x=>x.id=="J5057_4449");
  var junction=new TrafficJunction{name=rule.id,center=rule.V(),direction=new Vector3(rule.dx,0,rule.dz).normalized};
  var generatedYellow=new List<Vector3>();var generatedWhite=new List<Vector3>();
  RoadPaintGeometry.Paint(composed,new List<TrafficJunction>{junction},(name,p,dir,width,length,isYellow)=>{
   if(p.x<498||p.x>518||p.z<462||p.z>488)return;
   if(isYellow)generatedYellow.Add(p);else generatedWhite.Add(p);
  });
  Func<Vector3,bool> crosswalkBand=p=>p.x>=499.8f&&p.x<=515.7f&&p.z>=473.3f&&p.z<=475.7f;
  Func<Vector3,bool> inner=p=>p.x>=499.8f&&p.x<=515.7f&&p.z>=468f&&p.z<473.3f;
  Func<Vector3,bool> upstream=p=>p.x>=499.8f&&p.x<=515.7f&&p.z>478f&&p.z<485f;
  Debug.Log("J5057_GENERATED_PAINT red-cross="+generatedYellow.Count(crosswalkBand)+" white-cross="+generatedWhite.Count(crosswalkBand)+" red-inner="+generatedYellow.Count(inner)+" white-inner="+generatedWhite.Count(inner)+" upstream="+(generatedYellow.Count(upstream)+generatedWhite.Count(upstream)));
  Check(generatedYellow.Count(crosswalkBand)==0&&generatedWhite.Count(crosswalkBand)==0,"J5057 generated dividers stay out of authored north crosswalk");
  Check(generatedYellow.Count(inner)==0&&generatedWhite.Count(inner)==0,"J5057 generated dividers stop at authored crossing on junction side");
  Check(generatedYellow.Count(upstream)+generatedWhite.Count(upstream)>0,"J5057 valid generated approach dividers remain upstream");
  var sideIds=new[]{"B2218G610330","B2218G610901","B2218G613342","B222BC004751"};
  var sideMarks=composed.hdMarks.Where(m=>sideIds.Contains(m.id)).ToArray();Check(sideMarks.Length==sideIds.Length,"J5057 east/west imported hooks and outer source edge exist");
  var sideMap=new MapData{roads=composed.roads,hdEdges=composed.hdEdges,hdMarks=sideMarks,junctionRules=composed.junctionRules,editorDocument=composed.editorDocument};
  var sideVertices=YellowVertices(sideMap);
  Check(QuadsNear(sideVertices,new Vector3(493.9f,0,458.7f),.65f)==0&&QuadsNear(sideVertices,new Vector3(520.5f,0,454.7f),.65f)==0,"J5057 west/east interior yellow hooks end at generated crosswalks");
  Check(QuadsNear(sideVertices,new Vector3(523.2f,0,457.2f),.65f)==0,"J5057 east no-stopping tip does not enter generated crossing");
  Check(QuadsNear(sideVertices,new Vector3(475.5f,0,436f),.65f)>0&&QuadsNear(sideVertices,new Vector3(490.7f,0,429.8f),.65f)>0,"J5057 observed outer yellow kerb persists outside crossing and inner channel");
  Debug.Log("IMPORTED_MARKING_RENDERER_TEST_OK");
 }
 static void SharedLongitudinalRegressions(){
  var points=new[]{new MapPoint{x=20,z=-30},new MapPoint{x=20,z=30}};
  var divider=new HdMark{id="wide-divider",type="211",kind="506",points=points};
  var map=new MapData{hdMarks=new[]{divider},junctionRules=new[]{new HybridJunctionRule{id="wide",dx=0,dz=1,crosswalk=true,stopLine=true,approaches=new[]{new HybridApproach{dx=0,dz=1,width=42.6f},new HybridApproach{dx=1,dz=0,width=14}}}}};
  var white=WhiteVertices(map);
  Check(QuadsNear(white,new Vector3(20,0,6),.8f)==0,"white imported506 divider clipped at physical wide-junction corner beyond18m");
  Check(QuadsNear(white,new Vector3(20,0,-20),.8f)>0&&QuadsNear(white,new Vector3(20,0,20),.8f)>0,"white divider fragments survive outside approach opening");
  divider.type="212";white=WhiteVertices(map);
  Check(QuadsNear(white,new Vector3(20,0,-23),.6f)>0&&QuadsNear(white,new Vector3(20,0,-20),.6f)==0,"imported506 retains source dash stations after junction clipping");
  divider.kind="501";divider.type="111";var yellow=YellowVertices(map);
  Check(QuadsNear(yellow,new Vector3(20,0,6),.8f)==0&&QuadsNear(yellow,new Vector3(20,0,-20),.8f)>0,"imported501 shares physical clearance while upstream source survives");
  var mask=new HdRouteTrial.LongitudinalJunctionPaintMask(map);
  foreach(var kind in new[]{"525","530","532","505","515"}){divider.kind=kind;Check(!mask.MightAffect(divider),"non-longitudinal source kind retains separate semantics "+kind);}
  divider.kind="530";divider.type="211";white=WhiteVertices(map);
  Check(QuadsNear(white,new Vector3(20,0,6),1.2f)>0,"source stop/turn-guide categories do not receive blanket longitudinal clipping");
  var empty=new HdRouteTrial.LongitudinalJunctionPaintMask(new MapData());divider.kind="506";
  Check(!empty.MightAffect(divider)&&!empty.Covers(Vector3.zero,Vector3.forward,.25f),"minimal maps without rules create no paint exclusion");
 }
 static void GeneratedCrosswalkRegressions(){
  var rule=new HybridJunctionRule{id="generated-only",dx=0,dz=1,crosswalk=true,stopLine=true,approaches=new[]{new HybridApproach{dx=0,dz=1,width=42.6f},new HybridApproach{dx=1,dz=0,width=14}}};
  var hook=new HdMark{id="kerb-hook",kind="515",type="111",points=new[]{new MapPoint{x=20,z=-30},new MapPoint{x=20,z=30}}};
  var map=new MapData{junctionRules=new[]{rule},hdMarks=new[]{hook}};
  var bands=ReviewedCrosswalkGeometry.Build(map);Check(bands.Count==2&&bands.All(b=>!b.authored),"generated crosswalk zones exist without any authored facilities or document");
  foreach(string kind in new[]{"505","515"}){hook.kind=kind;var vertices=YellowVertices(map);Check(QuadsNear(vertices,new Vector3(20,0,-8.8f),.9f)==0,"generated zebra clips actual kerb hook kind "+kind);Check(QuadsNear(vertices,new Vector3(20,0,-20),.8f)>0,"actual outer kerb preserved upstream kind "+kind);}
  hook.type="212";hook.points=new[]{new MapPoint{x=0,z=-28},new MapPoint{x=0,z=30}};
  foreach(string kind in new[]{"502","503"}){hook.kind=kind;var vertices=WhiteVertices(map);Check(QuadsNear(vertices,new Vector3(0,0,-2),.8f)==0,"longitudinal/taper kind clipped in physical opening "+kind);Check(QuadsNear(vertices,new Vector3(0,0,-26),.8f)>0,"source approach dash retained "+kind);}
  hook.kind="525";var guide=WhiteVertices(map);Check(QuadsNear(guide,new Vector3(0,0,-2),.8f)>0,"legitimate dashed turn guide remains inside intersection");Check(QuadsNear(guide,new Vector3(0,0,-8.8f),.9f)==0,"turn guide ends before zebra band");
 }
 static void ZeroReachCrosswalkRegressions(){
  var map=new MapData{junctionRules=new[]{new HybridJunctionRule{id="midblock",x=648,z=257.3f,dx=1,dz=0,crosswalk=false}},editorDocument=new RoadMapDocument{facilities=new[]{new RoadMapFacility{kind="crosswalk",junctionId="midblock",x=648,z=257.3f,yaw=90,width=17.6f,length=4}}}};
  var zones=ReviewedCrosswalkGeometry.Build(map);Check(zones.Count==1&&zones[0].authored&&Mathf.Abs(zones[0].reach)<.001f,"authored zero-reach crossing exists even with generated crossing disabled");
  var centre=new Vector3(648,0,257.3f);Check(zones[0].InBand(centre,0)&&!zones[0].JunctionSide(centre+Vector3.right*5,0)&&!zones[0].Core(centre+Vector3.right*5,18),"midblock masks stripe band without inventing a junction-side corridor");
  Check(RoadPaintGeometry.InOpening(centre,new List<RoadJunctionGeometry.Clearance>(),new List<RoadJunctionGeometry.Opening>(),zones,0),"compiled paint checks zero-reach authored stripe band");
  var line=new HdMark{id="midblock-hook",type="111",kind="515",points=new[]{new MapPoint{x=638,z=257.3f},new MapPoint{x=658,z=257.3f}}};map.hdMarks=new[]{line};
  foreach(string kind in new[]{"505","515"}){line.kind=kind;var vertices=YellowVertices(map);Check(QuadsNear(vertices,centre,.8f)==0,"midblock zero-reach zebra clips kerb kind "+kind);Check(QuadsNear(vertices,centre+Vector3.right*6,.8f)>0,"midblock preserves kerb outside band "+kind);}
  line.kind="525";line.type="212";var guide=WhiteVertices(map);Check(QuadsNear(guide,centre,.8f)==0&&QuadsNear(guide,centre-Vector3.right*9,.8f)>0,"zero-reach crossing trims dashed guide only inside stripe band");
 }
 static void ReplacedStopLineRegressions(){
  var rule=new HybridJunctionRule{id="stop-replacement",dx=0,dz=1,stopLine=true,approaches=new[]{new HybridApproach{dx=0,dz=1,width=14},new HybridApproach{dx=1,dz=0,width=30}}};
  var old=new HdMark{id="old-stop",type="211",kind="530",points=new[]{new MapPoint{x=1,z=-25},new MapPoint{x=6,z=-25}}};
  var map=new MapData{hybrid=true,junctionRules=new[]{rule},hdMarks=new[]{old}};
  Check(WhiteVertices(map).Length==0,"source transverse stop line is replaced by the runtime approach stop");
  old.points=new[]{new MapPoint{x=1,z=-70},new MapPoint{x=6,z=-70}};Check(WhiteVertices(map).Length>0,"distant source stop line outside replacement approach survives");
  old.points=new[]{new MapPoint{x=1,z=-25},new MapPoint{x=1,z=-20}};Check(WhiteVertices(map).Length>0,"parallel source category530 is not treated as the approach stop line");
  old.points=new[]{new MapPoint{x=-6,z=-25},new MapPoint{x=-1,z=-25}};Check(WhiteVertices(map).Length>0,"opposite carriageway source mark is not assigned to incoming approach");
  rule.stopLine=false;old.points=new[]{new MapPoint{x=1,z=-25},new MapPoint{x=6,z=-25}};Check(WhiteVertices(map).Length>0,"source stop line survives without a runtime replacement");
  var composed=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var ids=new[]{"B2218G610924","B222BC004101"};
  var marks=composed.hdMarks.Where(m=>ids.Contains(m.id)).ToArray();Check(marks.Length==2,"J1927 two original transverse source stop lines are present");
  composed.hdMarks=marks;Check(WhiteVertices(composed).Length==0,"J1927 stale source stops are absent on both carriageways while runtime stops remain canonical");
 }
 static void UturnMedianRegressions(){
  var head=new RoadMapLane{id="uturn-head",points=new[]{new MapPoint{x=0,z=-20},new MapPoint{x=0,z=0}},next=new[]{"uturn-join"}};
  var join=new RoadMapLane{id="uturn-join",turn=-2,points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=-6,z=6},new MapPoint{x=-12,z=0}}};
  var map=new MapData{editorDocument=new RoadMapDocument{lanes=new[]{head,join}},hdMarks=new[]{new HdMark{id="uturn-median",kind="501",type="111",points=new[]{new MapPoint{x=-6,z=-8},new MapPoint{x=-6,z=16}}}}};
  var zones=UturnRoadMarkings.Build(map);Check(zones.Count==1&&Mathf.Abs(zones[0].width-12)<.001f&&Mathf.Abs(zones[0].length-8)<.001f,"U-turn median dimensions use across heading and furthest join station plus2m");
  Check(UturnRoadMarkings.Covers(zones,new Vector3(-6,0,4))&&!UturnRoadMarkings.Covers(zones,new Vector3(-3,0,4))&&!UturnRoadMarkings.Covers(zones,new Vector3(-6,0,-3)),"U-turn replacement masks only the bounded median strip");
  var strokes=new List<Vector3>();UturnRoadMarkings.Paint(map,(name,p,dir,width,length,yellow)=>{Check(!yellow&&length<=1.5f&&Vector3.Dot(dir,Vector3.forward)>.99f,"U-turn median is a white broken line along incoming heading");strokes.Add(p);});Check(strokes.Count==3,"U-turn allowed interval paints three separated strokes");
  var yellow=YellowVertices(map);Check(QuadsNear(yellow,new Vector3(-6,0,4),.7f)==0&&QuadsNear(yellow,new Vector3(-6,0,-5),.7f)>0&&QuadsNear(yellow,new Vector3(-6,0,12),.7f)>0,"yellow501 ends only within U-turn interval and remains solid on both ends");
  map.hdMarks[0].points=new[]{new MapPoint{x=-7.3f,z=-8},new MapPoint{x=-7.3f,z=16}};zones=UturnRoadMarkings.Build(map);
  Check(Mathf.Abs(zones[0].sourceOffset-1.3f)<.001f&&Mathf.Abs(zones[0].start.x+7.3f)<.001f,"white U-turn median follows nearby aligned source501 axis");
  Check(UturnRoadMarkings.Covers(zones,new Vector3(-6,0,4))&&UturnRoadMarkings.Covers(zones,new Vector3(-7.3f,0,4))&&!UturnRoadMarkings.Covers(zones,new Vector3(-8.3f,0,4))&&!UturnRoadMarkings.Covers(zones,new Vector3(-6,0,-3)),"source-offset mask covers both median axes only in allowed interval without symmetric overreach");
  Check(QuadsNear(YellowVertices(map),new Vector3(-7.3f,0,4),.7f)==0,"offset source501 does not remain beside white U-turn strokes");
  map.hdMarks[0].points=new[]{new MapPoint{x=-6,z=-8},new MapPoint{x=-6,z=16}};
  map.hdMarks[0].kind="506";map.hdMarks[0].type="211";Check(QuadsNear(WhiteVertices(map),new Vector3(-6,0,4),1.2f)>0,"white lane divider is not erased by U-turn median mask");
  head.disabled=true;Check(UturnRoadMarkings.Build(map).Count==0,"disabled incoming lane does not authorize median paint");
  var actual=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var actualZones=UturnRoadMarkings.Build(actual);Check(actualZones.Count==6&&actualZones.Select(z=>z.incomingId).Distinct().Count()==6,"all six explicit U-turn approaches retain one unique median zone");
  foreach(var zone in actualZones)Check(zone.width>=10&&zone.width<=40&&zone.length>2,"actual U-turn median spans the measured across gap and active join "+zone.joinId);
 }
 static void UturnArrowFallbackRegressions(){
  var predecessor=new RoadMapLane{id="split:previous",sourceId="split",points=new[]{new MapPoint{x=0,z=-23},new MapPoint{x=0,z=-3}},next=new[]{"split:head"}};
  var incoming=new RoadMapLane{id="split:head",sourceId="split",points=new[]{new MapPoint{x=0,z=-3},new MapPoint{x=0,z=0}},next=new[]{"join:split:uturn","split:ordinary"}};
  var join=new RoadMapLane{id="join:split:uturn",turn=-2,points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=-6,z=6},new MapPoint{x=-12,z=0}}};
  var ordinary=new RoadMapLane{id="split:ordinary",sourceId="split",points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=20}}};
  var lanes=new[]{predecessor,incoming,join,ordinary};var edges=lanes.Select(l=>new HdEdge{id=l.id,sourceId=l.sourceId??"",status="provisional",turn=l.turn,points=l.points,next=l.next.Select(id=>Array.FindIndex(lanes,x=>x.id==id)).ToArray()}).ToArray();
  var map=new MapData{hybrid=true,hdEdges=edges,editorDocument=new RoadMapDocument{lanes=lanes}};
  Check(LaneDirectionPaint.Layout(map).Count==0,"short same-source split does not receive regular lane glyph");
  var fallback=UturnRoadMarkings.FallbackArrows(map);Check(fallback.Count==1&&fallback[0].turns==LaneDirectionPaint.Uturn&&fallback[0].edgeIndex==1&&fallback[0].centre.z<=-6&&fallback[0].centre.z>=-15,"short U-turn head receives dedicated glyph on contiguous ordinary predecessor");
  Check(incoming.next.Length==2&&edges[1].next.Length==2,"fallback paint preserves legal continuation and U-turn connections");
  predecessor.disabled=true;Check(UturnRoadMarkings.FallbackArrows(map).Count==0,"short head cannot borrow disabled predecessor for glyph footprint");
  var actual=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var arrows=LaneDirectionPaint.Layout(actual);arrows.AddRange(UturnRoadMarkings.FallbackArrows(actual));
  foreach(var zone in UturnRoadMarkings.Build(actual))Check(arrows.Any(a=>actual.hdEdges[a.edgeIndex].id==zone.incomingId&&(a.turns&LaneDirectionPaint.Uturn)!=0),"actual U-turn has a regular or dedicated fallback arrow "+zone.joinId);
 }
}

using System;
using System.Linq;
using UnityEngine;

public static class ReviewedNativeLanePresentationTests {
 static void Check(bool value,string reason){if(!value)throw new Exception("REVIEWED_NATIVE_PAINT: "+reason);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static RoadMapFacility Metadata(string id,string lane){return new RoadMapFacility{id=id,kind="lanePresentation",type="원본 차로",junctionId="paint fixture",laneId=lane,axis=0,sign=1,width=3.5f,startAt=0,endAt=30,evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,url="fixture approach evidence",month="2026-05"}};}
 static HdEdge Edge(string id,string status,float x,int turn=0,int[] next=null){return new HdEdge{id=id,status=status,sourceId="corridor",ruleId="paint fixture",number=1,turn=turn,points=new[]{P(x,0),P(x,30)},next=next??new int[0]};}
 static bool Suppresses(ReviewedNativeLanePresentation projection,string kind,Vector3 point,Vector3 direction){return (bool)typeof(ReviewedNativeLanePresentation).GetMethod("SuppressesSynthetic").Invoke(projection,new object[]{kind,point,direction});}
 public static void Run(){
  Check(typeof(LaneDirectionPaint).Assembly.GetType("ReviewedNativeLanePresentation")!=null,"explicit reviewed native lane presentation is available");
  Check(typeof(ReviewedNativeLanePresentation).GetMethod("SuppressesSynthetic")!=null,"reviewed native lanes exclude synthetic centre lines from lane interiors");
  var left=Edge("native-left","ngii",0,0,new[]{3});left.number=91;
  var middle=Edge("native-middle","ngii",3.5f,0,new[]{4});var outer=Edge("native-outer","ngii",7,0,new[]{5});outer.number=2;
  var connectors=new[]{Edge("native-left-connector","ngii",0,-1,new[]{6}),Edge("native-middle-connector","ngii",3.5f,0,new[]{6}),Edge("native-outer-connector","ngii",7,0,new[]{6})};
  var exit=Edge("native-exit","ngii",0);
  // Connected source geometry crosses the fixture junction before its exit.
  // Source turn labels remain authoritative for its dedicated left connector.
  foreach(var connector in connectors)connector.points=new[]{P(connector.points[0].x,30),P(0,70)};
  exit.points=new[]{P(0,70),P(0,90)};
  var synthetic=Edge("local-provisional","provisional",3.5f,0,new[]{8});var syntheticJoin=Edge("join:local-provisional","provisional",3.5f,0,new[]{6});
  var reverse=Edge("local-opposite","provisional",-3.5f,0,new[]{10});reverse.points=new[]{P(-3.5f,30),P(-3.5f,0)};var reverseJoin=Edge("join:local-opposite","provisional",-3.5f,0,new[]{6});
  var unknown=Edge("unreviewed-native","ngii",14,0,new[]{4});
  var map=new MapData{mapId="fixture",hybrid=true,roads=new[]{new MapRoad{id="corridor",width=14,lanes=4,points=new[]{P(0,0),P(0,30)}}},hdEdges=new[]{left,middle,outer}.Concat(connectors).Concat(new[]{exit,synthetic,syntheticJoin,reverse,reverseJoin,unknown}).ToArray(),junctionRules=new[]{new HybridJunctionRule{id="paint fixture",dx=0,dz=1,z=50,signalMode=2}},editorDocument=new RoadMapDocument{facilities=new[]{Metadata("left metadata",left.id),Metadata("middle metadata",middle.id),Metadata("outer metadata",outer.id)}}};
  map.editorDocument.lanes=map.hdEdges.Select(e=>RoadMapLane.From(e,map.hdEdges)).ToArray();
  Check(ReviewedNativeLanePresentation.ValidateAssignments(map,map.editorDocument).Count==0,"reviewed existing native assignments validate");
  var selected=map.editorDocument.facilities[0];selected.rule=true;Check(ReviewedNativeLanePresentation.ValidateAssignments(map,map.editorDocument).Any(i=>i.error),"presentation cannot grant a movement rule");selected.rule=false;
  selected.sign=-1;Check(ReviewedNativeLanePresentation.ValidateAssignments(map,map.editorDocument).Any(i=>i.error),"opposite approach assignment rejected");selected.sign=1;
  selected.endAt=31;Check(ReviewedNativeLanePresentation.ValidateAssignments(map,map.editorDocument).Any(i=>i.error),"reviewed interval cannot overrun the native lane");selected.endAt=30;
  string before=JsonUtility.ToJson(map);var arrows=LaneDirectionPaint.Layout(map);
  Check(arrows.Any(a=>a.edgeIndex==0&&a.turns==LaneDirectionPaint.Left),"enabled native number91 retains its actual dedicated left glyph");
  Check(arrows.Count(a=>a.edgeIndex==1||a.edgeIndex==2)==2&&arrows.Where(a=>a.edgeIndex==1||a.edgeIndex==2).All(a=>a.turns==LaneDirectionPaint.Straight),"two reviewed native through lanes retain separate straight roles");
  Check(!arrows.Any(a=>a.edgeIndex==7),"overlapping same-approach provisional role is suppressed locally");
  Check(arrows.Any(a=>a.edgeIndex==9),"opposing provisional approach survives");
  Check(!arrows.Any(a=>a.edgeIndex==11),"unreviewed NGII lanes are not globally enabled for paint");
  var projection=new ReviewedNativeLanePresentation(map);Check(projection.SuppressesDivider(new Vector3(5.25f,0,15),Vector3.forward),"overlapping synthetic divider inside native carriageway is replaced");Check(!projection.SuppressesDivider(new Vector3(-3.5f,0,15),Vector3.back),"opposing carriageway divider remains");Check(!projection.SuppressesDivider(new Vector3(5.25f,0,35),Vector3.forward),"outside reviewed interval remains unchanged");
  Check(Suppresses(projection,"Centre line",new Vector3(0,0,15),Vector3.forward),"synthetic centre line through the reviewed left lane is suppressed");
  Check(!Suppresses(projection,"Centre line",new Vector3(-3.5f,0,15),Vector3.back)&&!Suppresses(projection,"Centre line",new Vector3(0,0,35),Vector3.forward),"opposite and outside-interval synthetic paint survive");
  Check(!Suppresses(projection,"Road edge",new Vector3(0,0,15),Vector3.forward),"presentation filter does not rewrite kerb-side edge semantics");
  var paint=new System.Collections.Generic.List<Vector3>();var yellow=new System.Collections.Generic.List<Vector3>();RoadPaintGeometry.Paint(map,new System.Collections.Generic.List<TrafficJunction>(),(n,p,d,w,l,y)=>{if(n=="Reviewed native lane divider")paint.Add(p);if(y)yellow.Add(p);});
  Check(paint.Count>0&&paint.Any(p=>Mathf.Abs(p.x-1.75f)<.05f)&&paint.Any(p=>Mathf.Abs(p.x-5.25f)<.05f),"internal white dividers follow adjacent native lane centres");
  Check(paint.All(p=>p.z>=0&&p.z<=30),"native dividers stay inside the explicit reviewed interval");
  Check(JsonUtility.ToJson(map)==before,"presentation preserves graph, permissions, stops and metadata");
  Check(!yellow.Any(p=>Mathf.Abs(p.x)<1&&p.z>1&&p.z<29),"actual compiled synthetic centre line no longer crosses the native left lane");
  map.hdMarks=new[]{new HdMark{id="original native divider",type="212",kind="503",points=new[]{P(1.75f,0),P(1.75f,30)}},new HdMark{id="original median",type="111",kind="501",points=new[]{P(-1.75f,0),P(-1.75f,30)}}};
  paint.Clear();RoadPaintGeometry.Paint(map,new System.Collections.Generic.List<TrafficJunction>(),(n,p,d,w,l,y)=>{if(n=="Reviewed native lane divider")paint.Add(p);});
  Check(!paint.Any(p=>Mathf.Abs(p.x-1.75f)<.05f)&&paint.Any(p=>Mathf.Abs(p.x-5.25f)<.05f),"matching original503 divider is kept once while uncovered boundary still gets fallback paint");
  var original=new GameObject("original reviewed paint preservation");try{HdRouteTrial.DrawMarkings(map,original.transform);var mesh=original.transform.Find("원본 노면선 1").GetComponent<MeshFilter>().sharedMesh;Check(mesh.vertices.Any(v=>Mathf.Abs(v.x+1.75f)<.2f&&v.z>12&&v.z<18),"original median source paint survives the generated-paint filter");}finally{UnityEngine.Object.DestroyImmediate(original);}map.hdMarks=null;
  connectors[0].next=new int[0];Check(!LaneDirectionPaint.Layout(map).Any(a=>a.edgeIndex==0),"detached native connector cannot paint a left permission");connectors[0].next=new[]{6};
  // Both alternatives must start at this incoming lane's endpoint. The
  // middle lane's normal connector starts 3.5m away and is not its branch.
  var middleConnectorPoints=connectors[1].points;connectors[1].points=new[]{P(0,30),P(0,70)};
  connectors[0].turn=UturnMovement.Turn;left.next=new[]{3,4};selected.endAt=10;
  Check(ReviewedNativeLanePresentation.MovementMask(map,left,map.junctionRules[0])==(LaneDirectionPaint.Uturn|LaneDirectionPaint.Straight),"short-interval fixture has two connected U-turn and through alternatives");
  Check(!LaneDirectionPaint.Layout(map).Any(a=>a.edgeIndex==0),"short reviewed interval cannot fit the rear glyph of a combined U-turn indication");
  connectors[0].turn=-1;left.next=new[]{3};selected.endAt=30;connectors[1].points=middleConnectorPoints;
  exit.disabled=true;Check(!LaneDirectionPaint.Layout(map).Any(a=>a.edgeIndex<3),"disabled native exits remove reviewed movement arrows");
  Debug.Log("REVIEWED_NATIVE_LANE_PRESENTATION_OK");
 }
}

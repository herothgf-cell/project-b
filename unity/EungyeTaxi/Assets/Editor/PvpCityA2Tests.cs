using System;
using System.Linq;
using UnityEngine;
public static class PvpCityA2Tests {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception("PVP_CITY_A2: "+label);}
 public static void Run(){
  checks=0;var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);
  foreach(int count in new[]{2,4,8}){
   var road=map.roads.FirstOrDefault(r=>r.id.StartsWith("city-road:")&&r.DisplayLanes==count);
   Check(road!=null,"driveable "+count+" lane street exists");
   var lanes=map.hdEdges.Where(e=>e.sourceId==road.id&&e.turn==0&&e.change==0).ToArray();
   Check(lanes.Select(e=>e.number).Distinct().Count()==count/2,"actual parallel lane positions "+count);
   Check(road.width>=count*3.4f,"asphalt contains all lanes "+count);
  }
  var changes=map.hdEdges.Where(e=>e.change!=0).ToArray();Check(changes.Length>=24,"lane changes provide passing routes");
  foreach(var e in changes){Check(e.points.Length>=12,"smooth lane change");Check(e.next.Length>0&&map.hdEdges[e.next[0]].number==e.number,"lane change arrives at intended lane");var head=Array.FindIndex(map.hdEdges,x=>x.next.Contains(Array.IndexOf(map.hdEdges,e)));var route=graph.Route(head,graph.paths[head].length-1,e.next[0],1);Check(route!=null&&route.path.length<70&&route.edges.Any(x=>map.hdEdges[x].change!=0),"adjacent lane is reachable by a local passing manoeuvre");}
  Check(map.roads.Count(r=>r.id.StartsWith("city-road:")&&r.points.Any(p=>Distance(p.V(),r.points[0].V(),r.points.Last().V())>8))>=12,"curved residential links");
  Check(map.roads.Count(r=>r.id.StartsWith("city-road:alley"))>=12,"interior alley routes");
  Check(map.junctionRules.Any(r=>r.id.Contains("alley")&&r.approaches.Length==3),"interior T junction");
  Check(map.junctionRules.Any(r=>r.signalMode==1)&&map.junctionRules.Any(r=>r.signalMode==2),"permissive and protected signal choices");
  Check(map.editorDocument!=null,"native signed movement document");
  var turns=map.hdEdges.Where(e=>e.turn==UturnMovement.Turn).ToArray();Check(turns.Length>=2,"real native U-turn paths");
  foreach(var turn in turns){var approach=map.hdEdges.First(e=>e.next.Contains(Array.IndexOf(map.hdEdges,turn)));Check(UturnMovement.Condition(map.editorDocument,approach.id)!=null,"U-turn approach has one native condition");Check(UturnMovement.GeometryError(turn.points.Select(p=>p.V()).ToArray(),(approach.points.Last().V()-approach.points[approach.points.Length-2].V()).normalized,(map.hdEdges[turn.next[0]].points[1].V()-map.hdEdges[turn.next[0]].points[0].V()).normalized)==null,"driveable U-turn radius");}
  foreach(var e in map.hdEdges){Check(e.next.Length>0,"no trapped route "+e.id);foreach(int n in e.next)Check(Vector3.Dot((e.points.Last().V()-e.points[e.points.Length-2].V()).normalized,(map.hdEdges[n].points[1].V()-map.hdEdges[n].points[0].V()).normalized)>.97f,"continuous road tangent "+e.id);}
  var owner=new GameObject("A2 native signals test");try{var traffic=owner.AddComponent<TrafficSystem>();traffic.Build(map,false);foreach(var r in map.junctionRules.Where(r=>r.signalMode>0)){var j=traffic.junctions.Single(x=>x.name==r.id);Check(j.lamps.Count==r.approaches.Length,"visible lamp per signal approach "+r.id);Check(j.lamps.All(l=>l.Length==3&&l.All(x=>x!=null)),"native visible signal renderers");}
   foreach(var turn in turns){var approach=map.hdEdges.First(e=>e.next.Contains(Array.IndexOf(map.hdEdges,turn)));var condition=UturnMovement.Condition(map.editorDocument,approach.id);var j=traffic.junctions.Single(x=>x.name==condition.junctionId);var direction=(approach.points.Last().V()-approach.points[approach.points.Length-2].V()).normalized;Check(traffic.SignedUturnApproach(j,condition.axis,condition.sign,approach.points.Last().V()-direction*2,direction),"native law recognizes signed U-turn approach before curve");bool permitted=false,denied=false;traffic.manualClock=true;for(int t=0;t<100;t++){traffic.manualTime=t;bool allowed=traffic.UturnPermitted(j,condition.axis,condition.sign,approach.id);permitted|=allowed;denied|=!allowed;}Check(permitted&&denied,"native U-turn permission follows actual signal phase");}
  }finally{UnityEngine.Object.DestroyImmediate(owner);}
  Debug.Log("PVP_CITY_A2_TESTS_OK checks="+checks+" lanes="+map.hdEdges.Length+" changes="+changes.Length+" uturns="+turns.Length);
 }
 public static void LaneChoices(){
  checks=0;var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);
  foreach(var road in map.roads.Where(r=>r.id.StartsWith("city-road:")&&r.DisplayLanes==8))foreach(int sign in new[]{-1,1}){
   int outer=Array.FindIndex(map.hdEdges,e=>e.sourceId==road.id&&e.number==4&&e.change==0&&e.turn==0&&e.id.EndsWith(":head")&&(e.points.Last().x-e.points[0].x)*sign>0);
   int inner=Array.FindIndex(map.hdEdges,e=>e.sourceId==road.id&&e.number==1&&e.change==0&&e.turn==0&&e.id.EndsWith(":lane:1")&&(e.points.Last().x-e.points[0].x)*sign>0);
   Check(outer>=0&&inner>=0,"8-lane incoming/outgoing fixtures exist");var route=graph.Route(outer,5,inner,graph.paths[inner].length-5);
   Check(route!=null&&route.edges.All(e=>map.hdEdges[e].sourceId==road.id&&map.hdEdges[e].turn==0)&&route.edges.Count(e=>map.hdEdges[e].change<0)==3,"outer fourth lane can reach first lane for left/U-turn before next intersection: "+road.id+":"+sign);
  }
  Debug.Log("PVP_CITY_A2_LANE_CHOICES_OK checks="+checks);
 }
 public static void Spawn(){
  checks=0;var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);var direction=(map.targets[1].V()-map.targets[0].V()).normalized;
  // DriveSession.Start's normal launch placement, not a stop-position fixture.
  var position=map.targets[0].V()-direction*23+TrafficSystem.Right(direction)*2.5f;float at;int edge=graph.Nearest(position,direction,out at);float error=float.PositiveInfinity;if(edge>=0)graph.paths[edge].Project(position,out error);
  Debug.Log("PVP_CITY_A2_SPAWN position="+position+" edge="+edge+" error="+error+" at="+at);Check(edge>=0&&error<2.5f,"actual initial taxi can join native lane assistance");
  var surface=new RoadSurfaceIndex(map);foreach(float side in new[]{-1f,0,1f})foreach(float along in new[]{-2f,0,2f}){var p=position+direction*along+TrafficSystem.Right(direction)*side;Check(surface.OnRoad(p)&&!ProjectMapScene.OnArea(map,p,true),"actual initial taxi footprint clears pavement edge and raised sidewalk");}
  Debug.Log("PVP_CITY_A2_SPAWN_OK checks="+checks);
 }
 static float Distance(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
}

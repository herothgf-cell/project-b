using System;
using System.Linq;
using UnityEngine;

public static class PedestrianSignalPlacementTests {
 static void Check(bool condition,string message){if(!condition)throw new Exception("PEDESTRIAN_SIGNAL_PLACEMENT: "+message);}
 static MapArea Walk(float left,float right,float near,float far,float height){return new MapArea{kind="sidewalk",height=height,points=new[]{new MapPoint{x=left,z=near},new MapPoint{x=right,z=near},new MapPoint{x=right,z=far},new MapPoint{x=left,z=far}},triangles=new[]{0,1,2,0,2,3}};}
 static TrafficJunction Junction(){return new TrafficJunction{name="test",center=Vector3.zero,direction=Vector3.forward,halfWidth=new[]{5f,5f}};}
 static float Gap(Vector3 p,MapPoint a,MapPoint b){var v=b.V()-a.V();float t=Mathf.Clamp01(Vector3.Dot(p-a.V(),v)/v.sqrMagnitude);var q=a.V()+v*t;return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(q.x,q.z));}
 public static void Run(){
  var crossing=new HdEdge{id="active",points=new[]{new MapPoint{x=6.5f,z=-20},new MapPoint{x=6.5f,z=20}}};
  var map=new MapData{areas=new[]{Walk(-10,-6,-9,-5,.42f),Walk(6,10,-9,-5,.42f)},hdEdges=new[]{crossing}};
  var j=Junction();Vector3 left,right,face;
  Check(PedestrianSignalPlacement.TryPlace(map,j,Vector3.forward,0,1,-1,5,5,out left,out face),"left walk endpoint found");
  Check(PedestrianSignalPlacement.TryPlace(map,j,Vector3.forward,0,1,1,5,5,out right,out face),"right walk endpoint found");
  Check(left.x<0&&right.x>8.4f&&Mathf.Abs(left.y-.42f)<.01f&&Mathf.Abs(right.y-.42f)<.01f,"posts use walk tops outside active lane footprint");
  Check(Gap(right,crossing.points[0],crossing.points[1])>2.1f,"post footing clears active lane");
  var island=Walk(9,12,-9,-5,.58f);map.areas=new[]{island};
  Check(PedestrianSignalPlacement.TryPlace(map,j,Vector3.forward,0,1,1,5,5,out right,out face)&&Mathf.Abs(right.y-.58f)<.01f,"raised sidewalk island provides a grounded post site");
  island.kind="island";
  Check(!PedestrianSignalPlacement.TryPlace(map,j,Vector3.forward,0,1,1,5,5,out right,out face),"unsupported island area cannot provide a floating post site");
  map.areas=new[]{Walk(-10,-6,-9,-5,.42f),Walk(6,10,-9,-5,.42f)};
  var absent=new MapData{areas=new MapArea[0],hdEdges=new[]{crossing}};Vector3 unused;
  Check(!PedestrianSignalPlacement.TryPlace(absent,j,Vector3.forward,0,1,1,5,5,out unused,out face),"missing walk returns no post");
  var parent=new GameObject("missing pedestrian signal test");try{var traffic=parent.AddComponent<TrafficSystem>();PedestrianSignalDisplay.Create(traffic,absent,j,Vector3.forward,5,5,parent.transform);Check(parent.transform.childCount==0,"missing walk hides both posts");}finally{UnityEngine.Object.DestroyImmediate(parent);}
  map.areas=map.areas.Concat(new[]{Walk(-10,-6,-14,-11,.3f),Walk(6,10,-14,-11,.3f)}).ToArray();
  map.editorDocument=new RoadMapDocument{facilities=new[]{new RoadMapFacility{id="authored-walk",kind="crosswalk",junctionId="test",axis=0,sign=1,x=0,z=-12,width=10,yaw=0}}};
  Check(PedestrianSignalPlacement.TryPlace(map,j,Vector3.forward,0,1,-1,5,5,out left,out face)&&left.z<-10,"explicit axis/sign crosswalk pose controls post search");
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var actual=package.Compose();var rule=actual.junctionRules.First(x=>x.id=="J5057_4449");
  var real=new TrafficJunction{name=rule.id,center=rule.V(),direction=new Vector3(rule.dx,0,rule.dz).normalized,halfWidth=new[]{0f,0f}};
  foreach(var approach in rule.approaches){var d=new Vector3(approach.dx,0,approach.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(d,real.direction))>.7f?0:1;real.halfWidth[axis]=Mathf.Max(real.halfWidth[axis],approach.width*.5f);}
  int placed=0;for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1})foreach(int side in new[]{-1,1}){
   var incoming=(axis==0?real.direction:TrafficSystem.Right(real.direction))*sign;Vector3 point;
   if(!PedestrianSignalPlacement.TryPlace(actual,real,incoming,axis,sign,side,real.halfWidth[axis],real.halfWidth[1-axis],out point,out face))continue;
   placed++;Check(ProjectMapScene.OnArea(actual,point,true),"J5057 post rests on composed sidewalk: "+axis+"/"+sign+"/"+side);
   foreach(var lane in actual.hdEdges){if(lane.disabled||lane.points==null)continue;for(int k=1;k<lane.points.Length;k++)Check(Gap(point,lane.points[k-1],lane.points[k])>2.1f,"J5057 post clears active lane: "+axis+"/"+sign+"/"+side+" "+lane.id);}
  }
  Check(placed>=4,"J5057 has grounded posts on multiple actual crosswalk endpoints, placed="+placed);
  var watch=System.Diagnostics.Stopwatch.StartNew();int allPlaced=0,allAttempted=0;
  foreach(var junctionRule in actual.junctionRules){
   var sceneJunction=new TrafficJunction{name=junctionRule.id,center=junctionRule.V(),direction=new Vector3(junctionRule.dx,0,junctionRule.dz).normalized,halfWidth=new[]{0f,0f}};
   foreach(var approach in junctionRule.approaches){var d=new Vector3(approach.dx,0,approach.dz).normalized;int a=Mathf.Abs(Vector3.Dot(d,sceneJunction.direction))>.7f?0:1;sceneJunction.halfWidth[a]=Mathf.Max(sceneJunction.halfWidth[a],approach.width*.5f);}
   foreach(var approach in junctionRule.approaches){var d=new Vector3(approach.dx,0,approach.dz).normalized;int a=Mathf.Abs(Vector3.Dot(d,sceneJunction.direction))>.7f?0:1;int s=Vector3.Dot(d,a==0?sceneJunction.direction:TrafficSystem.Right(sceneJunction.direction))>=0?1:-1;
    foreach(int side in new[]{-1,1}){Vector3 point;allAttempted++;if(PedestrianSignalPlacement.TryPlace(actual,sceneJunction,d,a,s,side,approach.width*.5f,sceneJunction.halfWidth[1-a],out point,out face))allPlaced++;
     else Debug.LogWarning("PEDESTRIAN_SIGNAL_SITE_MISSING_TEST "+junctionRule.id+" axis="+a+" sign="+s+" side="+side);
    }
   }
  }
  watch.Stop();Debug.Log("PEDESTRIAN_SIGNAL_PLACEMENT_TEST_OK J5057="+placed+"/8 all="+allPlaced+"/"+allAttempted+" elapsed_ms="+watch.ElapsedMilliseconds);
 }
}

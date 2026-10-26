using System.Collections.Generic;
using UnityEngine;
// Provisional game operation; scenario observations remain authoritative when active.
public sealed class PedestrianSignalDisplay:MonoBehaviour {
 TrafficSystem system;TrafficJunction junction;int axis,sign;Renderer red,green;Material onRed,onGreen,off;
 public static void Create(TrafficSystem traffic,MapData map,TrafficJunction j,Vector3 incoming,float halfWidth,float conflict,Transform parent){
  int axis=Mathf.Abs(Vector3.Dot(incoming,traffic.AxisDirection(j,0)))>.7f?0:1,sign=traffic.Sign(incoming,axis,j);
  for(int side=-1;side<=1;side+=2){Vector3 position,facing;
   if(!PedestrianSignalPlacement.TryPlace(map,j,incoming,axis,sign,side,halfWidth,conflict,out position,out facing)){
    Debug.LogWarning("PEDESTRIAN_SIGNAL_SITE_MISSING "+j.name+" axis="+axis+" sign="+sign+" side="+side+" · walkable site within 20 m unavailable");continue;
   }
   var root=new GameObject("보행자 신호 · 게임 설정 · "+j.name);root.transform.SetParent(parent,false);root.transform.SetPositionAndRotation(position,Quaternion.LookRotation(facing));
   var display=root.AddComponent<PedestrianSignalDisplay>();display.system=traffic;display.junction=j;display.axis=axis;display.sign=sign;display.Build();}
 }
 void Build(){var dark=WorldBuilder.Mat("Pedestrian housing",new Color(.025f,.03f,.04f));onRed=WorldBuilder.Mat("Pedestrian stop",Color.red);onGreen=WorldBuilder.Mat("Pedestrian go",Color.green);off=WorldBuilder.Mat("Pedestrian unlit",new Color(.07f,.09f,.09f));foreach(var m in new[]{onRed,onGreen}){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",m.color*2);}
  WorldBuilder.Box("보행 신호 기둥",new Vector3(0,1.4f,0),new Vector3(.12f,2.8f,.12f),dark,transform,false);WorldBuilder.Box("보행 신호함",new Vector3(0,2.6f,0),new Vector3(.7f,1.18f,.22f),dark,transform,false);
  red=WorldBuilder.Box("정지 사람",new Vector3(0,2.87f,-.14f),new Vector3(.43f,.43f,.04f),off,transform,false).GetComponent<Renderer>();green=WorldBuilder.Box("보행 사람",new Vector3(0,2.33f,-.14f),new Vector3(.43f,.43f,.04f),off,transform,false).GetComponent<Renderer>();
  var label=new GameObject("보행 안내");label.transform.SetParent(transform,false);label.transform.localPosition=new Vector3(0,1.95f,-.15f);var text=label.AddComponent<TextMesh>();text.text="보행";KoreanText.Fit(text,.65f,.23f);Update();
 }
 void Update(){if(!red)return;bool walk=system.PedestrianGreen(junction,axis,sign);red.sharedMaterial=walk?off:onRed;green.sharedMaterial=walk?onGreen:off;}
}

public static class PedestrianSignalPlacement {
 struct LaneSpan {public Vector3 a,b;public float clearance;}
 const float SearchRadius=20f,Step=.5f,Footing=.2f,LaneMargin=.4f;
 static bool Near(float min,float max,float center,float radius){return max>=center-radius&&min<=center+radius;}
 static float WalkHeight(MapArea area,Vector3 point){
  float top=Mathf.Max(.02f,area.height);var triangles=area.triangles;
  if(triangles!=null)for(int i=0;i+2<triangles.Length;i+=3){
    MapPoint a=area.points[triangles[i]],b=area.points[triangles[i+1]],c=area.points[triangles[i+2]];
   float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(denominator)<.00001f)continue;
   float wa=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/denominator;
   float wb=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/denominator,wc=1-wa-wb;
   if(wa>=-.001f&&wb>=-.001f&&wc>=-.001f)return top+wa*a.elevation+wb*b.elevation+wc*c.elevation;
  }
  float best=float.MaxValue,height=top;foreach(var vertex in area.points){float dx=point.x-vertex.x,dz=point.z-vertex.z,gap=dx*dx+dz*dz;if(gap<best){best=gap;height=top+vertex.elevation;}}return height;
 }
 static bool Fits(MapArea area,Vector3 point){
  if(!ProjectMapScene.Contains(area.points,point))return false;
  foreach(float x in new[]{-Footing,Footing})foreach(float z in new[]{-Footing,Footing})
   if(!ProjectMapScene.Contains(area.points,point+new Vector3(x,0,z)))return false;
  return true;
 }
 static bool ClearsLanes(Vector3 point,List<LaneSpan> lanes){
  foreach(var lane in lanes){var delta=lane.b-lane.a;float length=delta.x*delta.x+delta.z*delta.z;
   if(length<.0001f)continue;float at=Mathf.Clamp01(((point.x-lane.a.x)*delta.x+(point.z-lane.a.z)*delta.z)/length);
   var closest=Vector3.Lerp(lane.a,lane.b,at);if(Mathf.Abs(point.y-closest.y)>1.5f)continue;
   float dx=point.x-closest.x,dz=point.z-closest.z;if(dx*dx+dz*dz<lane.clearance*lane.clearance)return false;
  }return true;
 }
 public static bool TryPlace(MapData map,TrafficJunction junction,Vector3 incoming,int axis,int sign,int side,float halfWidth,float conflict,out Vector3 position,out Vector3 facing){
  position=Vector3.zero;incoming.y=0;if(incoming.sqrMagnitude<.1f){facing=Vector3.forward;return false;}incoming.Normalize();
  var across=TrafficSystem.Right(incoming);var center=junction.center-incoming*(conflict+TrafficJunction.CrosswalkCentreOffset);
  if(map.editorDocument!=null&&map.editorDocument.facilities!=null){RoadMapFacility selected=null;float best=float.MaxValue;
   foreach(var facility in map.editorDocument.facilities){if(facility.kind!="crosswalk"||facility.junctionId!=junction.name||facility.axis!=axis||facility.sign!=sign)continue;
    float distance=(facility.Position-center).sqrMagnitude;if(distance<best){best=distance;selected=facility;}}
   if(selected!=null){center=selected.Position;var direction=Quaternion.Euler(0,selected.yaw,0)*Vector3.forward;var explicitAcross=TrafficSystem.Right(direction);
    if(Vector3.Dot(explicitAcross,across)<0)explicitAcross=-explicitAcross;across=explicitAcross;if(selected.width>0)halfWidth=selected.width*.5f;}
  }
  facing=across*side;var desired=center+facing*(halfWidth+1.1f);
  var walks=new List<MapArea>();foreach(var area in map.areas??new MapArea[0]){
   if(area.kind!="sidewalk"||area.points==null||area.points.Length<3)continue;
   float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
   foreach(var vertex in area.points){minX=Mathf.Min(minX,vertex.x);maxX=Mathf.Max(maxX,vertex.x);minZ=Mathf.Min(minZ,vertex.z);maxZ=Mathf.Max(maxZ,vertex.z);}
   if(Near(minX,maxX,desired.x,SearchRadius+Footing)&&Near(minZ,maxZ,desired.z,SearchRadius+Footing))walks.Add(area);
  }
  if(walks.Count==0)return false;
  var widths=new Dictionary<string,float>();if(map.editorDocument!=null&&map.editorDocument.lanes!=null)
   foreach(var lane in map.editorDocument.lanes)if(lane!=null&&!string.IsNullOrEmpty(lane.id))widths[lane.id]=Mathf.Max(.5f,lane.width);
  var lanes=new List<LaneSpan>();foreach(var lane in map.hdEdges??new HdEdge[0]){
   if(lane.disabled||lane.points==null||lane.points.Length<2)continue;
   float width;if(!widths.TryGetValue(lane.id??"",out width))width=3.5f;float clearance=width*.5f+LaneMargin;
    for(int i=1;i<lane.points.Length;i++){Vector3 a=lane.points[i-1].V(),b=lane.points[i].V();
    if(!Near(Mathf.Min(a.x,b.x)-clearance,Mathf.Max(a.x,b.x)+clearance,desired.x,SearchRadius)||
       !Near(Mathf.Min(a.z,b.z)-clearance,Mathf.Max(a.z,b.z)+clearance,desired.z,SearchRadius))continue;
    lanes.Add(new LaneSpan{a=a,b=b,clearance=clearance});
   }
  }
  for(int ring=0;ring<=SearchRadius/Step;ring++){float radius=ring*Step;int samples=ring==0?1:Mathf.CeilToInt(2*Mathf.PI*ring);
   for(int i=0;i<samples;i++){float angle=2*Mathf.PI*i/samples;var candidate=desired+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
    if(Vector3.Dot(candidate-center,facing)<.75f||Mathf.Abs(Vector3.Dot(candidate-center,incoming))>10)continue;
    foreach(var area in walks){if(!Fits(area,candidate))continue;candidate.y=WalkHeight(area,candidate);
     if(!ClearsLanes(candidate,lanes)){candidate.y=desired.y;continue;}position=candidate;return true;
    }
   }
  }
  return false;
 }
}

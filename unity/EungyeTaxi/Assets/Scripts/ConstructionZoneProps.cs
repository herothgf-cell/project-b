using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 2026-11-28: 3D props for the road-construction day card (DailyRoadConstruction, survey #48 Y08,
// westbound Gyeysu-ro outer lane). Until now the closure was only a red line on the map.
// Own low-poly cartoon meshes (sizes taken from the Fab/Megascans trial downloads, which stay out
// of Git): traffic cones on a taper and along the open-lane boundary, a chevron arrow board ahead
// of the taper, and red/white temporary barriers around the work area. Visual only: no colliders,
// the lane is already closed to routing and AI traffic.
public static class ConstructionZoneProps {
 const string RootName="도로 공사 구간 소품";
 const float HalfLane=1.6f,ConeInset=.2f,TaperLength=15f,TaperStep=2.5f,LineStep=3f,EndTaper=6f;
 static Mesh cone;static Material orange,white,black,yellow,red,barrierWhite,asphalt;

 public static void Sync(DriveSession session,bool closed){
  if(!session||!session.world)return;
  var parent=session.world.transform;var old=parent.Find(RootName);
  if(!closed){if(old)UnityEngine.Object.Destroy(old.gameObject);return;}
  if(old)return;
  Build(DailyRoadConstruction.Preview(session.world.map),parent);
 }

 public static int Count(Transform parent){var root=parent?parent.Find(RootName):null;return root?root.childCount:0;}

 public static Transform Build(Vector3[] lane,Transform parent){
  if(lane==null||lane.Length<2)return null;
  Materials();
  var root=new GameObject(RootName).transform;root.SetParent(parent,false);
  var path=new Polyline(lane);float length=path.length;
  // taper: from the outer (right) edge of the closed lane to the open-lane boundary
  float taper=Mathf.Min(TaperLength,length*.3f),endTaper=Mathf.Min(EndTaper,length*.15f);
  for(float d=0;d<=taper+.01f;d+=TaperStep)Cone(path,d,Mathf.Lerp(-(HalfLane-ConeInset),HalfLane-ConeInset,d/Mathf.Max(taper,.01f)),root);
  for(float d=taper+LineStep;d<length-endTaper;d+=LineStep)Cone(path,d,HalfLane-ConeInset,root);
  for(float d=length-endTaper;d<=length+.01f;d+=TaperStep)Cone(path,Mathf.Min(d,length),Mathf.Lerp(HalfLane-ConeInset,-(HalfLane-ConeInset),(d-(length-endTaper))/Mathf.Max(endTaper,.01f)),root);
  ArrowBoard(path,Mathf.Min(1.5f,length*.05f),root);
  // work area: barrier modules around a dug patch in the middle third
  float from=taper+LineStep*2,to=Mathf.Max(from+6,length-endTaper-LineStep*2);
  if(to<length-endTaper){
   int module=0;for(float d=from;d+2f<=to;d+=2.05f)Barrier(path,d,d+2f,.95f,(module++&1)==0,root);
   var a=path.At(from+2,-.35f);var b=path.At(to-2,-.35f);var dir=(b-a);dir.y=0;
   if(dir.magnitude>1)WorldBuilder.Box("공사 굴착 자리",Vector3.zero,new Vector3(1.3f,.02f,dir.magnitude),asphalt,root,false).transform.SetPositionAndRotation((a+b)*.5f+Vector3.up*.012f,Quaternion.LookRotation(dir.normalized));
  }
  return root;
 }

 static void Materials(){
  if(orange)return;
  orange=WorldBuilder.Mat("공사 라바콘 주황",new Color(1f,.36f,.06f));
  white=WorldBuilder.Mat("공사 라바콘 흰 반사띠",new Color(.96f,.96f,.92f));
  black=WorldBuilder.Mat("공사 라바콘 받침",new Color(.13f,.13f,.14f));
  yellow=WorldBuilder.Mat("공사 화살표 노랑",new Color(1f,.78f,.10f));
  red=WorldBuilder.Mat("공사 임시 방호벽 빨강",new Color(.82f,.17f,.13f));
  barrierWhite=WorldBuilder.Mat("공사 임시 방호벽 흰색",new Color(.90f,.88f,.81f));
  asphalt=WorldBuilder.Mat("공사 굴착 자리",new Color(.09f,.09f,.10f));
 }

 sealed class Polyline {
  readonly Vector3[] p;readonly float[] at;public readonly float length;
  public Polyline(Vector3[] points){p=points;at=new float[p.Length];for(int i=1;i<p.Length;i++){var d=p[i]-p[i-1];d.y=0;at[i]=at[i-1]+d.magnitude;}length=at[p.Length-1];}
  // point at distance d along the lane, offset to the left (+) or right (-) of travel
  public Vector3 At(float d,float left){Vector3 dir;var q=Point(d,out dir);return Ground(q+new Vector3(-dir.z,0,dir.x)*left);}
  public Vector3 Dir(float d){Vector3 dir;Point(d,out dir);return dir;}
  Vector3 Point(float d,out Vector3 dir){
   d=Mathf.Clamp(d,0,length);int i=1;while(i<p.Length-1&&at[i]<d)i++;
   var seg=p[i]-p[i-1];seg.y=0;dir=seg.sqrMagnitude>1e-6f?seg.normalized:Vector3.forward;
   float t=at[i]>at[i-1]?(d-at[i-1])/(at[i]-at[i-1]):0;return Vector3.Lerp(p[i-1],p[i],t);
  }
 }

 static Vector3 Ground(Vector3 p){RaycastHit hit;p.y=Physics.Raycast(new Vector3(p.x,p.y+30,p.z),Vector3.down,out hit,80,~(1<<8),QueryTriggerInteraction.Ignore)?hit.point.y:p.y;return p;}

 static void Cone(Polyline path,float d,float left,Transform root){
  var g=new GameObject("라바콘");g.transform.SetParent(root,false);g.transform.position=path.At(d,left);g.transform.rotation=Quaternion.LookRotation(path.Dir(d));
  g.AddComponent<MeshFilter>().sharedMesh=ConeMesh();g.AddComponent<MeshRenderer>().sharedMaterials=new[]{orange,white,black};
 }

 // 0.36 x 0.36 x 0.69 m like the Megascans cone: square black base, orange body, two white bands.
 static Mesh ConeMesh(){
  if(cone)return cone;
  var v=new List<Vector3>();var subs=new[]{new List<int>(),new List<int>(),new List<int>()};
  const float baseH=.035f,top=.69f,r0=.145f,r1=.028f;
  float R(float y){return Mathf.Lerp(r0,r1,(y-baseH)/(top-baseH));}
  void Ring(float y0,float y1,int sub,float grow){Frustum(v,subs[sub],y0,y1,R(y0)+grow,R(y1)+grow,12);}
  Ring(baseH,.30f,0,0);Ring(.30f,.38f,1,.004f);Ring(.38f,.47f,0,0);Ring(.47f,.53f,1,.004f);Ring(.53f,top,0,0);
  Cap(v,subs[0],top,r1,12);
  BoxInto(v,subs[2],new Vector3(0,baseH*.5f,0),new Vector3(.36f,baseH,.36f));
  cone=new Mesh{name="Construction cone"};cone.SetVertices(v);cone.subMeshCount=3;for(int i=0;i<3;i++)cone.SetTriangles(subs[i],i);
  cone.RecalculateNormals();cone.RecalculateBounds();return cone;
 }

 static void Frustum(List<Vector3> v,List<int> t,float y0,float y1,float r0,float r1,int sides){
  for(int i=0;i<sides;i++){
   float a0=i*Mathf.PI*2/sides,a1=(i+1)*Mathf.PI*2/sides;int s=v.Count;
   v.Add(new Vector3(Mathf.Cos(a0)*r0,y0,Mathf.Sin(a0)*r0));v.Add(new Vector3(Mathf.Cos(a1)*r0,y0,Mathf.Sin(a1)*r0));
   v.Add(new Vector3(Mathf.Cos(a1)*r1,y1,Mathf.Sin(a1)*r1));v.Add(new Vector3(Mathf.Cos(a0)*r1,y1,Mathf.Sin(a0)*r1));
   t.AddRange(new[]{s,s+2,s+1,s,s+3,s+2});
  }
 }
 static void Cap(List<Vector3> v,List<int> t,float y,float r,int sides){int c=v.Count;v.Add(new Vector3(0,y,0));for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));}for(int i=0;i<sides;i++)t.AddRange(new[]{c,c+i+2,c+i+1});}
 static void BoxInto(List<Vector3> v,List<int> t,Vector3 c,Vector3 s){
  var h=s*.5f;var n=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
  foreach(var f in n){var u=Mathf.Abs(f.y)>.5f?Vector3.right:Vector3.up;var w=Vector3.Cross(f,u);int b=v.Count;
   foreach(var k in new[]{-u-w,-u+w,u+w,u-w})v.Add(c+Vector3.Scale(f+k,h));t.AddRange(new[]{b,b+2,b+1,b,b+3,b+2});}
 }

 // Yellow board with three black chevrons pointing to the open lane (left of travel), on two legs.
 static void ArrowBoard(Polyline path,float d,Transform root){
  var g=new GameObject("공사 화살표 표지").transform;g.SetParent(root,false);g.position=path.At(d,0);g.rotation=Quaternion.LookRotation(-path.Dir(d));
  foreach(float x in new[]{-.6f,.6f})WorldBuilder.Box("표지 다리",new Vector3(x,.6f,0),new Vector3(.06f,1.2f,.06f),black,g,false);
  WorldBuilder.Box("표지 판",new Vector3(0,1.45f,0),new Vector3(1.7f,.9f,.05f),yellow,g,false);
  // g looks back at oncoming drivers, so its +x is their left (the open lane). Each chevron is
  // two bars meeting at the point on the +x side: "<" for the driver.
  for(int i=0;i<3;i++){float x=.45f-i*.45f;
   foreach(float s in new[]{1f,-1f})WorldBuilder.Box("표지 검정 꺾쇠",new Vector3(x-.12f,1.45f+s*.11f,.035f),new Vector3(.32f,.09f,.02f),black,g,false).transform.localRotation=Quaternion.Euler(0,0,-s*34f);
  }
 }

 static void Barrier(Polyline path,float d0,float d1,float left,bool isRed,Transform root){
  var a=path.At(d0,left);var b=path.At(d1,left);var dir=b-a;dir.y=0;if(dir.sqrMagnitude<.01f)return;
  var m=isRed?red:barrierWhite;var rot=Quaternion.LookRotation(dir.normalized);var mid=(a+b)*.5f;
  WorldBuilder.Box("공사 임시 방호벽 받침",Vector3.zero,new Vector3(.55f,.22f,dir.magnitude),m,root,false).transform.SetPositionAndRotation(mid+Vector3.up*.11f,rot);
  WorldBuilder.Box("공사 임시 방호벽 몸체",Vector3.zero,new Vector3(.36f,.58f,dir.magnitude),m,root,false).transform.SetPositionAndRotation(mid+Vector3.up*.5f,rot);
 }
}

// Probe: -constructionPropsTest closes the construction lane through the real day-card path,
// captures the props, opens the lane again, checks the props are gone, then quits.
public class ConstructionZonePropsProbe : MonoBehaviour {
 static bool Has(string a){return Array.IndexOf(Environment.GetCommandLineArgs(),a)>=0;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Has("-constructionPropsTest")||Has("-constructionPropsShot"))new GameObject("Construction props probe").AddComponent<ConstructionZonePropsProbe>();}

 IEnumerator Start(){
  DriveSession s=null;float wait=0;var report=new StringBuilder();
  while((s==null||s.world==null||s.world.map==null)&&wait<40){s=FindObjectOfType<DriveSession>();wait+=Time.unscaledDeltaTime;yield return null;}
  // -constructionPropsShot runs the campaign (back up the save first) so the day-card path has a business
  while(Has("-constructionPropsShot")&&s!=null&&!s.business&&wait<60){wait+=Time.unscaledDeltaTime;yield return null;}
  yield return new WaitForSecondsRealtime(2f);Physics.SyncTransforms();
  if(s==null||s.world==null||s.world.map==null){Finish(report.AppendLine("no DriveSession map"),false);yield break;}
  var lane=DailyRoadConstruction.Preview(s.world.map);report.AppendLine("lane points="+lane.Length+" business="+(s.business?(s.business.SimulationSession?"simulation":"yes"):"none")+" trial="+RoadMapRuntime.Trial+" practice="+PracticeSession.Enabled);
  bool applied=DailyRoadConstruction.Apply(s,true);report.AppendLine("apply(closed)="+applied);
  if(!applied){ConstructionZoneProps.Build(lane,s.world.transform);report.AppendLine("direct build (day-card path unsupported here)");}
  int count=ConstructionZoneProps.Count(s.world.transform);report.AppendLine("props="+count);
  Time.timeScale=0;yield return Capture(s,lane,report);
  bool removed=true;
  if(applied){DailyRoadConstruction.Apply(s,false);yield return null;removed=ConstructionZoneProps.Count(s.world.transform)==0;report.AppendLine("apply(open) removed="+removed);}
  Finish(report,count>0&&removed);
 }

 IEnumerator Capture(DriveSession s,Vector3[] lane,StringBuilder report){
  if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null||lane.Length<2){report.AppendLine("capture skipped");yield break;}
  var folder=Path.GetFullPath(Path.Combine("artifacts","construction-props"));Directory.CreateDirectory(folder);
  if(s.drivingCamera)s.drivingCamera.enabled=false;
  var cam=new GameObject("Construction capture").AddComponent<Camera>();cam.fieldOfView=62;cam.nearClipPlane=.08f;cam.farClipPlane=700;cam.enabled=false;
  var start=lane[0];var dir=lane[Mathf.Min(2,lane.Length-1)]-start;dir.y=0;dir.Normalize();var left=new Vector3(-dir.z,0,dir.x);
  var mid=lane[lane.Length/2];
  var views=new[]{
   ("approach",start-dir*22+left*3.2f+Vector3.up*1.25f,start+dir*12+Vector3.up*.8f), // taxi eye in the open lane
   ("alongside",start+dir*14+left*3.2f+Vector3.up*1.25f,start+dir*26+Vector3.up*.5f),
   ("raised",start-dir*12+left*8+Vector3.up*14,mid),
  };
  foreach(var v in views){
   cam.transform.SetPositionAndRotation(v.Item2,Quaternion.LookRotation(v.Item3-v.Item2));
   yield return new WaitForEndOfFrame();
   var rt=new RenderTexture(1600,900,24){antiAliasing=4};var prev=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
   File.WriteAllBytes(Path.Combine(folder,"construction-"+v.Item1+".png"),tex.EncodeToPNG());
   cam.targetTexture=null;RenderTexture.active=prev;Destroy(tex);rt.Release();Destroy(rt);report.AppendLine("captured "+v.Item1);
  }
 }

 static void Finish(StringBuilder report,bool ok){
  var folder=Path.GetFullPath(Path.Combine("artifacts","construction-props"));Directory.CreateDirectory(folder);
  report.AppendLine("success="+ok);File.WriteAllText(Path.Combine(folder,"construction-props.txt"),report.ToString());
  Debug.Log("CONSTRUCTION_PROPS_"+(ok?"OK":"FAILED")+"\n"+report);Application.Quit(ok?0:1);
 }
}

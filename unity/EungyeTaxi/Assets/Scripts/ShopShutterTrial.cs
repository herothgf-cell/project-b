using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Art trial (2026-11-27): navy cartoon shop shutters (Fab/Megascans mesh, baked by
// MegascansShutterTrial.BakeNavy into the git-ignored Assets/ArtTrial/Resources) on the ground
// floor of the shop street J6480~J7204 (x 587~708, z~255). Off unless the player is started with
// -shopShutters (place only) or -shopShutterTrial (place, capture street views, quit), so normal
// play and shipped builds are unchanged. Missing prefab (other PCs) = nothing placed.
// Visual only: no colliders.
public class ShopShutterTrial : MonoBehaviour {
 static readonly Rect Area=new Rect(575,200,145,105); // x 575~720, z 200~305 (building centroids)
 static readonly Vector2 Street=new Vector2(587,708); // shutter x range
 const float Pitch=2.15f,Margin=.5f,MaxRoad=24f;
 public static int Placed;
 static bool Has(string a){return Array.IndexOf(Environment.GetCommandLineArgs(),a)>=0;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Has("-shopShutters")||Has("-shopShutterTrial"))new GameObject("Shop shutter trial").AddComponent<ShopShutterTrial>();}

 IEnumerator Start(){
  DriveSession s=null;float wait=0;
  while((s==null||s.world==null||s.world.map==null)&&wait<30){s=FindObjectOfType<DriveSession>();wait+=Time.unscaledDeltaTime;yield return null;}
  yield return new WaitForSecondsRealtime(1.5f);Physics.SyncTransforms();
  var report=new StringBuilder();
  if(s==null||s.world==null||s.world.map==null){report.AppendLine("no DriveSession map");Finish(report,false);yield break;}
  var prefab=Resources.Load<GameObject>("MegascansShutter/ShutterNavy");
  if(!prefab){report.AppendLine("prefab MegascansShutter/ShutterNavy missing (git-ignored trial content not on this PC)");Finish(report,false);yield break;}
  Place(s.world.map,prefab,s.world.transform,report);
  if(Has("-shopShutterTrial")){Time.timeScale=0;yield return Capture(s,report);Finish(report,Placed>0);}
 }

 static void Place(MapData map,GameObject prefab,Transform parent,StringBuilder report){
  var root=new GameObject("상가 셔터 시험").transform;root.SetParent(parent,false);
  var roads=new List<(Vector3 a,Vector3 b)>();
  var near=new Rect(Area.xMin-30,Area.yMin-30,Area.width+60,Area.height+60);
  foreach(var r in map.roads){if(r.points==null)continue;for(int i=1;i<r.points.Length;i++){var a=r.points[i-1].V();var b=r.points[i].V();if(near.Contains(new Vector2(a.x,a.z))||near.Contains(new Vector2(b.x,b.z)))roads.Add((a,b));}}
  foreach(var b in map.buildings){
   // survey (NGII) buildings only: game facilities such as "restaurant" keep their open fronts
   if(b.points==null||b.points.Length<3||b.id==null||!b.id.Contains(":B")||SceneBuildingVisibility.Hidden(map,b.id))continue;
   Vector2 c=Vector2.zero;foreach(var p in b.points)c+=new Vector2(p.x,p.z);c/=b.points.Length;if(!Area.Contains(c))continue;
   float area=0;for(int i=0;i<b.points.Length;i++){var p=b.points[i];var q=b.points[(i+1)%b.points.Length];area+=p.x*q.z-q.x*p.z;}
   int count=0;
   for(int i=0;i<b.points.Length;i++){
    var a=b.points[i].V();var e=b.points[(i+1)%b.points.Length].V();a.y=e.y=0;var edge=e-a;float len=edge.magnitude;if(len<Pitch+Margin*2)continue;var dir=edge/len;
    // outward normal: right of the edge for a clockwise (negative area in x,z) ring, left otherwise
    var outward=area<0?new Vector3(-dir.z,0,dir.x):new Vector3(dir.z,0,-dir.x);
    var mid=(a+e)*.5f;var road=Nearest(roads,mid);var to=road-mid;to.y=0;
    if(to.magnitude>MaxRoad||Vector3.Dot(outward,to.normalized)<.75f)continue;
    int n=Mathf.FloorToInt((len-Margin*2)/Pitch);float start=-(n-1)*Pitch*.5f;
    for(int k=0;k<n;k++){
     var p=mid+dir*(start+k*Pitch)+outward*.03f;
     // the south complex has 115 m north walls (x 527~766): keep only the J6480~J7204 shop stretch
     if(p.x<Street.x||p.x>Street.y)continue;
     p.y=Ground(p+outward*.4f);
     var g=Instantiate(prefab,p,Quaternion.LookRotation(-outward),root);g.name="셔터 "+b.id+" "+i+"-"+k;count++;
    }
   }
   if(count>0)report.AppendLine("building "+b.id+" h="+b.height.ToString("F0")+" shutters="+count);
   Placed+=count;
  }
  report.AppendLine("placed="+Placed);Debug.Log("SHOP_SHUTTERS placed="+Placed+"\n"+report);
 }

 static Vector3 Nearest(List<(Vector3 a,Vector3 b)> segs,Vector3 p){
  Vector3 best=p+Vector3.forward*1e4f;float bd=float.MaxValue;
  foreach(var s in segs){var ab=s.b-s.a;float t=Mathf.Clamp01(Vector3.Dot(p-s.a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));var q=s.a+ab*t;q.y=0;float d=(q-p).sqrMagnitude;if(d<bd){bd=d;best=q;}}
  return best;
 }

 static float Ground(Vector3 p){RaycastHit hit;return Physics.Raycast(new Vector3(p.x,30,p.z),Vector3.down,out hit,80,~(1<<8),QueryTriggerInteraction.Ignore)?hit.point.y:0;}

 IEnumerator Capture(DriveSession s,StringBuilder report){
  if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null){report.AppendLine("no graphics, capture skipped");yield break;}
  var folder=Path.GetFullPath(Path.Combine("artifacts","shop-shutter"));Directory.CreateDirectory(folder);
  if(s.drivingCamera)s.drivingCamera.enabled=false;
  var cam=new GameObject("Shop shutter capture").AddComponent<Camera>();cam.fieldOfView=62;cam.nearClipPlane=.08f;cam.farClipPlane=700;cam.clearFlags=CameraClearFlags.Skybox;cam.enabled=false;
  // street level (taxi eye 1.25 m) along J6480~J7204, plus one raised view
  var views=new[]{
   ("east-along",new Vector3(592,1.25f,254),new Vector3(660,2.2f,264)),
   ("west-along",new Vector3(704,1.25f,256),new Vector3(630,2.2f,268)),
   ("north-face",new Vector3(640,1.25f,252),new Vector3(640,2.6f,280)),
   ("south-face",new Vector3(640,1.25f,258),new Vector3(640,2.6f,228)),
   ("raised",new Vector3(582,12f,255),new Vector3(650,1f,255)), // above the road, both frontages
  };
  foreach(var v in views){
   var from=v.Item2;from.y+=Ground(from);var at=v.Item3;at.y+=Ground(at);
   cam.transform.SetPositionAndRotation(from,Quaternion.LookRotation(at-from));
   yield return new WaitForEndOfFrame();
   var rt=new RenderTexture(1600,900,24){antiAliasing=4};var prev=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
   File.WriteAllBytes(Path.Combine(folder,"shop-shutter-"+v.Item1+".png"),tex.EncodeToPNG());
   cam.targetTexture=null;RenderTexture.active=prev;Destroy(tex);rt.Release();Destroy(rt);
   report.AppendLine("captured "+v.Item1);
  }
 }

 static void Finish(StringBuilder report,bool ok){
  if(!Has("-shopShutterTrial")){Debug.Log(report.ToString());return;}
  var folder=Path.GetFullPath(Path.Combine("artifacts","shop-shutter"));Directory.CreateDirectory(folder);
  report.AppendLine("success="+ok);File.WriteAllText(Path.Combine(folder,"shop-shutter-trial.txt"),report.ToString());
  Debug.Log("SHOP_SHUTTER_TRIAL_"+(ok?"OK":"FAILED")+"\n"+report);Application.Quit(ok?0:1);
 }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
// Isolated presentationTest: fixed cameras and geometry inspection, not driving/field verification.
public class RoadPresentationProbe : MonoBehaviour {
 public DriveSession session;
 readonly List<string> report=new List<string>();int checks;
 void Check(bool value,string why){checks++;report.Add((value?"PASS ":"FAIL ")+why);if(!value)throw new Exception(why);}
 IEnumerator Start(){
  while(!session||!session.StartupReady)yield return null;yield return null;yield return null;
  bool ok=true;string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts"));Directory.CreateDirectory(folder);
  try{
   Check(session.business.SavePath.StartsWith(folder+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"presentation uses isolated artifact save");
   session.business.enabled=false;session.jobs.enabled=false;session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.drivingCamera.enabled=false;
   foreach(var text in session.traffic.GetComponentsInChildren<TextMesh>(true))Check(!text.text.Contains("횡단보도"),"no fabricated crosswalk text board: "+text.name);
   int approaches=0,protectedCount=0,permissiveCount=0;
   foreach(var j in session.traffic.junctions)for(int i=0;i<j.lamps.Count;i++){
    var lamps=j.lamps[i];if(lamps==null||lamps.Length==0||!lamps[0])continue;approaches++;
    var sign=lamps[0].transform.parent.Find("비보호 표지");bool expected=session.traffic.PermissiveLeft(j,j.lampAxes[i],j.lampSigns[i])&&(!j.scenarioSignal||j.lampAxes[i]==j.observationAxis&&j.lampSigns[i]==j.observationSign);
    Check((sign&&sign.gameObject.activeInHierarchy)==expected,"matching actual signal condition "+j.name+"/"+i);
    if(expected)permissiveCount++;else protectedCount++;
    if(sign){var back=sign.Find("회색 뒷판");Check(back&&back.localScale.x<1&&back.localScale.y<1.2f,"compact condition plate");Check(sign.GetComponentsInChildren<TextMesh>(true).All(t=>t.text=="비보호"),"condition uses arrow plus short actual legend");}
   }
   Check(approaches>0&&protectedCount>0,"actual signal approaches inspected");report.Add("approaches="+approaches+" protected="+protectedCount+" permissive="+permissiveCount);
   var junc=session.traffic.junctions.FirstOrDefault(j=>j.name=="J5750_2576"&&j.lamps.Count>0)??session.traffic.junctions.First(j=>j.lamps.Count>0);
   var forward=session.traffic.AxisDirection(junc,junc.lampAxes[0])*junc.lampSigns[0];var camera=Camera.main;
   Capture(camera,junc.center-forward*24+Vector3.up*5,junc.center+forward*7+Vector3.up*3.3f,Path.Combine(folder,"signage-junction-view.png"),1900,998);
   Capture(camera,junc.center+forward*24+Vector3.up*5,junc.center-forward*7+Vector3.up*3.3f,Path.Combine(folder,"signage-junction-opposite.png"),1900,998);
   if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadContinuityViews")>=0){
    foreach(var entry in new[]{new Vector3(590,0,1240),new Vector3(700,0,1100),new Vector3(800,0,640),new Vector3(1035,0,642),new Vector3(463,0,1381),new Vector3(616,0,83),new Vector3(570,0,1422),new Vector3(193,0,88),new Vector3(193,0,518),new Vector3(725,0,1478)}){
     Capture(camera,entry+new Vector3(0,85,-35),entry,Path.Combine(folder,"road-continuity-"+(int)entry.x+"-"+(int)entry.z+".png"),1600,1000);
    }
    CaptureRoadGrid(camera,folder);
   }
   var actual=FindObjectsOfType<Transform>().FirstOrDefault(t=>t.name=="비보호 표지"&&t.gameObject.activeInHierarchy);
   GameObject fixture=null;if(!actual){fixture=RoadSignage.CreatePermissive(session.traffic.transform,Vector3.up*60);actual=fixture.transform;report.Add("condition detail uses rendering fixture: actual map has no permissive approach");}
   var centre=actual.position;
   Capture(camera,centre-actual.forward*2,centre,Path.Combine(folder,"signage-condition-front.png"),1280,720);
   Capture(camera,centre+actual.forward*2,centre,Path.Combine(folder,"signage-condition-back.png"),1280,720);
   if(fixture)Destroy(fixture);
  }catch(Exception e){ok=false;report.Add(e.ToString());Debug.LogException(e);}
  report.Add("device="+SystemInfo.graphicsDeviceName+" mode=fixed-camera-and-geometry manualDriving=false");File.WriteAllText(Path.Combine(folder,"road-signage-player.txt"),(ok?"PASS":"FAIL")+" checks="+checks+"\n"+string.Join("\n",report));Debug.Log("ROAD_SIGNAGE_PLAYER_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
 [Serializable] class GridTile {public string id,image,sheet;public int sheetColumn,sheetRowFromTop;public float minX,minZ,maxX,maxZ;}
 [Serializable] class GridCoverage {public string kind,id;public float margin;public int segments,coveredSegments;public bool covered;public string[] tiles;}
 [Serializable] class GridManifest {
  public string method="actual built world Camera.Render; top-down orthographic; north is image top",limitation="Frustum coverage of road geometry; does not prove visibility behind scene occluders or correctness of paint. Fixed camera, not manual driving.";
  public int resolution=768,columns,rows;public float tileMetres=260,stepMetres=230,mapWidth,mapDepth,minX,minZ,maxX,maxZ;
  public bool allRoadsCovered;public List<GridTile> tiles=new List<GridTile>();public List<GridCoverage> roads=new List<GridCoverage>();
 }
 void CaptureRoadGrid(Camera camera,string folder){
  var map=session.world.map;var manifest=new GridManifest{mapWidth=map.width,mapDepth=map.depth,minX=0,minZ=0,maxX=map.width,maxZ=map.depth};
  var paths=new List<Tuple<string,string,MapPoint[],float>>();
  foreach(var edge in map.hdEdges??new HdEdge[0])if(!edge.disabled)paths.Add(Tuple.Create("active-hd-lane",edge.id,edge.points,4f));
  foreach(var road in map.roads??new MapRoad[0])paths.Add(Tuple.Create("map-road",road.id,road.points,Mathf.Max(4,road.width*.5f+1)));
  // Expand beyond map bounds to include complete road envelopes near its edges.
  foreach(var path in paths)foreach(var p in path.Item3??new MapPoint[0]){
   manifest.minX=Mathf.Min(manifest.minX,p.x-path.Item4);manifest.maxX=Mathf.Max(manifest.maxX,p.x+path.Item4);
   manifest.minZ=Mathf.Min(manifest.minZ,p.z-path.Item4);manifest.maxZ=Mathf.Max(manifest.maxZ,p.z+path.Item4);
  }
  // Overlap also accommodates the widest road envelope in the containment proof.
  float margin=paths.Count==0?4:paths.Max(p=>p.Item4);manifest.stepMetres=Mathf.Min(230,260-2*margin-10);
  Check(manifest.stepMetres>0,"road grid overlap supports full road widths");
  manifest.columns=Mathf.Max(1,Mathf.CeilToInt((manifest.maxX-manifest.minX-260)/manifest.stepMetres)+1);
  manifest.rows=Mathf.Max(1,Mathf.CeilToInt((manifest.maxZ-manifest.minZ-260)/manifest.stepMetres)+1);
  string output=Path.Combine(folder,"road-continuity-grid");Directory.CreateDirectory(output);
  int sheetColumns=(manifest.columns+3)/4;
  for(int row=0;row<manifest.rows;row++)for(int col=0;col<manifest.columns;col++){
   float x=manifest.minX+col*manifest.stepMetres,z=manifest.minZ+row*manifest.stepMetres;
   string id="r"+row.ToString("D2")+"-c"+col.ToString("D2");
   manifest.tiles.Add(new GridTile{id=id,image=id+".png",sheet="sheet-"+(row/3*sheetColumns+col/4).ToString("D2")+".png",sheetColumn=col%4,sheetRowFromTop=2-row%3,minX=x,minZ=z,maxX=x+260,maxZ=z+260});
  }
  foreach(var path in paths){
   var coverage=new GridCoverage{kind=path.Item1,id=path.Item2,margin=path.Item4};var tileIds=new HashSet<string>();var points=path.Item3;
   if(points!=null)for(int i=1;i<points.Length;i++){
    var a=points[i-1].V();var b=points[i].V();int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/5));
    for(int n=0;n<steps;n++){
     var p=Vector3.Lerp(a,b,(float)n/steps);var q=Vector3.Lerp(a,b,(float)(n+1)/steps);coverage.segments++;
     var tile=manifest.tiles.FirstOrDefault(t=>Mathf.Min(p.x,q.x)-path.Item4>=t.minX&&Mathf.Max(p.x,q.x)+path.Item4<=t.maxX&&Mathf.Min(p.z,q.z)-path.Item4>=t.minZ&&Mathf.Max(p.z,q.z)+path.Item4<=t.maxZ);
     if(tile!=null){coverage.coveredSegments++;tileIds.Add(tile.id);}
    }
   }
   coverage.covered=coverage.segments>0&&coverage.segments==coverage.coveredSegments;coverage.tiles=tileIds.OrderBy(x=>x).ToArray();manifest.roads.Add(coverage);
  }
  manifest.allRoadsCovered=paths.Count>0&&manifest.roads.All(p=>p.covered);
  File.WriteAllText(Path.Combine(output,"coverage.json"),JsonUtility.ToJson(manifest,true));
  Check(manifest.allRoadsCovered,"full rendered grid contains all active lane and road segment envelopes");
  var position=camera.transform.position;var rotation=camera.transform.rotation;bool orthographic=camera.orthographic;float size=camera.orthographicSize,aspect=camera.aspect,near=camera.nearClipPlane,far=camera.farClipPlane;
  var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(768,768,24);var pixels=new Texture2D(768,768,TextureFormat.RGB24,false);var sheet=new Texture2D(3072,2304,TextureFormat.RGB24,false);
  try{
   camera.orthographic=true;camera.orthographicSize=130;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.targetTexture=rt;
   foreach(var group in manifest.tiles.GroupBy(t=>t.sheet)){
    sheet.SetPixels32(new Color32[3072*2304]);
    foreach(var tile in group){
     camera.transform.SetPositionAndRotation(new Vector3((tile.minX+tile.maxX)*.5f,600,(tile.minZ+tile.maxZ)*.5f),Quaternion.Euler(90,0,0));camera.Render();RenderTexture.active=rt;
     pixels.ReadPixels(new Rect(0,0,768,768),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,tile.image),pixels.EncodeToPNG());
     sheet.ReadPixels(new Rect(0,0,768,768),tile.sheetColumn*768,(2-tile.sheetRowFromTop)*768);
    }
    sheet.Apply();File.WriteAllBytes(Path.Combine(output,group.Key),sheet.EncodeToPNG());
   }
  }finally{
   camera.targetTexture=prior;RenderTexture.active=active;camera.transform.SetPositionAndRotation(position,rotation);camera.orthographic=orthographic;camera.orthographicSize=size;camera.aspect=aspect;camera.nearClipPlane=near;camera.farClipPlane=far;
   Destroy(pixels);Destroy(sheet);rt.Release();Destroy(rt);
  }
  report.Add("ROAD_GRID tiles="+manifest.tiles.Count+" roads="+manifest.roads.Count+" resolution=768 span=260m manifest="+Path.Combine(output,"coverage.json"));
 }
 void Capture(Camera camera,Vector3 eye,Vector3 target,string path,int width,int height){
  var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
  var texture=new RenderTexture(width,height,24);var active=RenderTexture.active;var prior=camera.targetTexture;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
  try{
   camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));camera.fieldOfView=50;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
   pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
  }finally{camera.targetTexture=prior;RenderTexture.active=active;camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;Destroy(pixels);texture.Release();Destroy(texture);}
  report.Add("CAPTURE "+Path.GetFileName(path)+" "+width+"x"+height+" eye="+eye+" target="+target);
 }
}

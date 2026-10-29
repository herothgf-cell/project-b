using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Explicit source edit, invoked once in the final integration phase. Input hashes are not bypassed.
public static class WorldFollowupMapEdits {
 public const string Crossing="J6480_2573";
 const string Prefix="observed:commercial-midblock:";
 const string Naver="https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15,0,0,0,adh&p=i7qori-dtj0W3l_zrXdqrg,-45,10,80,Float";
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="observed",url=Naver,month="2026-05",checkedAt="2026-10-29",appearanceConfirmed=true,ruleConfirmed=true,note="사용자 지도/네이버 거리뷰의 상가 중앙 신호 횡단보도. 좌표 대응·시설 치수·신호 시간은 게임 추정."};}
 static RoadMapLane Lane(RoadMapDocument d,Vector3 p,Vector3 forward){
  return d.lanes.Where(l=>!l.disabled&&l.turn==0&&l.change==0&&!l.id.StartsWith("join:")).Select(l=>{var path=new AssistedRoadNetwork.Path(l.points.Select(q=>q.V()).ToArray());float error;float at=path.Project(p,out error);return new{lane=l,error=Vector3.Dot(path.Tangent(at),forward)>.9f?error:float.MaxValue};}).OrderBy(l=>l.error).First().lane;
 }
 static RoadMapFacility Mark(string id,string kind,Vector3 p,float yaw,float width,float length,int sign,string lane){return new RoadMapFacility{id=Prefix+id,kind=kind,type="안내",junctionId=Crossing,laneId=lane,x=p.x,z=p.z,yaw=yaw,width=width,length=length,axis=0,sign=sign,text="",evidence=Evidence()};}
 static void CorrectBlue(HybridJunctionRule r){r.signalMode=0;r.crosswalk=true;r.stopLine=false;r.facilityStatus="user-corrected";r.facilityCheckedAt="2026-10-29";r.facilityEvidence="User map + Naver May2026 commercial T-crossing: marked crosswalk, no traffic signals. No surveyed timing or dimensions.";}
 static bool ClearPath(MapData geometry,RoadMapLane lane,MapPoint[] points){
  var path=new AssistedRoadNetwork.Path(points.Select(p=>p.V()).ToArray());for(float at=0;at<=path.length;at+=.5f){var center=path.Position(at);var side=TrafficSystem.Right(path.Tangent(at));foreach(float offset in new[]{-lane.width*.5f,0,lane.width*.5f}){var q=center+side*offset;if(ReviewedPavement.Contains(geometry,q))return false;foreach(var b in geometry.buildings)if(!SceneBuildingVisibility.Hidden(geometry,b.id)&&RoadMapRules.Polygon(b.points,q))return false;}}return true;
 }
 static int NormalizeConnections(RoadMapDocument d,MapData geometry){
  var targets=new HashSet<string>(new[]{"J993_5620","J1927_5181","J5057_4449","J7973_3744","J8066_6364","J7801_9629"});var lanes=d.lanes.ToDictionary(l=>l.id);int count=0;
  foreach(var join in d.lanes.Where(l=>!l.disabled&&l.id.StartsWith("join:")&&!l.id.StartsWith("join:reviewed:")&&l.change==0&&l.turn!=UturnMovement.Turn&&targets.Contains(l.ruleId))){
   var incoming=d.lanes.Where(l=>!l.disabled&&l.next.Contains(join.id)).ToArray();RoadMapLane target;if(incoming.Length!=1||join.next.Length!=1||!lanes.TryGetValue(join.next[0],out target)||target.disabled)continue;
   var from=incoming[0];var a=from.points.Last().V();var b=target.points[0].V();var forward=(a-from.points[from.points.Length-2].V()).normalized;var heading=(target.points[1].V()-b).normalized;float handle=Vector3.Distance(a,b)*.35f;
   // Retain every admitted movement/ID and exact lane endpoints. Only remove discontinuous connector tangents.
   var points=new MapPoint[33];var heights=new float[33];float startHeight=from.heights!=null&&from.heights.Length>0?from.heights.Last():0,endHeight=target.heights!=null&&target.heights.Length>0?target.heights[0]:0;
   for(int n=0;n<points.Length;n++){float t=n/32f,u=1-t;var q=u*u*u*a+3*u*u*t*(a+forward*handle)+3*u*t*t*(b-heading*handle)+t*t*t*b;points[n]=new MapPoint{x=q.x,z=q.z};heights[n]=Mathf.Lerp(startHeight,endHeight,t);}
   if(ClearPath(geometry,join,points)){join.points=points;join.heights=heights;count++;}else Debug.Log("WORLD_FOLLOWUP_CONNECTOR_PRESERVED_CLEARANCE "+join.id);
  }return count;
 }
 public static void FreezePhysicalCorridors(){
  string root=Path.GetFullPath("../..");var p=ProjectMapStore.Load(root);string path=Path.Combine(root,"config/maps/eungye-corridor-polish.json");var config=JsonUtility.FromJson<CorridorPolish.Settings>(File.ReadAllText(path));
  if(!config.includeScenarioRoutes)throw new InvalidOperationException("Physical corridors already frozen; do not overwrite original evidence.");
  File.WriteAllText(Path.Combine(root,"artifacts/world-followup-original-scene.sha256"),ProjectMapPackage.Digest(JsonUtility.ToJson(ProjectMapScene.Capture(p.baseline))));
  var clean=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(p.baseline));clean.roads=clean.roads.Where(r=>!r.id.StartsWith("fuel:")&&!r.id.StartsWith("restaurant:")&&!r.id.StartsWith("polish:")).ToArray();
  CorridorPolish.Apply(clean,config);var result=CorridorPolish.For(clean);config.roadIds=result.sources.OrderBy(x=>x,StringComparer.Ordinal).ToArray();config.includeScenarioRoutes=false;config.status+="; 2026-10-29 physical source selection frozen from validated project baseline, independent of signal scenario changes";
  File.WriteAllText(path,JsonUtility.ToJson(config,true),new System.Text.UTF8Encoding(false));File.Copy(path,"Assets/Resources/eungye-corridor-polish.json",true);UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/eungye-corridor-polish.json",UnityEditor.ImportAssetOptions.ForceUpdate);
  Debug.Log("WORLD_FOLLOWUP_CORRIDORS_FROZEN sources="+config.roadIds.Length+" widths="+result.widths.Count+" surfaces="+result.ribbons);
 }
 public static void Apply(){
  string root=Path.GetFullPath("../.."),fingerprint=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);var d=p.document;
  foreach(string id in new[]{"J5750_2576","J7204_2569"})CorrectBlue(p.baseline.junctionRules.First(j=>j.id==id));
  var junction=new HybridJunctionRule{id=Crossing,x=648,z=257.3f,dx=1,dz=0,signalMode=2,crosswalk=false,stopLine=true,allowLeft=false,allowRight=false,allowStraight=true,facilityStatus="user-corrected",facilityCheckedAt="2026-10-29",facilityEvidence=Naver+"; single midblock crossing observed; mapped centre/dimensions and35/12s stages are game estimates.",approaches=new[]{new HybridApproach{dx=1,dz=0,width=17.6f},new HybridApproach{dx=-1,dz=0,width=17.6f}}};
  p.baseline.junctionRules=p.baseline.junctionRules.Where(j=>j.id!=Crossing).Concat(new[]{junction}).ToArray();
  var east=Lane(d,new Vector3(643,0,253),Vector3.right);var west=Lane(d,new Vector3(653,0,261.5f),Vector3.left);
  var fs=d.facilities.Where(f=>!f.id.StartsWith(Prefix)).ToList();
  var crossing=Mark("crosswalk","crosswalk",new Vector3(648,0,257.3f),90,17.6f,4,1,east.id);fs.Add(crossing);
  var eStop=Mark("east-stop","stopLine",new Vector3(643.5f,0,252.9f),90,8.5f,.4f,1,east.id);fs.Add(eStop);
  var wStop=Mark("west-stop","stopLine",new Vector3(652.5f,0,261.7f),270,8.5f,.4f,-1,west.id);fs.Add(wStop);
  var eVehicle=new RoadMapSignalGroup{id=Prefix+"east",name="동향 차량",axis=0,sign=1,turn=0};var wVehicle=new RoadMapSignalGroup{id=Prefix+"west",name="서향 차량",axis=0,sign=-1,turn=0};
  var northWalk=new RoadMapSignalGroup{id=Prefix+"walk-north",name="보행 북단",pedestrian=true,axis=0,sign=1,crosswalkId=crossing.id};var southWalk=new RoadMapSignalGroup{id=Prefix+"walk-south",name="보행 남단",pedestrian=true,axis=0,sign=-1,crosswalkId=crossing.id};
  var program=new RoadMapSignalProgram{junctionId=Crossing,groups=new[]{eVehicle,wVehicle,northWalk,southWalk},stages=new[]{new RoadMapStage{name="차량 통행 (게임 시간)",green=35,yellow=3,clearance=2,groups=new[]{eVehicle.id,wVehicle.id}},new RoadMapStage{name="보행 횡단 (게임 시간)",green=12,yellow=0,clearance=2,groups=new[]{northWalk.id,southWalk.id}}}};
  foreach(int sign in new[]{1,-1}){
   var stop=sign>0?eStop:wStop;var lane=sign>0?east:west;var signal=Mark(sign>0?"east-signal":"west-signal","vehicleSignal",new Vector3(648+sign*8,0,257.3f-sign*9.8f),sign>0?90:270,5.4f,.3f,sign,lane.id);signal.height=4.9f;signal.groupId=sign>0?eVehicle.id:wVehicle.id;signal.targetId=stop.id;fs.Add(signal);
   var walk=Mark(sign>0?"north-walk":"south-walk","pedestrianSignal",new Vector3(648,0,257.3f+sign*10),sign>0?0:180,.65f,.3f,sign,"");walk.height=2.7f;walk.groupId=sign>0?northWalk.id:southWalk.id;walk.targetId=crossing.id;fs.Add(walk);
  }
  d.facilities=fs.ToArray();d.signals=d.signals.Where(s=>s.junctionId!=Crossing&&s.junctionId!="J5750_2576"&&s.junctionId!="J7204_2569").Concat(new[]{program}).ToArray();
  var geometry=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(p.baseline));ProjectMapScene.Apply(geometry,d.scene);int normalized=NormalizeConnections(d,geometry);
  string expectedScene="07f690dee7b361448ec21a1facba2ccec49467eb2ef0a4a4437aa327d07be44f";string actualScene=ProjectMapPackage.Digest(JsonUtility.ToJson(ProjectMapScene.Capture(p.baseline)));if(expectedScene!=actualScene)throw new InvalidOperationException("Signal edits must preserve the original physical baseline scene.");
  d.revision++;d.savedAt=DateTime.UtcNow.ToString("o");p.baselineHash=ProjectMapPackage.Digest(JsonUtility.ToJson(p.baseline));p.baseline.editorBaseHash=p.baselineHash;d.baseHash=p.baselineHash;d.projectRevision="";p.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(d));ProjectMapStore.Save(root,p,fingerprint);
  string path=Path.Combine(root,"config/maps/eungye-traffic-overrides.json");var overrides=JsonUtility.FromJson<HybridOverrides>(File.ReadAllText(path));var rules=overrides.junctions.Where(r=>r.id!=Crossing&&r.id!="J5750_2576"&&r.id!="J7204_2569").ToList();foreach(string id in new[]{"J5750_2576","J7204_2569"})rules.Add(p.baseline.junctionRules.First(j=>j.id==id));rules.Add(junction);overrides.junctions=rules.ToArray();File.WriteAllText(path,JsonUtility.ToJson(overrides,true),new System.Text.UTF8Encoding(false));
  Debug.Log("WORLD_FOLLOWUP_MAP_EDITED twoUnsignalized=true midblock="+Crossing+" normalizedConnections="+normalized+" projectSource="+ProjectMapStore.Fingerprint(root));
 }
}

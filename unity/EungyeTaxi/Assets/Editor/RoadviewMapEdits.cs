using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Explicit, repeatable appearance-only source update. Movement topology is preserved.
public static class RoadviewMapEdits {
 const string Prefix="reviewed:2026-11-09:";
 const string East="https://map.naver.com/p?c=18.00,0,0,0,adh&p=LMCaJTYVRVjleY6YcYU_4g,90,-20,80,Float";
 const string North="https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15.99,0,0,0,adh&p=mFn3SPTx5TLQAlev8v1uyQ,21,0,35,Float";
 const string NorthHead="https://map.naver.com/p?c=19.00,0,0,0,adh&p=j346rcCdslMpOzuyx4gG3Q,0,0,90,Float";
 const string West="https://map.naver.com/p?c=19.00,0,0,0,adh&p=cWFgUsKpLaYz-MMUKXztag,281,3,30,Float";
 const string Midblock="https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15,0,0,0,adh&p=i7qori-dtj0W3l_zrXdqrg,-45,10,80,Float";
 static void Check(bool ok,string reason){if(!ok)throw new Exception("ROADVIEW_MAP: "+reason);}
 static RoadMapEvidence Evidence(string url,string note,bool confirmed=true){return new RoadMapEvidence{state=confirmed?"observed":"user",url=url,month="2026-05",checkedAt="2026-11-09",appearanceConfirmed=confirmed,ruleConfirmed=false,note=note+" 좌표·폭·표시 간격·신호 시간은 게임 추정이며 통행 허용은 변경하지 않음."};}
 static RoadMapFacility Head(ProjectMapPackage p,string junction,int axis,int sign,string type,string url,string note){var rule=p.baseline.junctionRules.Single(j=>j.id==junction);return new RoadMapFacility{id=Prefix+junction+":"+axis+":"+sign+":head",kind="signalHead",type=type,junctionId=junction,axis=axis,sign=sign,x=rule.x,z=rule.z,text="",evidence=Evidence(url,note)};}
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);Check(p.baselineHash=="81b34b282c27bb03e2ead75ba3385d2c4e5e2bdd0730c53fcbe14fb8f30aebbe","known source baseline required before mutation");
  string baseline=p.baselineHash,lanes=ProjectMapPackage.Digest(JsonUtility.ToJson(new RoadMapDocument{id="",lanes=p.document.lanes})),programs=ProjectMapPackage.Digest(JsonUtility.ToJson(new RoadMapDocument{id="",signals=p.document.signals}));
  var fs=p.document.facilities.Where(f=>!f.id.StartsWith(Prefix)).ToList();
  fs.Add(Head(p,"J7917_2565",1,-1,"4구",East,"동향 접근 전면 4구 신호기 확인."));
  var northHead=Head(p,"J7973_3744",1,-1,"4구",NorthHead,"북향 접근 전면4구를 코너 2024-03 거리뷰에서 확인. 차로 표시의 2026-05 근거와 구분.");northHead.evidence.month="2024-03";fs.Add(northHead);
  fs.Add(Head(p,"J1927_5181",1,1,"3구",West,"서향 접근 3구 및 좌회전 금지 표지를 직접 확인. 기존 좌회전 금지 유지."));
  foreach(string id in new[]{"A2218G610050:0","A2218G610051:0","A2218G610052:0"}){
   var lane=p.document.lanes.Single(l=>l.id==id);var path=new AssistedRoadNetwork.Path(lane.points.Select(q=>q.V()).ToArray());
   fs.Add(new RoadMapFacility{id=Prefix+id+":paint",kind="lanePresentation",type="원본 차로",junctionId="J7973_3744",axis=1,sign=-1,laneId=id,width=lane.width,startAt=0,endAt=path.length,x=lane.points[0].x,z=lane.points[0].z,text="",evidence=Evidence(North,"중앙분리대 인접 좌회전 전용1개·우측 직진2개와 별도 우회전 공간 확인. NGII 기존 중심선과 연결에서 화살표/내부 구분선을 복구하며 원본 lane number91은 차선 수가 아님.")});
  }
  var midblock=fs.Where(f=>f.junctionId=="J6480_2573"&&f.kind=="vehicleSignal").ToArray();Check(midblock.Length==2&&midblock.Any(f=>f.id=="observed:commercial-midblock:east-signal")&&midblock.Any(f=>f.id=="observed:commercial-midblock:west-signal"),"both known midblock heads required before save");
  foreach(var f in midblock){
   f.type="3구";f.evidence=Evidence(Midblock,f.sign==1?"횡단보도 전면 3구 확인. 반대편 신호기는 뒤쪽 외형만 보임.":"동향 전면3구·반대편 뒷면 확인 및 사용자 3구 지정. 서향 전면 램프 수는 이번 거리뷰에서 별도 확인하지 못함.",f.sign==1);
  }
  p.document.facilities=fs.ToArray();p.document.revision++;p.document.savedAt=DateTime.UtcNow.ToString("o");p.document.projectRevision="";p.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(p.document));
  Check(p.baselineHash==baseline&&ProjectMapPackage.Digest(JsonUtility.ToJson(new RoadMapDocument{id="",lanes=p.document.lanes}))==lanes&&ProjectMapPackage.Digest(JsonUtility.ToJson(new RoadMapDocument{id="",signals=p.document.signals}))==programs,"appearance edit preserves baseline, all lane data and authored signal programs");
  ProjectMapStore.Save(root,p,token);ProjectMapBuild.Prepare();Verify();
  Debug.Log("ROADVIEW_MAP_APPLIED baseline="+baseline+" content="+p.contentHash+" source="+ProjectMapStore.Fingerprint(root));
 }
 public static void Verify(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var map=p.Compose();
  Check(p.baselineHash=="81b34b282c27bb03e2ead75ba3385d2c4e5e2bdd0730c53fcbe14fb8f30aebbe","existing baseline hash preserved");
  Check(map.editorDocument.facilities.Count(f=>f.id.StartsWith(Prefix)&&f.kind=="signalHead")==3,"three explicit generated head assignments");
  var paint=LaneDirectionPaint.Layout(map);foreach(string id in new[]{"A2218G610050:0","A2218G610051:0","A2218G610052:0"}){int edge=Array.FindIndex(map.hdEdges,e=>e.id==id);Check(paint.Any(a=>a.edgeIndex==edge&&a.turns==(id=="A2218G610050:0"?LaneDirectionPaint.Left:LaneDirectionPaint.Straight)),"actual native lane role painted "+id);}
  var midblock=map.editorDocument.facilities.Where(f=>f.junctionId=="J6480_2573"&&f.kind=="vehicleSignal").ToArray();Check(midblock.Length==2&&midblock.All(f=>f.type=="3구")&&midblock.Any(f=>f.id=="observed:commercial-midblock:east-signal")&&midblock.Any(f=>f.id=="observed:commercial-midblock:west-signal"),"both known midblock physical heads explicitly three");
  var catalog=PassengerStopCatalog.For(map);Check(catalog.Stops.Length==24&&catalog.Rejected.Count==0,"all24 passenger stops preserved");
  var graph=new HdRoadGraph(map);int routes=0;foreach(var a in catalog.Stops)foreach(var b in catalog.Stops){if(a==b)continue;float distance;Check(PassengerStopCatalog.Reachable(graph,a,b,out distance),"directed route "+a.name+" to "+b.name);routes++;}
  Debug.Log("ROADVIEW_MAP_OK nativeRoles=3 stops=24 directedRoutes="+routes+" content="+p.contentHash);
 }
}

using UnityEngine;

// Always exposes the distinction between source geometry and temporary rules.
public class HybridMapStatus : MonoBehaviour {
 public DriveSession session;public string current="임시 도로 설정";float next;GUIStyle style;
 void Start(){
  var map=session.world.map;MapStop spawn=map.stops[0];float best=float.MaxValue;
  foreach(var s in map.stops){float d=Vector3.Distance(s.V(),map.targets[0].V());if(d<best){best=d;spawn=s;}}
  var path=session.jobs.nav.hd.paths[spawn.laneEdge];float at=Mathf.Max(1,spawn.laneAt-10);
  session.taxi.SetSpawn(path.Position(at)+Vector3.up*.65f,Quaternion.LookRotation(path.Tangent(at)));session.taxi.ResetTaxi();
 }
 void Update(){
  if(Time.time<next)return;next=Time.time+.4f;float at;int id=session.jobs.nav.hd.Nearest(session.taxi.transform.position,session.taxi.transform.forward,out at);
  if(id<0){current="차로 밖 · 현장 통행 규제 미검증";return;}var e=session.jobs.nav.hd.edges[id];
  current=(e.status=="ngii"?"국도42 원본 차로 · 규제 현황 미검증":e.status=="provisional-portal"?"임시 접속 구간":"임시 차로·교차로 설정")+"\n"+e.ruleId;
  HybridJunctionRule nearby=null;float distance=65;foreach(var r in session.world.map.junctionRules){if(r.signalMode==0&&!r.crosswalk&&!r.stopLine)continue;float d=Vector3.Distance(r.V(),session.taxi.transform.position);if(d<distance){distance=d;nearby=r;}}
  if(mapSupplementAvailable())current+="\nOSM 보완 적용 · 현장 미확인";
  if(nearby!=null)current+="\n시설: "+(nearby.facilityStatus=="verified"?"현장 확인 "+nearby.facilityCheckedAt:"임시 설정 · 현장 미확인")+"\n"+nearby.id;
 }
 bool mapSupplementAvailable(){return session.world.map.supplementApplied;}
 void OnGUI(){if(GetComponent<DrivingReadability>())return;if(session.business.ShowingSummary||session.business.preparing||session.services.open||session.law.HistoryOpen)return;if(style==null)style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=12,wordWrap=true};GUI.Label(new Rect(Screen.width-254,455,245,36),"© OpenStreetMap contributors / ODbL\nopenstreetmap.org/copyright",style);GUI.Box(new Rect(Screen.width-254,340,238,112),GUIContent.none);GUI.Label(new Rect(Screen.width-245,345,220,103),current,style);}
}

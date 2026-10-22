using System;
using System.Linq;
using UnityEngine;
public partial class RoadMapEditor {
 public void EasyMapClick(Vector3 position){
  if(confirmClose||showSaved)return;
  var facility=document.facilities.OrderBy(f=>(f.Position-position).sqrMagnitude).FirstOrDefault();
  if(facility!=null&&Vector3.Distance(facility.Position,position)>Span/950*14)facility=null;
  if(easyAction=="place"){PlaceEasyFacility(position);return;}
  if(easyAction=="bindMark"||easyAction=="signalWalk"){
   string expected=easyAction=="signalWalk"?"crosswalk":document.facilities.First(f=>f.id==easySource).kind=="pedestrianSignal"?"crosswalk":"stopLine";
   if(facility==null||facility.kind!=expected){notice=expected=="crosswalk"?"횡단보도 표시를 클릭하세요.":"정지선 표시를 클릭하세요.";return;}
   var target=facility;
   if(easyAction=="bindMark"){var signal=document.facilities.First(f=>f.id==easySource);if(signal.junctionId!=target.junctionId){notice="같은 교차로의 노면을 선택하세요.";return;}Edit(()=>{signal.targetId=target.id;var p=document.signals.FirstOrDefault(x=>x.junctionId==signal.junctionId);var g=p==null?null:p.groups.FirstOrDefault(x=>x.id==signal.groupId);if(g!=null&&g.pedestrian)g.crosswalkId=target.id;});Select(signal.id);}
   else{var p=document.signals.First(x=>x.junctionId==junction);var g=p.groups.First(x=>x.id==easyGroup);if(target.junctionId!=junction){notice="같은 교차로의 횡단보도를 선택하세요.";return;}Edit(()=>{g.crosswalkId=target.id;foreach(var f in document.facilities.Where(f=>f.groupId==g.id))f.targetId=target.id;});}
   easyAction="select";notice="제어 대상을 연결했습니다.";return;
  }
  if(easyAction=="select"&&facility!=null){Select(facility.id);tool=facility.kind.Contains("Signal")?"신호":facility.kind=="sign"?"표지":"노면";dragFacility=true;easyFacilityMoved=false;facilityBefore=document.Copy();facilityOffset=facility.Position-position;return;}
  var lane=RoadMapEasySelection.PickLane(document,position,Mathf.Max(2,Span/950*12));
  if(lane==null){notice="도로 선을 클릭하세요. 휠로 확대하면 선택하기 쉽습니다.";return;}
  if(easyAction=="connect"){var source=document.lanes.FirstOrDefault(l=>l.id==easySource);if(source==null||source.id==lane.id){notice="다른 도착 차로를 선택하세요.";return;}var existing=source.next.Select(id=>document.lanes.FirstOrDefault(l=>l.id==id)).FirstOrDefault(l=>l!=null&&l.next.Contains(lane.id));if(existing!=null){Select(source.id);notice="이미 연결된 출구입니다.";easyAction="select";return;}float angle=Vector3.SignedAngle(RoadMapEasySelection.Heading(source),RoadMapEasySelection.Heading(lane),Vector3.up);int turn=Mathf.Abs(angle)<35?0:angle<0?-1:1;if(Mathf.Abs(angle)>145){notice="유턴 연결은 지원하지 않습니다. 다른 차로를 선택하세요.";return;}Edit(()=>Connect(source,lane.id,turn,0));Select(source.id);easyAction="select";notice="출구 연결 완료 · 검사와 시험 주행으로 확인하세요.";return;}
  if(easyAction=="bindLane"){var f=document.facilities.First(x=>x.id==easySource);Edit(()=>{f.laneId=lane.id;if(!string.IsNullOrEmpty(lane.ruleId))f.junctionId=lane.ruleId;AssignApproach(f,lane);if(f.type=="제한속도"){f.startAt=0;f.endAt=new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray()).length;}});Select(f.id);easyAction="select";notice="시설의 적용 차로를 연결했습니다.";return;}
  if(easyAction=="signalLane"){var p=document.signals.First(x=>x.junctionId==junction);var g=p.groups.First(x=>x.id==easyGroup);if(lane.ruleId!=junction){notice="선택 교차로로 들어오는 차로를 클릭하세요.";return;}var j=session.traffic.junctions.First(x=>x.name==junction);var dir=RoadMapEasySelection.Heading(lane);int axis=Mathf.Abs(Vector3.Dot(dir,session.traffic.AxisDirection(j,0)))>.7f?0:1;int sign=session.traffic.Sign(dir,axis,j);if(axis!=g.axis||sign!=g.sign){notice="이 신호와 같은 진입 방향의 차로를 클릭하세요.";return;}Edit(()=>g.laneIds=new[]{lane.id});easyAction="select";notice="선택 차로에 신호 그룹을 연결했습니다.";return;}
  if(easyAction=="trialStart"||easyAction=="trialGoal"){if(easyAction=="trialStart")TrialStart=lane.id;else TrialGoal=lane.id;easyAction="select";notice="시험 주행 위치를 지정했습니다.";return;}
  SelectRoadAt(position);
 }
 void EasyCanvasOverlay(){
  if(!SimpleMode)return;
  if(easyAction=="connect"){var source=document.lanes.FirstOrDefault(l=>l.id==easySource);if(source!=null){var a=source.points.Last().V();var b=Unproject(Event.current.mousePosition);for(int i=0;i<=28;i++){float t=i/28f;var pos=Vector3.Lerp(a,b,t);var at=Project(pos);if(canvas.Contains(at))Dot(at,Color.yellow,4);}}}
  if(easyAction=="place"&&canvas.Contains(Event.current.mousePosition)){Dot(Event.current.mousePosition,Color.yellow,18);GUI.Label(new Rect(Event.current.mousePosition.x+15,Event.current.mousePosition.y+8,160,28),placeType,label);}
  foreach(var l in ApproachLanes(easyApproach)){var path=new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray());if(path.length<9)continue;var p=path.Position(Mathf.Max(0,path.length-7));var at=Project(p);if(canvas.Contains(at)){Fill(new Rect(at.x-14,at.y-14,28,28),new Color(.03f,.07f,.1f));var style=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=18};GUI.Label(new Rect(at.x-14,at.y-14,28,28),Mathf.Max(1,l.number).ToString(),style);}}
 }
}

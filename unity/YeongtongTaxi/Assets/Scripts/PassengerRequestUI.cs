using UnityEngine;
public class PassengerRequestUI : MonoBehaviour {
 public DriveSession session;public readonly PassengerRequest request=new PassengerRequest();
 string observedTrip="";float stoppedSeconds;
 public void Begin(){if(!session.business.useV2)return;observedTrip=session.law.tripId;request.Begin(observedTrip,session.jobs.completed%2==0?RequestKind.Quiet:RequestKind.Eta);stoppedSeconds=0;}
 public void Finish(){if(!session.business.useV2)return;int deduction=request.Finish();if(deduction>0)session.manners.Deduct("request:"+observedTrip,deduction,"조용한 운행 요청 미응답",0);}
 void Update(){if(!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.paused)return;
  if(session.taxi.body.velocity.magnitude<.1f){stoppedSeconds+=Time.deltaTime;request.ObserveOpportunity(stoppedSeconds>=2);}else stoppedSeconds=0;
  if(Input.GetKeyDown(KeyCode.Y))request.Accept();if(Input.GetKeyDown(KeyCode.U))request.DeferSafety();
 }
 void OnGUI(){if(!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.paused)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};
  GUI.Box(new Rect(16,390,380,75),"");GUI.Label(new Rect(26,398,360,65),(request.Resolved?request.ResponseText():request.Kind==RequestKind.Quiet?"승객: 조용히 운행해 주세요.":"승객: 도착까지 얼마나 걸리나요? · 내비 예상 "+Mathf.CeilToInt(session.jobs.nav.estimatedSeconds)+"초 (예상치)")+(request.Resolved?"":"\nY 수락·응답 / U 안전상 보류 (감점 없음)"),style);
 }
}

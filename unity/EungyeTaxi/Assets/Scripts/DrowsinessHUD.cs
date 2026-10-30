using UnityEngine;
// Persisted exposure makes the toast one-shot per rest cycle, even after reload.
public class DrowsinessHUD : MonoBehaviour {
 public BusinessProgress business;
 void OnGUI(){
  if(!business||!business.DrowsinessRunning)return;
  var d=business.ledger.Driver.drowsiness;if(d==null||d.visionSeconds<=0)return;
  GUI.depth=10;
  if(d.VisionLoss>0){float loss=(float)d.VisionLoss,w=Screen.width,h=Screen.height;GUI.color=new Color(0,0,0,.35f*loss);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.black;float x=w*.175f*loss,y=h*.5f*loss;GUI.DrawTexture(new Rect(0,0,x,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(w-x,0,x,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x,0,w-2*x,y),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x,h-y,w-2*x,y),Texture2D.whiteTexture);GUI.color=Color.white;}
  GUI.depth=-40;var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=20,wordWrap=true,alignment=TextAnchor.MiddleCenter};
  if(!d.Blackout){
   if(d.visionSeconds<=5){float width=Mathf.Min(690,Screen.width-24);var toast=new Rect((Screen.width-width)/2,Screen.height*.75f,width,70);KoreanText.Panel(toast);GUI.Label(toast,"졸음 운전 위험 · 시야가 점점 좁아집니다 · 가까운 거점에서 쉬세요",style);}
   else {var taxi=business.session.taxi;var cam=Camera.main;var point=cam?cam.WorldToScreenPoint(taxi.transform.position+Vector3.up*2.8f):new Vector3(Screen.width*.5f,Screen.height*.52f,1);if(point.z>0){style.fontSize=28;style.normal.textColor=new Color(.65f,.9f,1);GUI.Label(new Rect(Mathf.Clamp(point.x-65,0,Screen.width-130),Screen.height-point.y-35-Mathf.Sin(Time.unscaledTime*2)*5,130,44),"Zzzz",style);}}
   return;
  }
  float panelWidth=Mathf.Min(690,Screen.width-24);var rect=new Rect((Screen.width-panelWidth)/2,Screen.height*.63f,panelWidth,155);KoreanText.Panel(rect);
  GUI.Label(new Rect(rect.x+10,rect.y+5,rect.width-20,72),PvpSimulationMode.Active?"졸음 운전 위험 · 정차 후 긴급 휴식\n100초 동안 운행 정지 · 피로 −20 · 경기 계속":"졸음 운전으로 위험합니다. 오늘 영업을 종료합니다.\n정차 후 귀가 휴식 · 피로 −20 (정상 수면 −40)",style);
  if(GUI.Button(new Rect(rect.x+20,rect.y+85,rect.width-40,52),PvpSimulationMode.Active?"긴급 휴식 · 100초 후 경기 계속":"귀가 휴식 후 오늘 영업 종료 · 100초",KoreanText.Button(19)))business.RequestEmergencyHome();
 }
}

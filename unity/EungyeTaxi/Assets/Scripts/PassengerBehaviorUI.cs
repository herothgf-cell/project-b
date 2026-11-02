using UnityEngine;
// Shared request stop gate and priority; one response surface handles the keyboard each frame.
public sealed class PassengerBehaviorUI : MonoBehaviour {
 public DriveSession session;
 float noticeUntil,responseUntil;
 string responseNotice="";PassengerBehaviorState responseOwner;
 public void NotifyResponse(int before,int after){responseOwner=session.jobs.Behavior;responseNotice="적용: 만족도 "+before+" → "+after+" ("+(after-before).ToString("+0;-0;0")+") · "+responseOwner.SelectedEffect;responseUntil=noticeUntil=Time.time+8;}
 public void NotifyStateChanged(){var b=session.jobs.Behavior;if(b==null)return;if(b.FollowupPending)session.requests.BlockResponseThisFrame();if(!b.Quiet||b.Sleeping||b.Type==PassengerType.Food)noticeUntil=Time.time+6;}

 public bool Showing {get{if(!session||!session.jobs||session.requests.ResponseBlockedThisFrame||session.jobs.Behavior==null||!session.jobs.Active||session.paused||session.FullMapOpen||session.ChoiceOpen||!string.IsNullOrEmpty(session.jobs.VisibleCareDialogue))return false;var b=session.jobs.Behavior;return b.Type!=PassengerType.General&&!session.requests.HasPendingChoice&&(Time.time<noticeUntil||!session.jobs.BehaviorAsleep&&(session.jobs.BehaviorChoicesOpen||b.WorkRemaining>0||!b.Quiet&&(b.Elapsed<8||b.MovingSeconds>=30&&b.MovingSeconds<38)));}}
 public bool CanRespond {get{return Showing&&session.jobs.BehaviorChoicesOpen&&session.requests.SafeToRespond()&&(session.jobs.trip.State!=TripState.ToPickup||session.jobs.At(session.jobs.current.pickup));}}
 void Update(){if(session.IsSimulationBot)return;if(session.jobs.CanRespondSituation){if(Input.GetKeyDown(KeyCode.Y))session.jobs.RespondSituation(0);else if(Input.GetKeyDown(KeyCode.I))session.jobs.RespondSituation(1);return;}if(!CanRespond)return;if(Input.GetKeyDown(KeyCode.Y))session.jobs.RespondBehavior(0);else if(Input.GetKeyDown(KeyCode.I))session.jobs.RespondBehavior(1);else if(Input.GetKeyDown(KeyCode.O))session.jobs.RespondBehavior(2);}
 void OnGUI(){if(session.IsSimulationBot)return;if(session.jobs.SituationVisible&&session.jobs.SituationPresentationAvailable){DrawSituation();return;}if(!Showing)return;var behavior=session.jobs.Behavior;
  string text=behavior.TypeLabel+" · "+behavior.VariantLabel+"\n"+behavior.Dialogue;
  if(responseOwner==behavior&&Time.time<responseUntil&&responseNotice.Length>0)text+="\n"+responseNotice;
  if(behavior.Type==PassengerType.Food&&session.jobs.BehaviorChoicesOpen)text+="\n기본 하차 청결 -2 별도 · 만족도 최대 100";
  if(session.jobs.BehaviorChoicesOpen&&!CanRespond)text+="\n교차로·횡단보도 밖에서 2초 정차 후 답변";
  var old=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};label.normal.textColor=new Color(.1f,.16f,.2f);
  float width=540,height=label.CalcHeight(new GUIContent(text),516)+22;var choices=session.jobs.BehaviorChoicesOpen?behavior.Choices:new string[0];float[] choiceHeights=new float[choices.Length];var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=12,wordWrap=true};float choiceTotal=0;for(int i=0;i<choices.Length;i++){choiceHeights[i]=Mathf.Max(48,button.CalcHeight(new GUIContent(new[]{"Y","I","O"}[i]+" · "+choices[i]),width-24)+8);choiceTotal+=choiceHeights[i]+4;}height+=choiceTotal;float x=(Screen.width/scale-width)/2,y=Screen.height/scale-24-height;
  CasualUI.Panel(new Rect(x,y,width,height),CasualUI.Soft);float title=height-choiceTotal-16;GUI.Label(new Rect(x+12,y+8,516,title),text,label);
  float choiceY=y+title+12;for(int i=0;i<choices.Length;i++){bool enabled=GUI.enabled;GUI.enabled=enabled&&CanRespond;if(GUI.Button(new Rect(x+8,choiceY,width-16,choiceHeights[i]),new[]{"Y","I","O"}[i]+" · "+choices[i],button))session.jobs.RespondBehavior(i);GUI.enabled=enabled;choiceY+=choiceHeights[i]+4;}
  GUI.matrix=old;
 }
 void DrawSituation(){
  var jobs=session.jobs;string text="승객 · 실제 운행 상황\n"+jobs.Situation.Dialogue+"\n응답 자체 만족도 변화0 · 청결은 실제 음식 사건만 적용";
  if(!jobs.CanRespondSituation&&!jobs.SituationWorkActive)text+="\n교차로·횡단보도 밖에서2초 안전 정차 후 답변 · 출발하면 답변 보류";
  var old=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};label.normal.textColor=new Color(.1f,.16f,.2f);
  float width=540,title=label.CalcHeight(new GUIContent(text),516)+16,height=title+(jobs.SituationWorkActive?8:112),x=(Screen.width/scale-width)/2,y=Screen.height/scale-24-height;
  GUI.color=new Color(.94f,.97f,.91f);GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(x+12,y+8,516,title),text,label);
  if(jobs.SituationWorkActive){GUI.matrix=old;return;}
  var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=12,wordWrap=true};bool enabled=GUI.enabled;GUI.enabled=enabled&&jobs.CanRespondSituation;
  if(GUI.Button(new Rect(x+8,y+title,524,48),jobs.Situation.OffersReSecure?"Y · 안전 정차한 동안 짐을 확인·재고정할게요\n작업5초 · 영업시간·연료 소모 · 추가 요금0원":"Y · 안전하게 상황을 설명할게요\n만족도 변화0 · 추가 약속 없음",button))jobs.RespondSituation(0);
  if(GUI.Button(new Rect(x+8,y+title+52,524,48),jobs.Situation.OffersReSecure?"I · 지금은 계속 운행하며 조용히 안내할게요\n작업 없음 · 만족도 변화0 · 후속 상황 질문 줄임":"I · 운전에 집중하며 조용히 안내할게요\n만족도 변화0 · 후속 상황 질문 줄임",button))jobs.RespondSituation(1);
  GUI.enabled=enabled;GUI.matrix=old;
 }
}

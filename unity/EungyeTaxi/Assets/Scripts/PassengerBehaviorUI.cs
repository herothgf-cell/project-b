using UnityEngine;
// Shared request stop gate and priority; one response surface handles the keyboard each frame.
public sealed class PassengerBehaviorUI : MonoBehaviour {
 public DriveSession session;
 float noticeUntil;
 public void NotifyStateChanged(){var b=session.jobs.Behavior;if(b==null)return;if(b.FollowupPending)session.requests.BlockResponseThisFrame();if(!b.Quiet||b.Sleeping||b.Type==PassengerType.Food)noticeUntil=Time.time+6;}

 public bool Showing {get{if(!session||!session.jobs||session.requests.ResponseBlockedThisFrame||session.jobs.Behavior==null||!session.jobs.Active||session.paused||session.FullMapOpen||session.ChoiceOpen||!string.IsNullOrEmpty(session.jobs.VisibleCareDialogue))return false;var b=session.jobs.Behavior;return b.Type!=PassengerType.General&&!session.requests.HasPendingChoice&&(Time.time<noticeUntil||!session.jobs.BehaviorAsleep&&(session.jobs.BehaviorChoicesOpen||b.WorkRemaining>0||!b.Quiet&&(b.Elapsed<8||b.MovingSeconds>=30&&b.MovingSeconds<38)));}}
 public bool CanRespond {get{return Showing&&session.jobs.BehaviorChoicesOpen&&session.requests.SafeToRespond()&&(session.jobs.trip.State!=TripState.ToPickup||session.jobs.At(session.jobs.current.pickup));}}
 void Update(){if(!CanRespond)return;if(Input.GetKeyDown(KeyCode.Y))session.jobs.RespondBehavior(0);else if(Input.GetKeyDown(KeyCode.I))session.jobs.RespondBehavior(1);else if(Input.GetKeyDown(KeyCode.O))session.jobs.RespondBehavior(2);}
 void OnGUI(){if(!Showing)return;var behavior=session.jobs.Behavior;
  string text=behavior.TypeLabel+" · "+behavior.VariantLabel+"\n"+behavior.Dialogue;
  if(session.jobs.BehaviorChoicesOpen&&!CanRespond)text+="\n교차로·횡단보도 밖에서 2초 안전 정차 후 선택";
  var old=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};label.normal.textColor=new Color(.1f,.16f,.2f);
  float width=540,height=label.CalcHeight(new GUIContent(text),516)+22;var choices=session.jobs.BehaviorChoicesOpen?behavior.Choices:new string[0];height+=choices.Length*40;float x=(Screen.width/scale-width)/2,y=Screen.height/scale-24-height;
  GUI.color=new Color(.94f,.97f,.91f);GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);GUI.color=Color.white;float title=height-choices.Length*40-16;GUI.Label(new Rect(x+12,y+8,516,title),text,label);
  var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=12,wordWrap=true};for(int i=0;i<choices.Length;i++){bool enabled=GUI.enabled;GUI.enabled=enabled&&CanRespond;if(GUI.Button(new Rect(x+8,y+title+12+i*40,width-16,36),new[]{"Y","I","O"}[i]+" · "+choices[i],button))session.jobs.RespondBehavior(i);GUI.enabled=enabled;}
  GUI.matrix=old;
 }
}

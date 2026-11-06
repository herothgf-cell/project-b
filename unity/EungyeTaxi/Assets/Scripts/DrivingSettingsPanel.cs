using UnityEngine;

// The pause menu owns this panel; editing never changes live preferences.
public sealed class DrivingSettingsPanel : MonoBehaviour {
 public bool IsOpen {get;private set;}
 public DrivingSettings Draft {get;private set;}
 public string PreferenceKey=DrivingSettings.Key;
 public static DrivingSettingsPanel For(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();return panel?panel:session.gameObject.AddComponent<DrivingSettingsPanel>();}
 public void Open(){Draft=(PreferenceKey==DrivingSettings.Key?DrivingSettings.Current:DrivingSettings.Load(PreferenceKey)).Copy();IsOpen=true;}
 public void Cancel(){IsOpen=false;Draft=null;}
 public void ResetDefaults(){if(IsOpen)Draft=new DrivingSettings();}
 public void Apply(){if(!IsOpen)return;if(PreferenceKey==DrivingSettings.Key)DrivingSettings.Commit(Draft);else Draft.Save(PreferenceKey);Cancel();}
 public void ValidateOwner(){var owner=GetComponent<DriveSession>();if(IsOpen&&owner&&owner.simulationMatch&&owner.simulationMatch.Finished)Cancel();}
 void Update(){ValidateOwner();}
 public static bool Back(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();if(!panel||!panel.IsOpen)return false;panel.Cancel();return true;}
 public static Rect Bounds(float width,float height){float scale=Mathf.Min(1,Mathf.Min((width-24)/620,(height-24)/520));return new Rect((width-620*scale)/2,(height-520*scale)/2,620*scale,520*scale);}
 GUIStyle Label(int size,bool bold=false){var style=new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=TextAnchor.MiddleLeft};style.normal.textColor=CasualUI.Ink;return style;}
 float Row(float y,string title,string hint,float value,float min,float max){
  GUI.Label(new Rect(28,y,400,28),title,Label(19,true));
  var valueStyle=Label(20,true);valueStyle.alignment=TextAnchor.MiddleRight;
  GUI.Label(new Rect(474,y,116,28),Mathf.RoundToInt(value*100)+"%",valueStyle);
  GUI.Label(new Rect(28,y+29,562,24),hint,Label(14));
  return Mathf.Round(GUI.HorizontalSlider(new Rect(30,y+61,558,20),value,min,max)*100)/100;
 }
 public void Draw(){
  ValidateOwner();if(!IsOpen)return;GUI.depth=-700;KoreanText.PrepareSkin();var old=GUI.matrix;var bounds=Bounds(Screen.width,Screen.height);float scale=bounds.width/620;
  GUI.matrix=Matrix4x4.TRS(new Vector3(bounds.x,bounds.y,0),Quaternion.identity,new Vector3(scale,scale,1));
  CasualUI.Panel(new Rect(0,0,620,520));
  GUI.Label(new Rect(28,16,540,40),"운전 설정",Label(27,true));
  GUI.Label(new Rect(28,57,562,24),"내 조작에 맞게 조절하고 적용하세요",Label(14));
  Draft.sensitivity=Row(91,"조향 민감도","같은 좌우 입력으로 꺾이는 회전량",Draft.sensitivity,.5f,1.5f);
  Draft.response=Row(176,"조향 반응 속도","누르거나 반대 방향으로 바꿀 때의 반응",Draft.response,.25f,2);
  Draft.centering=Row(261,"핸들 복원 속도","조향 입력을 놓았을 때 중앙으로 돌아오는 속도",Draft.centering,.25f,2);
  Draft.deadzone=Row(346,"게임패드 데드존","스틱의 작은 흔들림 무시 · 키보드에는 영향 없음",Draft.deadzone,0,.4f);
  if(GUI.Button(new Rect(28,452,164,44),"기본값",KoreanText.Button(17)))ResetDefaults();
  if(GUI.Button(new Rect(208,452,164,44),"취소",KoreanText.Button(17)))Cancel();
  if(GUI.Button(new Rect(388,452,204,44),"적용",CasualUI.Action(18)))Apply();
  GUI.matrix=old;
 }
}

using UnityEngine;

// The pause menu owns this panel; editing never changes live preferences.
public sealed class DrivingSettingsPanel : MonoBehaviour {
 public bool IsOpen {get;private set;}
 public DrivingSettings Draft {get;private set;}
<<<<<<< HEAD
 public RainSettings WeatherDraft {get;private set;} RainSettings openedWeather;public bool WeatherTab;public string Error {get;private set;}
 public string PreferenceKey=DrivingSettings.Key;
 public static DrivingSettingsPanel For(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();return panel?panel:session.gameObject.AddComponent<DrivingSettingsPanel>();}
 public void Open(){Draft=(PreferenceKey==DrivingSettings.Key?DrivingSettings.Current:DrivingSettings.Load(PreferenceKey)).Copy();WeatherDraft=RainSettings.Capture();openedWeather=WeatherDraft.Copy();Error="";WeatherTab=false;IsOpen=true;}
 public void Cancel(){IsOpen=false;Draft=null;WeatherDraft=null;openedWeather=null;}
 public void ResetDefaults(){if(IsOpen){if(WeatherTab){if(RainSettingsRuntime.CanEdit(GetComponent<DriveSession>()))WeatherDraft=new RainSettings();}else Draft=new DrivingSettings();}}
 public void Apply(){if(!IsOpen)return;if(!WeatherDraft.Same(openedWeather)){string error;if(!RainSettingsRuntime.Apply(GetComponent<DriveSession>(),WeatherDraft,out error)){Error=error;return;}}if(PreferenceKey==DrivingSettings.Key)DrivingSettings.Commit(Draft);else Draft.Save(PreferenceKey);Cancel();}
=======
 public bool FollowingDraft {get;private set;}
 public string PreferenceKey=DrivingSettings.Key;
 public static DrivingSettingsPanel For(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();return panel?panel:session.gameObject.AddComponent<DrivingSettingsPanel>();}
 public void Open(){Draft=(PreferenceKey==DrivingSettings.Key?DrivingSettings.Current:DrivingSettings.Load(PreferenceKey)).Copy();var owner=GetComponent<DriveSession>();FollowingDraft=owner&&owner.traffic&&owner.traffic.mitOptions.Enabled;IsOpen=true;}
 public void Cancel(){IsOpen=false;Draft=null;}
 public void ResetDefaults(){if(IsOpen){Draft=new DrivingSettings();FollowingDraft=true;}}
 public void Apply(){if(!IsOpen)return;if(PreferenceKey==DrivingSettings.Key)DrivingSettings.Commit(Draft);else Draft.Save(PreferenceKey);var owner=GetComponent<DriveSession>();if(owner&&owner.traffic&&!owner.simulationMatch)owner.traffic.mitOptions.SetEnabled(FollowingDraft);Cancel();}
>>>>>>> origin/main
 public void ValidateOwner(){var owner=GetComponent<DriveSession>();if(IsOpen&&owner&&owner.simulationMatch&&owner.simulationMatch.Finished)Cancel();}
 void Update(){ValidateOwner();}
 public static bool Back(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();if(!panel||!panel.IsOpen)return false;panel.Cancel();return true;}
 public static Rect Bounds(float width,float height){float scale=Mathf.Min(1,Mathf.Min((width-24)/620,(height-24)/670));return new Rect((width-620*scale)/2,(height-670*scale)/2,620*scale,670*scale);}
 GUIStyle Label(int size,bool bold=false){var style=new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=TextAnchor.MiddleLeft};style.normal.textColor=CasualUI.Ink;return style;}
 float Row(float y,string title,string hint,float value,float min,float max){
  CasualUI.RoundedFill(new Rect(18,y-3,584,81),CasualUI.Soft);
  GUI.Label(new Rect(28,y,400,28),title,Label(19,true));
  var valueStyle=Label(20,true);valueStyle.alignment=TextAnchor.MiddleRight;
  GUI.Label(new Rect(474,y,116,28),Mathf.RoundToInt(value*100)+"%",valueStyle);
  GUI.Label(new Rect(28,y+29,562,24),hint,Label(14));
  return Mathf.Round(GUI.HorizontalSlider(new Rect(30,y+61,558,20),value,min,max)*100)/100;
 }
 float WeatherRow(float y,string title,float value,float min,float max,string suffix){
  GUI.Label(new Rect(28,y,390,27),title,Label(17,true));var style=Label(18,true);style.alignment=TextAnchor.MiddleRight;GUI.Label(new Rect(440,y,150,27),value.ToString(suffix=="점"?"0":"0.##")+suffix,style);return Mathf.Round(GUI.HorizontalSlider(new Rect(30,y+35,558,20),value,min,max)*100)/100;
 }
 public void Draw(){
  ValidateOwner();if(!IsOpen)return;GUI.depth=-700;KoreanText.PrepareSkin();var old=GUI.matrix;var bounds=Bounds(Screen.width,Screen.height);float scale=bounds.width/620;
  GUI.matrix=Matrix4x4.TRS(new Vector3(bounds.x,bounds.y,0),Quaternion.identity,new Vector3(scale,scale,1));
<<<<<<< HEAD
  CasualUI.Panel(new Rect(0,0,620,605));
  CasualUI.Header(new Rect(18,10,584,38),"설정",13);
  if(GUI.Button(new Rect(28,54,274,36),"운전 관련 설정",WeatherTab?KoreanText.Button(17):CasualUI.Action(17)))WeatherTab=false;
  if(GUI.Button(new Rect(310,54,282,36),"날씨 관련 설정",WeatherTab?CasualUI.Action(17):KoreanText.Button(17)))WeatherTab=true;
  GUI.Label(new Rect(28,94,562,23),WeatherTab?"적용하면 현재 운행에 반영 · 확률 미끄러짐은 잠금":"조절 후 적용 · 취소하면 현재 설정을 유지해요",Label(14));
  if(!WeatherTab){
   Draft.sensitivity=Row(118,"조향 민감도","같은 좌우 입력으로 꺾이는 회전량",Draft.sensitivity,.5f,1.5f);
   Draft.response=Row(202,"조향 반응 속도","누르거나 반대 방향으로 바꿀 때의 반응",Draft.response,.25f,2);
   Draft.centering=Row(286,"핸들 복원 속도","조향 입력을 놓았을 때 중앙으로 돌아오는 속도",Draft.centering,.25f,2);
   Draft.braking=Row(370,"브레이크 감속 민감도","S/↓/패드 일반 감속의 세기 · 긴급제동은 그대로",Draft.braking,.5f,1.5f);
   Draft.deadzone=Row(454,"게임패드 데드존","스틱의 작은 흔들림 무시 · 키보드에는 영향 없음",Draft.deadzone,0,.4f);
  }else{
   bool editable=RainSettingsRuntime.CanEdit(GetComponent<DriveSession>()),wasEnabled=GUI.enabled;if(!editable){WeatherDraft=RainSettings.Capture();openedWeather=WeatherDraft.Copy();}GUI.enabled=wasEnabled&&editable;
   WeatherDraft.enabled=GUI.Toggle(new Rect(30,122,560,32),WeatherDraft.enabled,WeatherDraft.enabled?"비 켜짐 · 자동 와이퍼":"비 꺼짐 · 맑음",KoreanText.Button(18));
   WeatherDraft.intensity=WeatherRow(159,"빗줄기 강도",WeatherDraft.intensity,0,1,"배");
   WeatherDraft.wiperSeconds=WeatherRow(225,"자동 와이퍼 주기",WeatherDraft.wiperSeconds,.5f,3,"초");
   WeatherDraft.boardingDirt=Mathf.RoundToInt(WeatherRow(291,"승차 추가 청결 감소",WeatherDraft.boardingDirt,0,10,"점"));
   WeatherDraft.brakeMultiplier=WeatherRow(357,"비 제동 계수 · 낮을수록 길게 밀림",WeatherDraft.brakeMultiplier,.75f,1,"배");
   WeatherDraft.trafficSpeedMultiplier=WeatherRow(423,"일반 차량 속도 계수",WeatherDraft.trafficSpeedMultiplier,.6f,1,"배");
   GUI.enabled=wasEnabled;
   var note=Label(14);note.wordWrap=true;GUI.Label(new Rect(28,490,562,45),editable?"현재 승객 오염은 소급하지 않아요. 이어하기에는 저장되며, 툴의 원본 프로필은 바꾸지 않아요.":"LAN 날씨는 호스트가 변경합니다. 현재 공유된 값만 확인할 수 있어요.",note);
  }
  if(!string.IsNullOrEmpty(Error))GUI.Label(new Rect(28,529,562,22),Error,Label(13));
  if(GUI.Button(new Rect(28,552,164,40),"기본값",KoreanText.Button(17)))ResetDefaults();
  if(GUI.Button(new Rect(208,552,164,40),"취소",KoreanText.Button(17)))Cancel();
  if(GUI.Button(new Rect(388,552,204,40),"적용",CasualUI.Action(18)))Apply();
=======
  CasualUI.Panel(new Rect(0,0,620,670));
  CasualUI.Header(new Rect(18,14,584,48),"운전 설정 · 내 손에 맞게",13);
  GUI.Label(new Rect(28,65,562,24),"조절 후 적용 · 취소하면 현재 설정을 유지해요",Label(14));
  Draft.sensitivity=Row(91,"조향 민감도","같은 좌우 입력으로 꺾이는 회전량",Draft.sensitivity,.5f,1.5f);
  Draft.response=Row(176,"조향 반응 속도","누르거나 반대 방향으로 바꿀 때의 반응",Draft.response,.25f,2);
  Draft.centering=Row(261,"핸들 복원 속도","조향 입력을 놓았을 때 중앙으로 돌아오는 속도",Draft.centering,.25f,2);
  Draft.braking=Row(346,"브레이크 감속 민감도","S/↓/패드 일반 감속의 세기 · 긴급제동은 그대로",Draft.braking,.5f,1.5f);
  Draft.deadzone=Row(431,"게임패드 데드존","스틱의 작은 흔들림 무시 · 키보드에는 영향 없음",Draft.deadzone,0,.4f);
  var owner=GetComponent<DriveSession>();bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&owner&&!owner.simulationMatch;
  FollowingDraft=GUI.Toggle(new Rect(28,522,562,30),FollowingDraft,"앞차 안전 추종 보완 · "+(FollowingDraft?"켜짐":"꺼짐"),KoreanText.Button(17));GUI.enabled=oldEnabled;
  GUI.Label(new Rect(28,554,562,24),owner&&owner.simulationMatch?"PVP에서는 공통 안전 규칙을 유지합니다":"주변 차량이 앞차와 안전거리를 유지하도록 보완합니다",Label(14));
  if(GUI.Button(new Rect(28,602,164,44),"기본값",KoreanText.Button(17)))ResetDefaults();
  if(GUI.Button(new Rect(208,602,164,44),"취소",KoreanText.Button(17)))Cancel();
  if(GUI.Button(new Rect(388,602,204,44),"적용",CasualUI.Action(18)))Apply();
>>>>>>> origin/main
  GUI.matrix=old;
 }
}

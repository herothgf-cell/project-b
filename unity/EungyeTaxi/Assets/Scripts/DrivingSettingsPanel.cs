using UnityEngine;

// The pause menu owns this panel; editing never changes live preferences.
public sealed class DrivingSettingsPanel : MonoBehaviour {
 public bool IsOpen {get;private set;}
 public DrivingSettings Draft {get;private set;}
 public bool FollowingDraft {get;private set;}
 public RainSettings WeatherDraft {get;private set;} RainSettings openedWeather;public bool WeatherTab,DisplayTab,DevTab,SoundTab,FeelTab;public string Error {get;private set;}
 public string PreferenceKey=DrivingSettings.Key;
 public static DrivingSettingsPanel For(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();return panel?panel:session.gameObject.AddComponent<DrivingSettingsPanel>();}
 public void Open(){Draft=(PreferenceKey==DrivingSettings.Key?DrivingSettings.Current:DrivingSettings.Load(PreferenceKey)).Copy();var owner=GetComponent<DriveSession>();FollowingDraft=owner&&owner.traffic&&owner.traffic.mitOptions.Enabled;WeatherDraft=RainSettings.Capture();openedWeather=WeatherDraft.Copy();Error="";FeelTab=SoundTab=WeatherTab=DisplayTab=DevTab=false;IsOpen=true;}
 public DevRoadOverview RoadOverview {get{return GetComponent<DevRoadOverview>();}}
 public void OpenRoadOverview(){var owner=GetComponent<DriveSession>();if(!IsOpen||!owner||!owner.world)return;var view=RoadOverview;if(!view)view=gameObject.AddComponent<DevRoadOverview>();view.Open(owner);}
 public void Cancel(){if(RoadOverview)RoadOverview.Close();IsOpen=false;Draft=null;WeatherDraft=null;openedWeather=null;}
 public void SteeringPreset(int preset){if(!IsOpen||Draft==null)return;Draft.sensitivity=preset<0?.9f:preset>0?1.15f:1;Draft.response=preset<0?.8f:preset>0?1.35f:1;Draft.centering=preset<0?1.1f:preset>0?1.25f:1;}
 public void ResetDefaults(){
  if(!IsOpen||DevTab)return;
  if(SoundTab){
   Draft.engineSound=Draft.indicatorSound=Draft.hornSound=Draft.rainSound=true;
  }else if(FeelTab){
   Draft.steeringFeel=DrivingSettings.Casual;Draft.speedFov=Draft.steerLook=Draft.brakeDip=Draft.bodyLean=Draft.extraSound=Draft.rumble=1;
  }else if(WeatherTab){
   if(RainSettingsRuntime.CanEdit(GetComponent<DriveSession>()))WeatherDraft=new RainSettings();
  }else if(DisplayTab){
   Draft.radioSubtitles=DrivingSettings.DefaultRadioSubtitles;
   Draft.pvpNightLighting=DrivingSettings.DefaultPvpNightLighting;
  }else{
   Draft=new DrivingSettings{engineSound=Draft.engineSound,indicatorSound=Draft.indicatorSound,hornSound=Draft.hornSound,rainSound=Draft.rainSound,radioSubtitles=Draft.radioSubtitles,pvpNightLighting=Draft.pvpNightLighting,
    steeringFeel=Draft.steeringFeel,speedFov=Draft.speedFov,steerLook=Draft.steerLook,brakeDip=Draft.brakeDip,bodyLean=Draft.bodyLean,extraSound=Draft.extraSound,rumble=Draft.rumble};
   FollowingDraft=true;
  }
 }
 public void Apply(){if(!IsOpen)return;if(!WeatherDraft.Same(openedWeather)){string error;if(!RainSettingsRuntime.Apply(GetComponent<DriveSession>(),WeatherDraft,out error)){Error=error;return;}}if(PreferenceKey==DrivingSettings.Key)DrivingSettings.Commit(Draft);else Draft.Save(PreferenceKey);var owner=GetComponent<DriveSession>();if(owner&&owner.traffic&&!owner.simulationMatch)owner.traffic.mitOptions.SetEnabled(FollowingDraft);Cancel();}
 public void ValidateOwner(){var owner=GetComponent<DriveSession>();if(IsOpen&&owner&&owner.simulationMatch&&owner.simulationMatch.Finished)Cancel();}
 void Update(){ValidateOwner();}
 public static bool Back(DriveSession session){var panel=session.GetComponent<DrivingSettingsPanel>();if(!panel||!panel.IsOpen)return false;if(panel.RoadOverview&&panel.RoadOverview.IsOpen)panel.RoadOverview.Back();else panel.Cancel();return true;}
 public static Rect Bounds(float width,float height){float scale=Mathf.Min(1,Mathf.Min((width-24)/620,(height-24)/700));return new Rect((width-620*scale)/2,(height-700*scale)/2,620*scale,700*scale);}
 GUIStyle Label(int size,bool bold=false){var style=new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=TextAnchor.MiddleLeft};style.normal.textColor=CasualUI.Ink;return style;}
 float Row(float y,string title,string hint,float value,float min,float max){
  CasualUI.RoundedFill(new Rect(18,y-3,584,81),CasualUI.Soft);
  GUI.Label(new Rect(28,y,400,28),title,Label(19,true));
  var valueStyle=Label(20,true);valueStyle.alignment=TextAnchor.MiddleRight;
  GUI.Label(new Rect(474,y,116,28),Mathf.RoundToInt(value*100)+"%",valueStyle);
  GUI.Label(new Rect(28,y+29,562,24),hint,Label(14));
  return Mathf.Round(CasualUI.Slider(new Rect(40,y+61,538,20),value,min,max)*100)/100;
 }
 float WeatherRow(float y,string title,float value,float min,float max,string suffix){
  GUI.Label(new Rect(28,y,390,27),title,Label(17,true));var style=Label(18,true);style.alignment=TextAnchor.MiddleRight;GUI.Label(new Rect(440,y,150,27),value.ToString(suffix=="점"?"0":"0.##")+suffix,style);return Mathf.Round(CasualUI.Slider(new Rect(40,y+35,538,20),value,min,max)*100)/100;
 }
 bool DisplayRow(float y,string title,string hint,bool enabled){
  CasualUI.RoundedFill(new Rect(18,y,584,94),CasualUI.Soft);
  GUI.Label(new Rect(30,y+10,409,31),title,Label(19,true));
  var hintStyle=Label(14);hintStyle.wordWrap=true;
  GUI.Label(new Rect(30,y+43,409,42),hint,hintStyle);
  if(GUI.Button(new Rect(452,y+27,138,40),enabled?"켜짐":"꺼짐",enabled?CasualUI.Action(17):KoreanText.Button(17)))enabled=!enabled;
  return enabled;
 }
 public void SetSteeringFeel(int feel){if(IsOpen&&Draft!=null&&(feel==DrivingSettings.Casual||feel==DrivingSettings.Sim))Draft.steeringFeel=feel;}
 // Two-line slider row (title and value, then the slider) so the feel tab fits six strengths.
 float CompactRow(float y,string title,float value){
  CasualUI.RoundedFill(new Rect(18,y-2,584,56),CasualUI.Soft);
  GUI.Label(new Rect(28,y,440,24),title,Label(16,true));
  var valueStyle=Label(17,true);valueStyle.alignment=TextAnchor.MiddleRight;GUI.Label(new Rect(474,y,116,24),Mathf.RoundToInt(value*100)+"%",valueStyle);
  return Mathf.Round(CasualUI.Slider(new Rect(40,y+30,538,20),value,0,1)*100)/100;
 }
 void DrawFeelTab(){
  CasualUI.RoundedFill(new Rect(18,121,584,96),CasualUI.Soft);
  GUI.Label(new Rect(30,127,560,28),"운전 감각",Label(19,true));
  var hint=Label(14);hint.wordWrap=true;
  GUI.Label(new Rect(30,155,300,58),Draft.CasualSteering?"캐주얼 · 40~70km/h 차선 변경이 빠르고 80km/h까지 회전을 도와요":"시뮬 · 기존 r50 조향 그대로, 고속일수록 둔해요",hint);
  if(GUI.Button(new Rect(338,150,122,42),"캐주얼",Draft.CasualSteering?CasualUI.Action(17):KoreanText.Button(17)))SetSteeringFeel(DrivingSettings.Casual);
  if(GUI.Button(new Rect(468,150,122,42),"시뮬",!Draft.CasualSteering?CasualUI.Action(17):KoreanText.Button(17)))SetSteeringFeel(DrivingSettings.Sim);
  Draft.speedFov=CompactRow(224,"속도감 시야 · 빠를수록 넓게",Draft.speedFov);
  Draft.steerLook=CompactRow(284,"조향 방향 미리 보기 · V로 켜고 끄기",Draft.steerLook);
  Draft.brakeDip=CompactRow(344,"제동 쏠림 · 세게 감속하면 숙여짐",Draft.brakeDip);
  Draft.bodyLean=CompactRow(404,"차체 기울기 · 물리·승차감 판정은 그대로",Draft.bodyLean);
  Draft.extraSound=CompactRow(464,"추가 주행음 · 가속 부하·급제동음(싱글)",Draft.extraSound);
  Draft.rumble=CompactRow(524,"패드 진동 · 노면·제동·접촉(싱글, Xbox 계열)",Draft.rumble);
  GUI.Label(new Rect(28,588,562,40),"PvP는 시뮬 조향 고정 · 연출·소리·진동은 이 기기에만 적용 · 0%는 끔",Label(14));
 }
 void DrawDisplayTab(){
  Draft.radioSubtitles=DisplayRow(124,"은계 FM 라디오 자막","은계 싱글 영업 중 화면 하단에 동네 소식과 교통 정보를 표시합니다.",Draft.radioSubtitles);
  Draft.pvpNightLighting=DisplayRow(234,"PvP 야간 조명","PvP에서 시간대에 따라 도시 조명과 밤 풍경을 표시합니다.",Draft.pvpNightLighting);
 }
 void DrawDevTab(){
  var owner=GetComponent<DriveSession>();var b=owner?owner.business:null;bool single=b&&!b.SimulationSession&&!owner.simulationMatch;
  CasualUI.RoundedFill(new Rect(18,124,584,150),CasualUI.Soft);
  GUI.Label(new Rect(28,130,562,28),"바로 퇴근",Label(19,true));
  var note=Label(14);note.wordWrap=true;
  GUI.Label(new Rect(28,160,562,44),"거점 정차·휴식 조건 없이 오늘 영업을 즉시 마치고 정산 화면으로 갑니다. 손님을 태우러 가는 중이면 호출을 취소합니다(태운 승객이 있으면 사용 불가).",note);
  bool old=GUI.enabled;GUI.enabled=old&&single;
  if(GUI.Button(new Rect(28,214,562,44),"지금 바로 퇴근 (개발용)",CasualUI.Action(18))){if(b.DevEndDayNow())Cancel();else Error=b.notice;}
  GUI.enabled=old;
  CasualUI.RoundedFill(new Rect(18,286,584,170),CasualUI.Soft);
  GUI.Label(new Rect(28,292,562,28),"영업 시간 (테스트)",Label(19,true));
  GUI.Label(new Rect(28,322,562,44),"영업 중에도 오늘 영업 시간을 바로 바꿉니다. 이미 지난 시간보다 짧게 하면 즉시 초과 영업(피로 100)이 됩니다. 다음 날에도 유지됩니다.",note);
  bool canShift=single&&b.CanDevChangeShiftLength;GUI.enabled=old&&canShift;
  var choices=PrototypeSettings.ShiftMinuteChoices;int current=b?b.ShiftMinutes:0;float w=(562-6*(choices.Length-1))/(float)choices.Length;
  for(int i=0;i<choices.Length;i++){int m=choices[i];if(GUI.Button(new Rect(28+i*(w+6),376,w,40),m+"분",m==current?CasualUI.Action(17):KoreanText.Button(17))&&m!=current&&!b.DevSetShiftLength(m))Error=b.notice;}
  GUI.enabled=old;
  if(b&&canShift)GUI.Label(new Rect(28,422,562,24),"현재 "+current+"분 · 남은 영업 "+Mathf.CeilToInt((float)b.ledger.SecondsLeft/60f)+"분",Label(14));
  if(!single)GUI.Label(new Rect(28,470,562,24),"PVP에서는 사용할 수 없습니다.",Label(14));
  CasualUI.RoundedFill(new Rect(18,502,584,122),CasualUI.Soft);
  GUI.Label(new Rect(28,508,562,28),"도로 전체 보기",Label(19,true));
  GUI.Label(new Rect(28,537,562,24),"실제 차선·인도·도로 폭을 위에서 확인 · 드래그와 휠로 이동",Label(14));
  if(GUI.Button(new Rect(28,572,562,40),"도로 전체 보기 열기 (개발용)",KoreanText.Button(18)))OpenRoadOverview();
 }
 public void Draw(){
  ValidateOwner();if(!IsOpen)return;GUI.depth=-700;KoreanText.PrepareSkin();if(RoadOverview&&RoadOverview.IsOpen){RoadOverview.Draw();return;}var old=GUI.matrix;var bounds=Bounds(Screen.width,Screen.height);float scale=bounds.width/620;
  GUI.matrix=Matrix4x4.TRS(new Vector3(bounds.x,bounds.y,0),Quaternion.identity,new Vector3(scale,scale,1));
  CasualUI.Panel(new Rect(0,0,620,700));
  CasualUI.Header(new Rect(18,10,584,38),"설정",13);
  bool driveTab=!WeatherTab&&!DisplayTab&&!DevTab&&!SoundTab&&!FeelTab;
  if(GUI.Button(new Rect(28,54,89,36),"운전",driveTab?CasualUI.Action(16):KoreanText.Button(16)))WeatherTab=DisplayTab=DevTab=SoundTab=FeelTab=false;
  if(GUI.Button(new Rect(122,54,89,36),"감각",FeelTab?CasualUI.Action(16):KoreanText.Button(16))){FeelTab=true;WeatherTab=DisplayTab=DevTab=SoundTab=false;}
  if(GUI.Button(new Rect(216,54,89,36),"날씨",WeatherTab?CasualUI.Action(16):KoreanText.Button(16))){WeatherTab=true;DisplayTab=DevTab=SoundTab=FeelTab=false;}
  if(GUI.Button(new Rect(310,54,89,36),"표시",DisplayTab?CasualUI.Action(16):KoreanText.Button(16))){DisplayTab=true;WeatherTab=DevTab=SoundTab=FeelTab=false;}
  if(GUI.Button(new Rect(404,54,89,36),"소리",SoundTab?CasualUI.Action(16):KoreanText.Button(16))){SoundTab=true;WeatherTab=DisplayTab=DevTab=FeelTab=false;}
  if(GUI.Button(new Rect(498,54,92,36),"개발용",DevTab?CasualUI.Action(16):KoreanText.Button(16))){DevTab=true;WeatherTab=DisplayTab=SoundTab=FeelTab=false;}
  GUI.Label(new Rect(28,94,562,23),FeelTab?"운전 감각은 싱글 영업에 적용 · 연출은 0%로 끌 수 있어요":SoundTab?"이 기기의 효과음 · 적용을 누르면 저장됩니다":DevTab?"개발·테스트 전용 · 정식 플레이 규칙을 건너뜁니다":DisplayTab?"이 기기에만 적용되는 화면 표시 설정":WeatherTab?"적용하면 현재 운행에 반영 · 확률 미끄러짐은 잠금":"조절 후 적용 · 취소하면 현재 설정을 유지해요",Label(14));
  if(SoundTab){
   Draft.engineSound=DisplayRow(124,"주행음","가속하면 높아지고 속도가 줄면 낮아지는 엔진 소리",Draft.engineSound);
   Draft.indicatorSound=DisplayRow(232,"깜빡이","방향지시등·비상등의 점멸에 맞춘 딸깍 소리",Draft.indicatorSound);
   Draft.hornSound=DisplayRow(340,"경적","Y 또는 게임패드 버튼 3을 눌러 빵빵",Draft.hornSound);
   Draft.rainSound=DisplayRow(448,"빗소리","비의 강도에 따른 차 지붕의 빗방울 소리",Draft.rainSound);
  }else if(FeelTab)DrawFeelTab();
  else if(DevTab)DrawDevTab();
  else if(DisplayTab)DrawDisplayTab();
  else if(!WeatherTab){
   Draft.sensitivity=Row(118,"조향 민감도 · 회전량","50~150% · 높이면 더 꺾여요. 좁은 코너는 감속하세요",Draft.sensitivity,.5f,1.5f);
   Draft.response=Row(202,"조향 반응 속도","누르거나 반대 방향으로 바꿀 때의 반응",Draft.response,.25f,2);
   Draft.centering=Row(286,"핸들 복원 속도","조향 입력을 놓았을 때 중앙으로 돌아오는 속도",Draft.centering,.25f,2);
   Draft.braking=Row(370,"브레이크 감속 민감도","S/↓/패드 일반 감속의 세기 · 긴급제동은 그대로",Draft.braking,.5f,1.5f);
   Draft.deadzone=Row(454,"게임패드 데드존","스틱의 작은 흔들림 무시 · 키보드에는 영향 없음",Draft.deadzone,0,.4f);
   var owner=GetComponent<DriveSession>();bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&owner&&!owner.simulationMatch;
   FollowingDraft=GUI.Toggle(new Rect(28,540,562,30),FollowingDraft,"앞차 안전 추종 보완 · "+(FollowingDraft?"켜짐":"꺼짐"),KoreanText.Button(17));GUI.enabled=oldEnabled;
   GUI.enabled=oldEnabled&&owner&&!owner.simulationMatch;
   Draft.cabinAssist=GUI.Toggle(new Rect(28,571,562,28),Draft.cabinAssist,"차내 조작 자동 보조 · 첫 3일",KoreanText.Button(15));GUI.enabled=oldEnabled;
   if(GUI.Button(new Rect(28,604,180,32),"입력: 부드럽게",KoreanText.Button(15)))SteeringPreset(-1);
   if(GUI.Button(new Rect(218,604,180,32),"입력: 표준",KoreanText.Button(15)))SteeringPreset(0);
   if(GUI.Button(new Rect(408,604,180,32),"입력: 빠르게",KoreanText.Button(15)))SteeringPreset(1);
  }else{
   bool editable=RainSettingsRuntime.CanEdit(GetComponent<DriveSession>()),wasEnabled=GUI.enabled;if(!editable){WeatherDraft=RainSettings.Capture();openedWeather=WeatherDraft.Copy();}GUI.enabled=wasEnabled&&editable;
   var b=GetComponent<DriveSession>().business;
   if(b&&b.DailyConditionsEnabled)GUI.Label(new Rect(30,122,560,32),"하루 조건 · "+DailyConditions.Title(b.DayCondition),Label(17));
   else WeatherDraft.enabled=GUI.Toggle(new Rect(30,122,560,32),WeatherDraft.enabled,WeatherDraft.enabled?"비 켜짐 · 자동 와이퍼":"비 꺼짐 · 맑음",KoreanText.Button(18));
   WeatherDraft.intensity=WeatherRow(159,"빗줄기 강도",WeatherDraft.intensity,0,1,"배");
   WeatherDraft.wiperSeconds=WeatherRow(225,"자동 와이퍼 주기",WeatherDraft.wiperSeconds,.5f,3,"초");
   WeatherDraft.boardingDirt=Mathf.RoundToInt(WeatherRow(291,"승차 추가 청결 감소",WeatherDraft.boardingDirt,0,10,"점"));
   WeatherDraft.brakeMultiplier=WeatherRow(357,"비 제동 계수 · 낮을수록 길게 밀림",WeatherDraft.brakeMultiplier,.75f,1,"배");
   WeatherDraft.trafficSpeedMultiplier=WeatherRow(423,"일반 차량 속도 계수",WeatherDraft.trafficSpeedMultiplier,.6f,1,"배");
   GUI.enabled=wasEnabled;
   var note=Label(14);note.wordWrap=true;GUI.Label(new Rect(28,490,562,45),editable?"현재 승객 오염은 소급하지 않아요. 이어하기에는 저장되며, 툴의 원본 프로필은 바꾸지 않아요.":"LAN 날씨는 호스트가 변경합니다. 현재 공유된 값만 확인할 수 있어요.",note);
  }
  if(!string.IsNullOrEmpty(Error))GUI.Label(new Rect(28,636,562,14),Error,Label(12));
  if(GUI.Button(new Rect(28,650,164,40),"기본값",KoreanText.Button(17)))ResetDefaults();
  if(GUI.Button(new Rect(208,650,164,40),"취소",KoreanText.Button(17)))Cancel();
  if(GUI.Button(new Rect(388,650,204,40),"적용",CasualUI.Action(18)))Apply();
  GUI.matrix=old;
 }
}

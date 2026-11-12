using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace Pvp {
 public sealed class PvpPrototypePanel : MonoBehaviour {
  // Default match length 15 minutes (900s); still editable 30~7200.
  string duration="900",message="경기 시간은 기본 15분(900초)입니다. 30~7200초로 바꿀 수 있습니다.";
  string networkAddress="127.0.0.1",networkPort="7777";
  string nickname="";void Awake(){nickname=DriveFlowPolicy.EnsureName();}
  bool NameReady(){if(DriveFlowPolicy.SaveName(nickname)){nickname=DriveFlowPolicy.LocalName;return true;}message="닉네임을 입력하세요 (한글/영문/숫자, 최대12자).";return false;}
  bool legacy,showControls,showLan;Vector2 scroll;
  PvpRelayClient relay;
  PvpRelayRoom[] rooms=Array.Empty<PvpRelayRoom>();
  bool showRooms,loadingRooms;
  string relayNotice="";
  PvpConditionSettings condition=new PvpConditionSettings();
  public bool ConditionSettingsOpen {get;set;}
  void OnGUI(){
   GUI.skin.font=KoreanText.Font;
   if(PvpNativeLan.Instance){DrawNativeLobby();return;}
   CasualUI.Backdrop();float width=Mathf.Min(760,Screen.width-32),height=Mathf.Min(680,Screen.height-32),x=(Screen.width-width)/2,y=(Screen.height-height)/2;
   CasualUI.Panel(new Rect(x,y,width,height));CasualUI.Header(new Rect(x+8,y+8,width-16,52),"PVP · 함께 달리는 택시",1);
   GUILayout.BeginArea(new Rect(x+18,y+76,width-36,height-92));scroll=GUILayout.BeginScrollView(scroll);
   GUILayout.Label("닉네임",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=20});nickname=GUILayout.TextField(nickname,12);GUILayout.Label("자동으로 정해진 이름입니다. 바꾸면 경기 시작 때 저장됩니다 · 한글·영문·숫자, 최대 12자");GUILayout.Space(12);
   GUILayout.BeginHorizontal();GUILayout.Label("경기 시간(초, 기본 900 = 15분)");duration=GUILayout.TextField(duration,5);GUILayout.EndHorizontal();
   DrawOnlineRooms();
   DrawMyCar();
   DrawConditionSettings();
   var boot=GetComponent<PvpBootstrap>();
   bool actionsEnabled=GUI.enabled;
   GUI.enabled=actionsEnabled&&!boot.RelayConnecting;
   GUILayout.BeginVertical(GUI.skin.box);
   GUILayout.Label("나 + 봇 3대 · 공용 호출 8개 · 오프라인");
   GUILayout.Label("전용 도시 · 다차로·골목·신호·유턴");
   if(GUILayout.Button("봇 3대와 경기 시작",CasualUI.Action(18),GUILayout.Height(48))){int seconds;if(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)message="30~7200초로 입력하세요.";else if(NameReady()){PvpConditionRules.Configure(condition);GetComponent<PvpBootstrap>().StartSimulation(true,seconds);}}
   GUILayout.EndVertical();GUILayout.Space(10);
   showLan=GUILayout.Toggle(showLan,"LAN 직접 접속");
   if(showLan){
    GUILayout.BeginVertical(GUI.skin.box);
    GUILayout.BeginHorizontal();GUILayout.Label("호스트 IP",GUILayout.Width(75));networkAddress=GUILayout.TextField(networkAddress,45);GUILayout.Label("포트",GUILayout.Width(40));networkPort=GUILayout.TextField(networkPort,5,GUILayout.Width(75));GUILayout.EndHorizontal();
    GUILayout.BeginHorizontal();if(GUILayout.Button("LAN 방 만들기",CasualUI.Action(18),GUILayout.Height(44)))Native(true);if(GUILayout.Button("IP로 참가",KoreanText.Button(18),GUILayout.Height(44)))Native(false);GUILayout.EndHorizontal();GUILayout.EndVertical();
   }
   GUILayout.Label(PvpNativeLan.LastNotice);
   if(!string.IsNullOrEmpty(boot.StartupError))GUILayout.Label(boot.StartupError);
   GUILayout.Label(message);GUILayout.Space(12);
   if(GUILayout.Button(showControls?"조작법 닫기":"조작법 보기",KoreanText.Button(16)))showControls=!showControls;
   if(showControls)GUILayout.Label("운전  W/↑ 가속 · S/↓ 감속·후진 · A/D/←→ 조향 · Space 제동\n호출  M 지도에서 선택·수락 → 승차 지점 정차 → F 승하차\n시점  C 전환 · B 누르는 동안 후방 · Z/X 좌우 확인\n보조  H 조향 보조 · Q/E 방향지시(보조 중 방향 요청) · G 비상등\n시설  정차 후 F7 · Esc 메뉴 (LAN 경기는 계속 진행)");
   GUILayout.Label("지도를 열어도 경기는 계속됩니다. 내 차는 멈춥니다.");
   legacy=GUILayout.Toggle(legacy,"이전 시험장 (개발용)");
   if(legacy){GUILayout.Label("이전 평면 모터 개발 검사입니다.");if(GUILayout.Button("이전 오프라인 시험장"))GetComponent<PvpBootstrap>().StartPractice();GUILayout.BeginHorizontal();networkAddress=GUILayout.TextField(networkAddress,45);networkPort=GUILayout.TextField(networkPort,5);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();if(GUILayout.Button("이전 IP 호스트"))Network(true);if(GUILayout.Button("이전 IP 참가"))Network(false);GUILayout.EndHorizontal();}
   GUI.enabled=actionsEnabled;
   var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified&&GUILayout.Button("모드 선택"))unified.ReturnToMenu();
   if(GUILayout.Button("나가기"))Application.Quit();GUILayout.EndScrollView();GUILayout.EndArea();
  }
  void DrawOnlineRooms(){
   var boot=GetComponent<PvpBootstrap>();
   GUILayout.BeginVertical(GUI.skin.box);
   GUILayout.Label("온라인 대전 · 최대 4명",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=20});
   bool enabled=GUI.enabled;
   GUI.enabled=enabled&&!boot.RelayConnecting&&!loadingRooms;
   GUILayout.BeginHorizontal();
   if(GUILayout.Button("방 만들기",CasualUI.Action(18),GUILayout.Height(44)))OpenOnline(null);
   if(GUILayout.Button(showRooms?"새로고침":"방 목록",KoreanText.Button(18),GUILayout.Height(44))){showRooms=true;StartCoroutine(RefreshRooms());}
   GUILayout.EndHorizontal();
   if(showRooms&&relay!=null){
    bool visible=false;
    foreach(var room in rooms){
     if(room==null||!room.Valid||room.started&&!PvpNativeLan.CanRejoinRelay(relay,room))continue;
     visible=true;
     GUILayout.BeginHorizontal();
     GUILayout.Label(room.name+"  ·  "+room.players+"/4  ·  "+room.seconds+"초");
     bool available=room.players<4&&room.Compatible;
     GUI.enabled=enabled&&!boot.RelayConnecting&&!loadingRooms&&available;
     string action=!room.Compatible?"실행본 확인":room.players==4?"가득 참":room.started?"재참가":"참가";
     if(GUILayout.Button(action,KoreanText.Button(16),GUILayout.Width(130)))OpenOnline(room);
     GUILayout.EndHorizontal();
    }
    if(!visible&&!loadingRooms&&string.IsNullOrEmpty(relayNotice))GUILayout.Label("참가할 수 있는 방이 없습니다.");
   }
   GUI.enabled=enabled;
   if(boot.RelayConnecting)GUILayout.Label("중계 서버에 연결 중…");
   else if(loadingRooms)GUILayout.Label("방 목록을 불러오는 중…");
   else if(!string.IsNullOrEmpty(relayNotice))GUILayout.Label(relayNotice);
   else if(!string.IsNullOrEmpty(boot.RelayError))GUILayout.Label(boot.RelayError);
   GUILayout.EndVertical();GUILayout.Space(12);
  }
  bool LoadRelay(){
   try{relay=PvpRelayClient.Load();relayNotice="";return true;}
   catch(Exception){relay=null;relayNotice="온라인 대전의 중계 주소와 접속 설정을 확인하세요.";return false;}
  }
  IEnumerator RefreshRooms(){
   GetComponent<PvpBootstrap>().ClearRelayError();
   if(!LoadRelay())yield break;
   loadingRooms=true;rooms=Array.Empty<PvpRelayRoom>();
   using(var request=relay.List()){
    yield return request.SendWebRequest();
    if(request.result==UnityWebRequest.Result.Success){
     try{rooms=JsonUtility.FromJson<PvpRelayRoomList>(request.downloadHandler.text)?.rooms??Array.Empty<PvpRelayRoom>();}
     catch(ArgumentException){relayNotice="방 목록을 읽지 못했습니다.";}
    }else relayNotice=request.responseCode==401?"온라인 대전의 접속 설정을 확인하세요.":"중계 서버에서 방 목록을 불러오지 못했습니다.";
   }
   loadingRooms=false;
  }
  void OpenOnline(PvpRelayRoom room){
   var boot=GetComponent<PvpBootstrap>();
   boot.ClearRelayError();
   if(!NameReady()||!LoadRelay())return;
   int seconds=room?.seconds??0;
   if(room==null){
    if(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200){relayNotice="경기 시간을 30~7200초로 입력하세요.";return;}
    PvpConditionRules.Configure(condition);
   }
   boot.StartCoroutine(boot.StartRelay(relay,room,seconds));
  }
  // 내 차: appearance unlocked by single-player purchases. Performance is PVP standard for all.
  VehicleUnlocks unlocks;PvpVehicleLook look;
  public static string LookName(PvpVehicleLook l){var v=VehicleCatalog.Get(l.model)??VehicleCatalog.Standard;return v.name+" · "+v.colorNames[l.color==1?1:0]+(l.ornament?" · 화분":"");}
  void DrawMyCar(){
   if(unlocks==null){unlocks=VehicleUnlockStore.Load();look=VehicleUnlockStore.Selected;}
   GUILayout.Label("내 차",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=20});
   GUILayout.Label("외형만 바뀝니다 · 성능은 모두 PVP 표준 · 싱글에서 산 차만 고를 수 있어요",new GUIStyle(GUI.skin.label){fontSize=14});
   // Same icon and 2:1 proportions as the single-player garage cards.
   var row=GUILayoutUtility.GetRect(10,170,GUILayout.ExpandWidth(true));float gap=10,w=(row.width-gap*2)/3;
   var name=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=16,alignment=TextAnchor.MiddleCenter};var note=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleCenter};
   for(int i=0;i<VehicleCatalog.All.Length;i++){
    var spec=VehicleCatalog.All[i];bool have=unlocks.HasModel(spec.id),chosen=look.model==spec.id;var card=new Rect(row.x+i*(w+gap),row.y,w,row.height);
    if(chosen)CasualUI.RoundedFill(card,CasualUI.Blue);CasualUI.RoundedFill(chosen?new Rect(card.x+3,card.y+3,card.width-6,card.height-6):card,chosen?CasualUI.Soft:CasualUI.Card);
    VehicleIcon.Draw(new Rect(card.x+12,card.y+8,card.width-24,(card.width-24)/2),spec,chosen?look.color:0,chosen&&look.ornament,!have);
    GUI.Label(new Rect(card.x,card.yMax-58,card.width,26),spec.name,name);GUI.Label(new Rect(card.x,card.yMax-32,card.width,22),chosen?"선택됨":have?"선택하기":"잠김 · 싱글에서 구매",note);
    bool enabled=GUI.enabled;GUI.enabled=enabled&&have&&!chosen;if(GUI.Button(card,GUIContent.none,GUIStyle.none)){look=new PvpVehicleLook{model=spec.id,color=0,ornament=unlocks.ornament};VehicleUnlockStore.Selected=look;}GUI.enabled=enabled;
   }
   GUILayout.Space(8);GUILayout.BeginHorizontal();var current=VehicleCatalog.Get(look.model)??VehicleCatalog.Standard;
   for(int c=0;c<2;c++){bool have=unlocks.HasColor(current.id,c),chosen=look.color==c;var rgb=current.colors[c];
    bool enabled=GUI.enabled;GUI.enabled=enabled&&have;if(GUILayout.Button("      "+current.colorNames[c]+(chosen?" · 사용 중":have?"":" · 잠김"),chosen?CasualUI.Action(15):KoreanText.Button(15),GUILayout.Height(38))&&!chosen){look=new PvpVehicleLook{model=look.model,color=c,ornament=unlocks.ornament};VehicleUnlockStore.Selected=look;}GUI.enabled=enabled;
    var b=GUILayoutUtility.GetLastRect();CasualUI.RoundedFill(new Rect(b.x+10,b.y+8,22,b.height-16),new Color(rgb[0],rgb[1],rgb[2]));}
   GUILayout.EndHorizontal();
   if(VehicleUnlockStore.LastError.Length>0)GUILayout.Label(VehicleUnlockStore.LastError);
   GUILayout.Space(12);
  }  static string ConditionSummary(PvpConditionSettings c){return "식사 "+c.mealCost.ToString("N0")+"원 / "+c.mealSeconds.ToString("0")+"초 · 휴식 "+(c.restCost==0?"무료":c.restCost.ToString("N0")+"원")+" / "+c.restSeconds.ToString("0")+"초\n피로 증가 "+(4*c.fatigueMultiplier).ToString("0.##")+"점/분 (배고픔에 따라 증가) · 피로 최대 100";}
  double Setting(string label,double value,double min,double max,double step,string format,string unit){
   GUILayout.BeginHorizontal();
   GUILayout.Label(label,new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleLeft},GUILayout.MinWidth(170),GUILayout.Height(36));
   bool enabled=GUI.enabled;GUI.enabled=enabled&&value>min;
   if(GUILayout.Button("−",KoreanText.Button(20),GUILayout.Width(44),GUILayout.Height(36)))value=Math.Max(min,Math.Round((value-step)*100)/100);
   GUI.enabled=enabled;
   GUILayout.Label(value.ToString(format)+unit,new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18,alignment=TextAnchor.MiddleCenter},GUILayout.Width(140),GUILayout.Height(36));
   GUI.enabled=enabled&&value<max;
   if(GUILayout.Button("+",KoreanText.Button(20),GUILayout.Width(44),GUILayout.Height(36)))value=Math.Min(max,Math.Round((value+step)*100)/100);
   GUI.enabled=enabled;
   GUILayout.Label(step.ToString("0.##")+unit+"씩",new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleRight},GUILayout.Width(90),GUILayout.Height(36));
   GUILayout.EndHorizontal();GUILayout.Space(4);return value;
  }
  void DrawConditionSettings(){
   if(GUILayout.Button(ConditionSettingsOpen?"방 규칙 설정 닫기  ▲":"방 규칙 설정  ▼",KoreanText.Button(17),GUILayout.Height(38)))ConditionSettingsOpen=!ConditionSettingsOpen;
   if(!ConditionSettingsOpen){GUILayout.Space(8);return;}
   GUILayout.BeginVertical(GUI.skin.box);GUILayout.Label("봇 경기 / 방 만들기에 적용",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18});
   condition.fatigueMultiplier=Setting("피로 증가 배율",condition.fatigueMultiplier,.25,3,.05,"0.##","배");
   condition.mealCost=(int)Setting("식사 비용",condition.mealCost,0,100000,100,"N0","원");
   condition.mealSeconds=Setting("식사 시간",condition.mealSeconds,1,300,1,"0","초");
   condition.restCost=(int)Setting("휴식 비용 (0원: 무료)",condition.restCost,0,100000,100,"N0","원");
   condition.restSeconds=Setting("휴식 시간",condition.restSeconds,1,300,1,"0","초");
   GUILayout.Label(ConditionSummary(condition));double toMax=PvpConditionRules.SecondsToFatigue100(new DriverCondition(),condition.fatigueMultiplier);GUILayout.Label("기본 시작 상태 → 피로100 약 "+Math.Ceiling(toMax/60).ToString("0")+"분 (식사·휴식·승객 효과 전)");GUILayout.Label("식사 포만감 +60 · 휴식 피로 -40 · 참가자는 호스트 규칙을 따릅니다.");
   if(GUILayout.Button("방 규칙 기본값 복원",KoreanText.Button(15),GUILayout.Height(32)))condition=new PvpConditionSettings();GUILayout.EndVertical();GUILayout.Space(10);
  }
  void Native(bool host){if(!NameReady())return;ushort port;int seconds=1200;if(!ushort.TryParse(networkPort,out port)||port==0){message="포트를 확인하세요";return;}if(host&&(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)){message="경기 시간을 입력하세요";return;}if(host)PvpConditionRules.Configure(condition);GetComponent<PvpBootstrap>().StartNativeLan(host,host?"127.0.0.1":networkAddress,port,seconds);}
  void DrawNativeLobby(){var lan=PvpNativeLan.Instance;if(!lan||lan.InMatch)return;bool online=lan.RelayRoomName!=null;CasualUI.Backdrop();float w=Mathf.Min(640,Screen.width-32),h=Mathf.Min(500,Screen.height-32),x=(Screen.width-w)/2,y=(Screen.height-h)/2;CasualUI.Panel(new Rect(x,y,w,h));CasualUI.Header(new Rect(x+8,y+8,w-16,52),online?"온라인 대기실":"LAN 대기실",1);GUILayout.BeginArea(new Rect(x+22,y+78,w-44,h-96));scroll=GUILayout.BeginScrollView(scroll);GUILayout.Label(lan.Status);GUILayout.Label(online?lan.RelayRoomName:"호스트 "+(lan.IsHost?PvpNativeLan.LocalAddress():lan.Address)+" : "+lan.Port);GUILayout.Label("경기 "+lan.Seconds+"초");GUILayout.Space(12);for(int i=0;i<4;i++){GUILayout.BeginVertical(GUI.skin.box);GUILayout.Label(lan.Label(i),new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18});GUILayout.Label("차량 "+LookName(lan.Look(lan.LocalToServer(i))));GUILayout.EndVertical();}if(lan.IsHost&&!lan.Started&&GUILayout.Button("현재 인원으로 경기 시작",CasualUI.Action(18),GUILayout.Height(48)))lan.StartMatch();if(GUILayout.Button("방에서 나가기",KoreanText.Button(17),GUILayout.Height(40)))lan.LeaveToMenu();GUILayout.EndScrollView();GUILayout.EndArea();}

  void Network(bool host){ushort port;if(!ushort.TryParse(networkPort,out port)||port==0){message="유효한 포트를 입력하세요.";return;}GetComponent<PvpBootstrap>().StartNetwork(host,networkAddress,port);}
 }
}

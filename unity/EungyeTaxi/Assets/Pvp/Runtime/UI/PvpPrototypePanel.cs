using System;
using UnityEngine;
namespace Pvp {
 public sealed class PvpPrototypePanel : MonoBehaviour {
  string duration="",message="경기 시간을 입력하세요 (30~7200초).";
  string networkAddress="127.0.0.1",networkPort="7777";
  string nickname="";void Awake(){nickname=DriveFlowPolicy.LocalName;}
  bool NameReady(){if(DriveFlowPolicy.SaveName(nickname)){nickname=DriveFlowPolicy.LocalName;return true;}message="닉네임을 입력하세요 (한글/영문/숫자, 최대12자).";return false;}
  bool legacy,showControls;Vector2 scroll;
  PvpConditionSettings condition=new PvpConditionSettings();
  public bool ConditionSettingsOpen {get;set;}
  void OnGUI(){
   GUI.skin.font=KoreanText.Font;
   if(PvpNativeLan.Instance){DrawNativeLobby();return;}
   CasualUI.Backdrop();float width=Mathf.Min(760,Screen.width-32),height=Mathf.Min(680,Screen.height-32),x=(Screen.width-width)/2,y=(Screen.height-height)/2;
   CasualUI.Panel(new Rect(x,y,width,height));CasualUI.Header(new Rect(x+8,y+8,width-16,52),"PVP · 함께 달리는 택시",1);
   GUILayout.BeginArea(new Rect(x+18,y+76,width-36,height-92));scroll=GUILayout.BeginScrollView(scroll);
   GUILayout.Label("내 이름",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=20});nickname=GUILayout.TextField(nickname,12);GUILayout.Label("한글·영문·숫자, 최대 12자");GUILayout.Space(12);
   DrawConditionSettings();
   GUILayout.BeginVertical(GUI.skin.box);
   GUILayout.Label("나 + 봇 3대 · 공용 호출 8개 · 오프라인");
   GUILayout.BeginHorizontal();GUILayout.Label("경기 시간(초, 30~7200)");duration=GUILayout.TextField(duration,5);GUILayout.EndHorizontal();
   GUILayout.Label("전용 도시 · 다차로·골목·신호·유턴");
   if(GUILayout.Button("봇 3대와 경기 시작",CasualUI.Action(18),GUILayout.Height(48))){int seconds;if(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)message="30~7200초로 입력하세요.";else if(NameReady()){PvpConditionRules.Configure(condition);GetComponent<PvpBootstrap>().StartSimulation(true,seconds);}}
   GUILayout.EndVertical();GUILayout.Space(10);GUILayout.BeginVertical(GUI.skin.box);GUILayout.Label("LAN 대전 · 최대 4명",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=20});GUILayout.Label("같은 네트워크에서 접속 · 빈자리는 봇");
   GUILayout.BeginHorizontal();GUILayout.Label("호스트 IP",GUILayout.Width(75));networkAddress=GUILayout.TextField(networkAddress,45);GUILayout.Label("포트",GUILayout.Width(40));networkPort=GUILayout.TextField(networkPort,5,GUILayout.Width(75));GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();if(GUILayout.Button("방 만들기",CasualUI.Action(18),GUILayout.Height(44)))Native(true);if(GUILayout.Button("IP로 참가",KoreanText.Button(18),GUILayout.Height(44)))Native(false);GUILayout.EndHorizontal();GUILayout.EndVertical();
   GUILayout.Label(PvpNativeLan.LastNotice);
   var boot=GetComponent<PvpBootstrap>();if(!string.IsNullOrEmpty(boot.StartupError))GUILayout.Label(boot.StartupError);
   GUILayout.Label(message);GUILayout.Space(12);
   if(GUILayout.Button(showControls?"조작법 닫기":"조작법 보기",KoreanText.Button(16)))showControls=!showControls;
   if(showControls)GUILayout.Label("운전  W/↑ 가속 · S/↓ 감속·후진 · A/D/←→ 조향 · Space 제동\n호출  M 지도에서 선택·수락 → 승차 지점 정차 → F 승하차\n시점  C 전환 · B 누르는 동안 후방 · Z/X 좌우 확인\n보조  H 조향 보조 · Q/E 방향지시(보조 중 방향 요청) · G 비상등\n시설  정차 후 F7 · Esc 메뉴 (LAN 경기는 계속 진행)");
   GUILayout.Label("지도를 열어도 경기는 계속됩니다. 내 차는 멈춥니다.");
   legacy=GUILayout.Toggle(legacy,"이전 시험장 (개발용)");
   if(legacy){GUILayout.Label("이전 평면 모터 개발 검사입니다. 현재 도시 LAN 대전은 위의 방 만들기/IP 참가를 사용하세요.");if(GUILayout.Button("이전 오프라인 시험장"))GetComponent<PvpBootstrap>().StartPractice();GUILayout.BeginHorizontal();networkAddress=GUILayout.TextField(networkAddress,45);networkPort=GUILayout.TextField(networkPort,5);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();if(GUILayout.Button("이전 IP 호스트"))Network(true);if(GUILayout.Button("이전 IP 참가"))Network(false);GUILayout.EndHorizontal();}
   var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified&&GUILayout.Button("모드 선택"))unified.ReturnToMenu();
   if(GUILayout.Button("나가기"))Application.Quit();GUILayout.EndScrollView();GUILayout.EndArea();
  }
  static string ConditionSummary(PvpConditionSettings c){return "식사 "+c.mealCost.ToString("N0")+"원 / "+c.mealSeconds.ToString("0")+"초 · 휴식 "+(c.restCost==0?"무료":c.restCost.ToString("N0")+"원")+" / "+c.restSeconds.ToString("0")+"초\n피로 증가 "+(4*c.fatigueMultiplier).ToString("0.##")+"점/분 (배고픔에 따라 증가) · 피로 최대 100";}
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
   GUILayout.BeginVertical(GUI.skin.box);GUILayout.Label("봇 경기 / LAN 방 만들기에 적용",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18});
   condition.fatigueMultiplier=Setting("피로 증가 배율",condition.fatigueMultiplier,.25,3,.05,"0.##","배");
   condition.mealCost=(int)Setting("식사 비용",condition.mealCost,0,100000,100,"N0","원");
   condition.mealSeconds=Setting("식사 시간",condition.mealSeconds,1,300,1,"0","초");
   condition.restCost=(int)Setting("휴식 비용 (0원: 무료)",condition.restCost,0,100000,100,"N0","원");
   condition.restSeconds=Setting("휴식 시간",condition.restSeconds,1,300,1,"0","초");
   GUILayout.Label(ConditionSummary(condition));double toMax=PvpConditionRules.SecondsToFatigue100(new DriverCondition(),condition.fatigueMultiplier);GUILayout.Label("기본 시작 상태 → 피로100 약 "+Math.Ceiling(toMax/60).ToString("0")+"분 (식사·휴식·승객 효과 전)");GUILayout.Label("식사 포만감 +60 · 휴식 피로 -40 · 참가자는 호스트 규칙을 따릅니다.");
   if(GUILayout.Button("방 규칙 기본값 복원",KoreanText.Button(15),GUILayout.Height(32)))condition=new PvpConditionSettings();GUILayout.EndVertical();GUILayout.Space(10);
  }
  void Native(bool host){if(!NameReady())return;ushort port;int seconds=1200;if(!ushort.TryParse(networkPort,out port)||port==0){message="포트를 확인하세요";return;}if(host&&(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)){message="경기 시간을 입력하세요";return;}if(host)PvpConditionRules.Configure(condition);GetComponent<PvpBootstrap>().StartNativeLan(host,host?"127.0.0.1":networkAddress,port,seconds);}
  void DrawNativeLobby(){var lan=PvpNativeLan.Instance;if(!lan||lan.InMatch)return;CasualUI.Backdrop();float w=Mathf.Min(640,Screen.width-32),h=Mathf.Min(500,Screen.height-32),x=(Screen.width-w)/2,y=(Screen.height-h)/2;CasualUI.Panel(new Rect(x,y,w,h));CasualUI.Header(new Rect(x+8,y+8,w-16,52),"LAN 대기실",1);GUILayout.BeginArea(new Rect(x+22,y+78,w-44,h-96));scroll=GUILayout.BeginScrollView(scroll);GUILayout.Label(lan.Status);GUILayout.Label("호스트 "+(lan.IsHost?PvpNativeLan.LocalAddress():lan.Address)+" : "+lan.Port);GUILayout.Label("경기 "+lan.Seconds+"초 · 빈자리는 봇");GUILayout.Label(ConditionSummary(PvpConditionRules.Settings));GUILayout.Space(12);for(int i=0;i<4;i++){GUILayout.BeginVertical(GUI.skin.box);GUILayout.Label(lan.Label(i),new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18});GUILayout.EndVertical();}if(lan.IsHost&&!lan.Started&&GUILayout.Button("현재 인원으로 경기 시작",CasualUI.Action(18),GUILayout.Height(48)))lan.StartMatch();if(GUILayout.Button("방에서 나가기",KoreanText.Button(17),GUILayout.Height(40)))lan.LeaveToMenu();GUILayout.EndScrollView();GUILayout.EndArea();}

  void Network(bool host){ushort port;if(!ushort.TryParse(networkPort,out port)||port==0){message="유효한 포트를 입력하세요.";return;}GetComponent<PvpBootstrap>().StartNetwork(host,networkAddress,port);}
 }
}

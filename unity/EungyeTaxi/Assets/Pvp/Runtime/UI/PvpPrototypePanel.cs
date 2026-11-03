using System;
using UnityEngine;
namespace Pvp {
 public sealed class PvpPrototypePanel : MonoBehaviour {
  string duration="",message="경기 시간을 입력하세요 (30~7200초).";
  string networkAddress="127.0.0.1",networkPort="7777";
  string nickname="";void Awake(){nickname=DriveFlowPolicy.LocalName;}
  bool NameReady(){if(DriveFlowPolicy.SaveName(nickname)){nickname=DriveFlowPolicy.LocalName;return true;}message="닉네임을 입력하세요 (한글/영문/숫자, 최대12자).";return false;}
  bool legacy;Vector2 scroll;
  void OnGUI(){
   GUI.skin.font=KoreanText.Font;
   if(PvpNativeLan.Instance){DrawNativeLobby();return;}
   float width=Mathf.Min(680,Screen.width-32),height=Mathf.Min(620,Screen.height-32);
   GUILayout.BeginArea(new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height),GUI.skin.box);scroll=GUILayout.BeginScrollView(scroll);
   GUILayout.Label("PVP · 택시 경쟁");GUILayout.Label("닉네임 (최대12자)");nickname=GUILayout.TextField(nickname,12);GUILayout.Space(10);
   GUILayout.Label("나 + 봇 3대 · 공용 호출 8개 · 오프라인");
   GUILayout.BeginHorizontal();GUILayout.Label("경기 시간(초, 30~7200)");duration=GUILayout.TextField(duration,5);GUILayout.EndHorizontal();
   GUILayout.Label("전용 도시 · 다차로·골목·신호·유턴");
   if(GUILayout.Button("봇 3대와 경기 시작",CasualUI.Action(18),GUILayout.Height(48))){int seconds;if(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)message="30~7200초로 입력하세요.";else if(NameReady())GetComponent<PvpBootstrap>().StartSimulation(true,seconds);}
   GUILayout.Space(10);GUILayout.Label("사람과 LAN 대전 · 최대 4명 · 빈자리는 봇");
   GUILayout.BeginHorizontal();GUILayout.Label("호스트 IP",GUILayout.Width(75));networkAddress=GUILayout.TextField(networkAddress,45);GUILayout.Label("포트",GUILayout.Width(40));networkPort=GUILayout.TextField(networkPort,5,GUILayout.Width(75));GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();if(GUILayout.Button("방 만들기",CasualUI.Action(16),GUILayout.Height(40)))Native(true);if(GUILayout.Button("IP로 참가",CasualUI.Action(16),GUILayout.Height(40)))Native(false);GUILayout.EndHorizontal();
   GUILayout.Label(PvpNativeLan.LastNotice);
   var boot=GetComponent<PvpBootstrap>();if(!string.IsNullOrEmpty(boot.StartupError))GUILayout.Label(boot.StartupError);
   GUILayout.Label(message);GUILayout.Space(12);
   GUILayout.Label("운전  W/↑ 가속 · S/↓ 감속·후진 · A/D/←→ 조향 · Space 제동\n호출  M 지도에서 선택·수락 → 승차 지점 정차 → F 승하차\n시점  C 전환 · B 누르는 동안 후방 · Z/X 좌우 확인\n보조  H 조향 보조 · Q/E 방향지시(보조 중 방향 요청) · G 비상등\n시설  정차 후 F7 · Esc 메뉴 (LAN 경기는 계속 진행)");
   GUILayout.Label("지도를 열어도 경기는 계속됩니다. 내 차는 멈춥니다.");
   legacy=GUILayout.Toggle(legacy,"이전 시험장 (개발용)");
   if(legacy){GUILayout.Label("이전 평면 모터 개발 검사입니다. 현재 도시 LAN 대전은 위의 방 만들기/IP 참가를 사용하세요.");if(GUILayout.Button("이전 오프라인 시험장"))GetComponent<PvpBootstrap>().StartPractice();GUILayout.BeginHorizontal();networkAddress=GUILayout.TextField(networkAddress,45);networkPort=GUILayout.TextField(networkPort,5);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();if(GUILayout.Button("이전 IP 호스트"))Network(true);if(GUILayout.Button("이전 IP 참가"))Network(false);GUILayout.EndHorizontal();}
   var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified&&GUILayout.Button("모드 선택"))unified.ReturnToMenu();
   if(GUILayout.Button("나가기"))Application.Quit();GUILayout.EndScrollView();GUILayout.EndArea();
  }
  void Native(bool host){if(!NameReady())return;ushort port;int seconds=1200;if(!ushort.TryParse(networkPort,out port)||port==0){message="포트를 확인하세요";return;}if(host&&(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)){message="경기 시간을 입력하세요";return;}GetComponent<PvpBootstrap>().StartNativeLan(host,host?"127.0.0.1":networkAddress,port,seconds);}
  void DrawNativeLobby(){var lan=PvpNativeLan.Instance;if(!lan||lan.InMatch)return;GUILayout.BeginArea(new Rect((Screen.width-540)/2,(Screen.height-330)/2,540,330),GUI.skin.box);GUILayout.Label("은계 PVP · LAN 대기실");GUILayout.Label(lan.Status);GUILayout.Label("호스트 IP "+(lan.IsHost?PvpNativeLan.LocalAddress():lan.Address)+" : "+lan.Port);GUILayout.Label("경기 "+lan.Seconds+"초 · 빈자리는 봇으로 진행");for(int i=0;i<4;i++)GUILayout.Label(lan.Label(i));if(lan.IsHost&&!lan.Started&&GUILayout.Button("현재 인원으로 경기 시작",CasualUI.Action(18),GUILayout.Height(48)))lan.StartMatch();if(GUILayout.Button("방에서 나가기"))lan.LeaveToMenu();GUILayout.EndArea();}
  void Network(bool host){ushort port;if(!ushort.TryParse(networkPort,out port)||port==0){message="유효한 포트를 입력하세요.";return;}GetComponent<PvpBootstrap>().StartNetwork(host,networkAddress,port);}
 }
}

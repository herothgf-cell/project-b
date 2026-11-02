using System;
using UnityEngine;
namespace Pvp {
 public sealed class PvpPrototypePanel : MonoBehaviour {
  string duration="",message="경기 시간을 입력하세요 (30~7200초).";
  string networkAddress="127.0.0.1",networkPort="7777";
  bool legacy;Vector2 scroll;
  void OnGUI(){
   GUI.skin.font=KoreanText.Font;
   float width=Mathf.Min(680,Screen.width-32),height=Mathf.Min(620,Screen.height-32);
   GUILayout.BeginArea(new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height),GUI.skin.box);scroll=GUILayout.BeginScrollView(scroll);
   GUILayout.Label("PVP · 택시 경쟁");GUILayout.Space(16);
   GUILayout.Label("나 + 봇 3대 · 공용 호출 8개");
   GUILayout.BeginHorizontal();GUILayout.Label("경기 시간(초, 30~7200)");duration=GUILayout.TextField(duration,5);GUILayout.EndHorizontal();
   GUILayout.Label("전용 도시 · 다차로·골목·유턴");
   if(GUILayout.Button("경기 시작",CasualUI.Action(18),GUILayout.Height(48))){int seconds;if(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)message="30~7200초로 입력하세요.";else GetComponent<PvpBootstrap>().StartSimulation(true,seconds);}
   GUILayout.Label(message);GUILayout.Space(12);
   GUILayout.Label("WASD 운전 · Space 제동\nM 호출 선택 · F 승하차 · F7 시설\nC 시점 · H 조향 보조 · Esc 일시정지\n자세한 조작은 게임 안의 도움말을 확인하세요.");
   GUILayout.Label("지도를 열어도 경기는 계속됩니다. 내 차는 멈춥니다.");
   legacy=GUILayout.Toggle(legacy,"이전 시험장 (개발용)");
   if(legacy){GUILayout.Label("이전 평면 모터 실험입니다. 기존 차량 기반 유저 간 네트워크 대전은 별도 전환 검증이 필요합니다.");if(GUILayout.Button("이전 오프라인 시험장"))GetComponent<PvpBootstrap>().StartPractice();GUILayout.BeginHorizontal();networkAddress=GUILayout.TextField(networkAddress,45);networkPort=GUILayout.TextField(networkPort,5);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();if(GUILayout.Button("이전 IP 호스트"))Network(true);if(GUILayout.Button("이전 IP 참가"))Network(false);GUILayout.EndHorizontal();}
   var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified&&GUILayout.Button("모드 선택"))unified.ReturnToMenu();
   if(GUILayout.Button("나가기"))Application.Quit();GUILayout.EndScrollView();GUILayout.EndArea();
  }
  void Network(bool host){ushort port;if(!ushort.TryParse(networkPort,out port)||port==0){message="유효한 포트를 입력하세요.";return;}GetComponent<PvpBootstrap>().StartNetwork(host,networkAddress,port);}
 }
}

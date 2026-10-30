using System;
using UnityEngine;
namespace Pvp {
 public sealed class PvpPrototypePanel : MonoBehaviour {
  string duration="",message="경기 시간을 입력하고 지도를 선택하세요. 기존 시뮬레이션 조작을 그대로 사용합니다.";
  string networkAddress="127.0.0.1",networkPort="7777";
  bool expanded,legacy;Vector2 scroll;
  void OnGUI(){
   GUI.skin.font=KoreanText.Font;
   float width=Mathf.Min(680,Screen.width-32),height=Mathf.Min(620,Screen.height-32);
   GUILayout.BeginArea(new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height),GUI.skin.box);scroll=GUILayout.BeginScrollView(scroll);
   GUILayout.Label("은계 택시 시뮬레이터 · PVP 봇 대전");GUILayout.Space(16);
   GUILayout.Label("기존 차량·운전석·승객·법규·내구도·시설로 경쟁합니다.");
   GUILayout.BeginHorizontal();GUILayout.Label("경기 시간(초, 30~7200)");duration=GUILayout.TextField(duration,5);GUILayout.EndHorizontal();
   expanded=GUILayout.SelectionGrid(expanded?1:0,new[]{"은계 맵","확장 A · 신규 도시"},2)==1;
   GUILayout.Label(expanded?"3.6 × 2.8km · 9개 권역 · 승객 거점 18곳":"기존 은계 시뮬레이션 지도와 콘텐츠");
   if(GUILayout.Button("봇과 경기 시작",GUILayout.Height(44))){int seconds;if(!int.TryParse(duration,out seconds)||seconds<30||seconds>7200)message="경기 시간은 30~7200초 범위에서 직접 입력하세요.";else GetComponent<PvpBootstrap>().StartSimulation(expanded,seconds);}
   GUILayout.Label(message);GUILayout.Space(12);
   GUILayout.Label("WASD/방향키 운전 · Space 제동 · F 승하차\nC 시점 · B 후방 · M 지도 · F7 시설 (기존 조작 동일)");
   GUILayout.Label("현재 연결: 한 창에서 나와 봇의 경기. 기존 차량 기반 유저 간 네트워크 대전은 별도 전환 검증이 필요합니다.");
   legacy=GUILayout.Toggle(legacy,"개발용 이전 시험장 검사 열기");
   if(legacy){GUILayout.Label("이전 평면 모터 실험입니다. 위 시뮬레이션 경기와 다릅니다.");if(GUILayout.Button("이전 오프라인 시험장"))GetComponent<PvpBootstrap>().StartPractice();GUILayout.BeginHorizontal();networkAddress=GUILayout.TextField(networkAddress,45);networkPort=GUILayout.TextField(networkPort,5);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();if(GUILayout.Button("이전 IP 호스트"))Network(true);if(GUILayout.Button("이전 IP 참가"))Network(false);GUILayout.EndHorizontal();}
   if(GUILayout.Button("나가기"))Application.Quit();GUILayout.EndScrollView();GUILayout.EndArea();
  }
  void Network(bool host){ushort port;if(!ushort.TryParse(networkPort,out port)||port==0){message="유효한 포트를 입력하세요.";return;}GetComponent<PvpBootstrap>().StartNetwork(host,networkAddress,port);}
 }
}

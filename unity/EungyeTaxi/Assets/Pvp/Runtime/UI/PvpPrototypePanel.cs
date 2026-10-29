using System;
using System.Globalization;
using UnityEngine;

namespace Pvp {
    public sealed class PvpPrototypePanel : MonoBehaviour {
        string duration="",players="",grace="0",message="혼자 경기는 시간과 유예만 입력하세요. 경기 시간은 직접 정합니다.";
        Vector2 scroll;
        string networkAddress="127.0.0.1",networkPort="7777";
        void OnGUI() {
            float width=Mathf.Min(680,Screen.width-32);
            float height=Mathf.Min(640,Screen.height-32);GUILayout.BeginArea(new Rect((Screen.width-width)/2,Mathf.Max(16,(Screen.height-height)/2),width,height),GUI.skin.box);scroll=GUILayout.BeginScrollView(scroll);
            GUILayout.Label("PVP 프로토타입 / 초기 설정 확인");
            GUILayout.Space(12);
            GUILayout.Label("혼자 한 창에서 운전하며 봇과 승객 운송·NPC·법규·정산을 시험할 수 있습니다.");
            duration=Field("경기 시간(초) — 필수",duration);
            grace=Field("마감 유예(초) — 0 허용",grace);
            if(GUILayout.Button("혼자 경기 시작 · 봇 상대")) StartSolo();
            GUILayout.Label("인터넷/IP 입력 없이 나 1대 + 봇 1대. 승객을 선택하고 현장에서 E로 승하차하세요.");
            GUILayout.Space(8);players=Field("설정 검사 전용 참가 인원 — 최소 2명",players);
            if(GUILayout.Button("설정 확인")) Validate();
            GUILayout.Label(message);
            var bootstrap=GetComponent<PvpBootstrap>();
            if(bootstrap!=null&&!String.IsNullOrEmpty(bootstrap.StartupError)) GUILayout.Label(bootstrap.StartupError);
            if(GUILayout.Button("오프라인 시험장 주행")) { if(bootstrap!=null) bootstrap.StartPractice(); }
            GUILayout.Space(8);
            GUILayout.Label("직접 IP 2인 시험 (접속 후 준비·경기 시간 설정)");
            GUILayout.BeginHorizontal();GUILayout.Label("Host IP / 호스트의 수신 IP");networkAddress=GUILayout.TextField(networkAddress,45);networkPort=GUILayout.TextField(networkPort,5);GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("호스트 시작")) StartNetwork(true);
            if(GUILayout.Button("IP로 참가")) StartNetwork(false);
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("나가기")) Quit();
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void StartSolo(){int seconds,finish;if(!Int32.TryParse(duration,out seconds)||!Int32.TryParse(grace,out finish)){message="경기 시간과 마감 유예를 초 단위 정수로 입력하세요.";return;}GetComponent<PvpBootstrap>().StartSolo(seconds,finish);}
        void StartNetwork(bool host){ushort port;if(!ushort.TryParse(networkPort,out port)||port==0){message="유효한 포트를 입력하세요.";return;}GetComponent<PvpBootstrap>().StartNetwork(host,networkAddress,port);}
        static string Field(string label,string value) { GUILayout.BeginHorizontal();GUILayout.Label(label,GUILayout.Width(320));value=GUILayout.TextField(value,10);GUILayout.EndHorizontal();return value; }
        void Validate() {
            int parsedDuration,parsedPlayers,parsedGrace;
            if(!Int32.TryParse(duration,NumberStyles.Integer,CultureInfo.InvariantCulture,out parsedDuration)||!Int32.TryParse(players,NumberStyles.Integer,CultureInfo.InvariantCulture,out parsedPlayers)||!Int32.TryParse(grace,NumberStyles.Integer,CultureInfo.InvariantCulture,out parsedGrace)) { message="경기 시간·참가 인원·마감 유예를 정수로 입력하세요.";return; }
            if(parsedDuration<=0) { message="경기 시간은 0보다 커야 합니다.";return; }
            if(parsedPlayers<2) { message="참가 인원은 최소 2명이어야 합니다.";return; }
            if(parsedGrace<0) { message="마감 유예는 0 이상이어야 합니다.";return; }
            try {
                new PvpSessionConfig { MatchDurationSeconds=parsedDuration,PlayerCount=parsedPlayers,FinishGraceSeconds=parsedGrace,RulesVersion="startup-validation-v1" }.Validate();
                message="설정이 유효합니다. 검증된 시험장과 온라인 세션이 준비되면 경기를 만들 수 있습니다.";
            } catch(ArgumentException) { message="유효하지 않은 설정입니다. 입력값을 확인하세요."; }
        }
        static void Quit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}

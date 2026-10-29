using System;
using System.Globalization;
using UnityEngine;

namespace Pvp {
    public sealed class PvpPrototypePanel : MonoBehaviour {
        string duration="",players="",grace="0",message="경기 시간과 참가 인원을 입력하세요. 기본 경기 시간은 정해지지 않았습니다.";
        void OnGUI() {
            float width=Mathf.Min(680,Screen.width-32);
            GUILayout.BeginArea(new Rect((Screen.width-width)/2,Mathf.Max(16,(Screen.height-400)/2),width,400),GUI.skin.box);
            GUILayout.Label("PVP 프로토타입 / 초기 설정 확인");
            GUILayout.Space(12);
            GUILayout.Label("시험장·차량·온라인 경기는 아직 준비 중입니다.");
            GUILayout.Label("이 화면은 초기 설정만 확인하며 경기를 시작하지 않습니다.");
            duration=Field("경기 시간(초) — 필수",duration);
            players=Field("참가 인원 — 필수, 최소 2명",players);
            grace=Field("마감 유예(초) — 0 허용",grace);
            if(GUILayout.Button("설정 확인")) Validate();
            GUILayout.Label(message);
            var bootstrap=GetComponent<PvpBootstrap>();
            if(bootstrap!=null&&!String.IsNullOrEmpty(bootstrap.StartupError)) GUILayout.Label("진단 기록을 저장하지 못했습니다. 로그를 확인하세요.");
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("나가기")) Quit();
            GUILayout.EndArea();
        }
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

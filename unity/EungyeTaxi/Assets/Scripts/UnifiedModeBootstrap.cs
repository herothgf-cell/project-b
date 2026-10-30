using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// A scene-owned shell: neither campaign map nor save is opened before selection.
public sealed class UnifiedModeBootstrap : MonoBehaviour {
    public enum Mode { Menu, Single, Pvp }
    public static Mode Selected { get; private set; }
    static bool commandConsumed;
    public string startupRegion="eungye";
    Camera menuCamera;
    DriveSession human;
    string message="";
    public static bool IsUnified { get { return FindObjectOfType<UnifiedModeBootstrap>()!=null; } }
    public static bool IsSingleCommand(string[] args) {
        foreach(var arg in args)if(arg.StartsWith("-pvp",StringComparison.OrdinalIgnoreCase)||arg=="-unifiedModeCheck")return false;
        foreach(var arg in args)if(arg.EndsWith("Test",StringComparison.Ordinal)||arg=="-singlePlayer"||arg=="-oneDayMode"||arg=="-assistedTrial"||arg=="-hybridV2")return true;
        return false;
    }
    void Start(){
        Time.timeScale=1;
        var args=Environment.GetCommandLineArgs();
        if(!commandConsumed){
            commandConsumed=true;
            if(Array.IndexOf(args,"-unifiedModeCheck")>=0)new GameObject("Unified mode acceptance").AddComponent<UnifiedModeProbe>();
            else if(IsSingleCommand(args))Selected=Mode.Single;
            else foreach(var arg in args)if(arg.StartsWith("-pvp",StringComparison.OrdinalIgnoreCase)){Selected=Mode.Pvp;break;}
        }
        if(Selected==Mode.Single){StartSingle();return;}
        if(Selected==Mode.Pvp){StartPvp();return;}
        menuCamera=new GameObject("모드 선택 화면").AddComponent<Camera>();menuCamera.transform.SetParent(transform,false);
        menuCamera.clearFlags=CameraClearFlags.SolidColor;menuCamera.backgroundColor=new Color(.06f,.09f,.13f);menuCamera.cullingMask=0;
    }
    void HideMenu(){if(menuCamera)menuCamera.gameObject.SetActive(false);message="";}
    public void StartSingle(){
        if(FindObjectOfType<DriveSession>()||FindObjectOfType<Pvp.PvpBootstrap>())return;
        Selected=Mode.Single;PvpSimulationMode.Reset();HideMenu();
        var root=new GameObject("Drive Session");root.SetActive(false);
        root.AddComponent<DriveSession>().startupRegion=startupRegion;root.SetActive(true);
    }
    public void StartPvp(){
        if(FindObjectOfType<DriveSession>()||FindObjectOfType<Pvp.PvpBootstrap>())return;
        Selected=Mode.Pvp;HideMenu();new GameObject("PVP mode").AddComponent<Pvp.PvpBootstrap>();
    }
    public bool ReturnToMenu(){
        var session=FindObjectOfType<DriveSession>();
        if(session&&!session.IsSimulationBot&&session.business&&!session.business.SimulationSession){
            // Rejected saves remain untouched; no reset or recovery is implicit.
            if(session.business.IsRestoring){message="저장 복원이 끝난 뒤 다시 시도해 주세요.";return false;}
            if(!session.business.ResumeBlocked&&!session.business.SaveCheckpoint()){message="저장하지 못했습니다. 저장 상태를 확인한 뒤 다시 시도해 주세요.";return false;}
        }
        Selected=Mode.Menu;PvpSimulationMode.Reset();Time.timeScale=1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);return true;
    }
    void OnGUI(){
        GUI.skin.font=KoreanText.Font;
        if(Selected==Mode.Menu){
            float width=Mathf.Min(580,Screen.width-32);
            GUILayout.BeginArea(new Rect((Screen.width-width)/2,Screen.height*.28f,width,310),GUI.skin.box);
            GUILayout.Label("은계 택시 시뮬레이터");GUILayout.Space(20);
            if(GUILayout.Button("싱글 플레이 · 은계 영업 이어하기",GUILayout.Height(55)))StartSingle();
            GUILayout.Label("기존 캠페인의 준비·운송·정산을 이어갑니다.");GUILayout.Space(12);
            if(GUILayout.Button("PVP · 자체 도시에서 봇 3대와 경기",GUILayout.Height(55)))StartPvp();
            GUILayout.Label("경기 시간 직접 설정 · 캠페인 저장과 분리");GUILayout.Space(12);
            if(GUILayout.Button("게임 종료"))Application.Quit();GUILayout.EndArea();return;
        }
        if(!human)foreach(var item in FindObjectsOfType<DriveSession>())if(!item.IsSimulationBot){human=item;break;}
        if(human&&human.StartupReady&&(human.paused||human.business.preparing)&&!human.business.IsRestoring){
            GUI.depth=-300;
            if(GUI.Button(new Rect(Screen.width-200,12,188,38),"모드 선택으로 돌아가기"))ReturnToMenu();
            if(message.Length>0)GUI.Box(new Rect(Screen.width-420,56,408,65),message);
        }
    }
}

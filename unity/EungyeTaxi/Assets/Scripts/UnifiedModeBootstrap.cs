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
        menuCamera.clearFlags=CameraClearFlags.SolidColor;menuCamera.backgroundColor=CasualUI.Canvas;menuCamera.cullingMask=0;
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
        if(PvpNativeLan.Instance)PvpNativeLan.Instance.StopTransport();
        Selected=Mode.Menu;PvpSimulationMode.Reset();Time.timeScale=1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);return true;
    }
    void OnGUI(){
        GUI.skin.font=KoreanText.Font;
        if(Selected==Mode.Menu){
            int choice=CasualMenuUI.Main();if(choice==1)StartSingle();else if(choice==2)StartPvp();else if(choice==3)Application.Quit();return;
        }
        if(!human)foreach(var item in FindObjectsOfType<DriveSession>())if(!item.IsSimulationBot){human=item;break;}
        if(human&&human.StartupReady&&(human.paused||human.business.preparing)&&!human.business.IsRestoring){
            GUI.depth=-300;
            if(GUI.Button(new Rect(Screen.width-200,12,188,38),"모드 선택"))ReturnToMenu();
            if(message.Length>0)GUI.Box(new Rect(Screen.width-420,56,408,65),message);
        }
    }
}

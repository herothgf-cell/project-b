using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// A scene-owned shell: neither campaign map nor save is opened before selection.
public sealed class UnifiedModeBootstrap : MonoBehaviour {
    public enum Mode { Menu, Single, Pvp }
    public static Mode Selected { get; private set; }
    static bool commandConsumed;
    public string startupRegion="eungye";
    Camera menuCamera;
    string message="";
    public string Message {get{return message;}}
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
    enum SinglePage { Closed, Choose, Slots, Replace }
    string replaceTarget;
    SinglePage singlePage;
    string[] slots=Array.Empty<string>();
    Vector2 scroll;
    static string titleSlot;
    public static string ConsumeTitleSlot(){var path=titleSlot;titleSlot=null;return path;}
    string DefaultCareerPath(){
        if(UnifiedModeProbe.Enabled)return UnifiedModeProbe.CampaignPath;
        return CampaignSaveLocation.Resolve(Path.GetDirectoryName(Application.persistentDataPath),PrototypeSettings.ThreeDaySaveFile);
    }
    public void NewSingle(){
        try{
            string first=DefaultCareerPath();
            // Three slots only: with all three in use the player picks the one the new career replaces.
            if(CampaignSaveLocation.Full(Path.GetDirectoryName(first))){ShowSlots();singlePage=SinglePage.Replace;replaceTarget=null;return;}
            titleSlot=File.Exists(first)||File.Exists(first+".bak")?CampaignSaveLocation.NewCareer(Path.GetDirectoryName(first)):first;
            StartSingle();
        }catch(Exception e){message="새 경력을 열 수 없습니다. "+e.Message;}
    }
    public void ShowSlots(){
        singlePage=SinglePage.Slots;scroll=Vector2.zero;message="";slots=Array.Empty<string>();
        try{slots=CampaignSaveLocation.ListCareers(Path.GetDirectoryName(DefaultCareerPath()));}
        catch(Exception e){message="저장 목록을 읽지 못했습니다. "+e.Message;}
    }
    public bool ReplaceSingle(string path){
        if(Array.IndexOf(slots,path)<0)return false;
        try{CampaignSaveLocation.Retire(path);titleSlot=CampaignSaveLocation.NewCareer(Path.GetDirectoryName(path));StartSingle();return true;}
        catch(Exception e){message="슬롯을 비우지 못했습니다. "+e.Message;return false;}
    }
    public bool LoadSingle(string path){
        if(Array.IndexOf(slots,path)<0)return false;
        try{CampaignSaveLocation.SelectCareer(path);titleSlot=path;StartSingle();return true;}
        catch(Exception e){message="저장 파일을 열지 못했습니다. "+e.Message;return false;}
    }
    void DrawSinglePopup(){
        CasualUI.Backdrop();
        float w=Mathf.Min(Screen.width-24,460),h=Mathf.Min(Screen.height-24,380);
        var panel=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);
        CasualUI.Panel(panel);GUI.BeginGroup(panel);
        bool replacing=singlePage==SinglePage.Replace,listing=singlePage==SinglePage.Slots||replacing;
        CasualUI.Header(new Rect(6,6,w-12,46),replacing?"덮어쓸 슬롯 선택":listing?"불러오기":"싱글",0);
        var button=KoreanText.Button(18);
        var info=new GUIStyle(GUI.skin.label){wordWrap=true};info.normal.textColor=CasualUI.Ink;
        if(listing){
            if(replacing&&replaceTarget!=null){
                int chosen=Array.IndexOf(slots,replaceTarget);
                GUI.Label(new Rect(22,70,w-44,70),CampaignSaveLocation.SlotLabel(chosen,replaceTarget)+"\n이 슬롯의 경력 대신 새 경력을 시작합니다.",info);
                if(GUI.Button(new Rect(22,150,w-44,50),"덮어쓰고 시작",CasualUI.Action(18)))ReplaceSingle(replaceTarget);
                if(GUI.Button(new Rect(22,212,w-44,44),"다른 슬롯 고르기",button))replaceTarget=null;
                GUI.Label(new Rect(22,h-104,w-44,48),message,info);
                if(GUI.Button(new Rect(22,h-54,w-44,40),"돌아가기",button)){singlePage=SinglePage.Choose;message="";}
                GUI.EndGroup();return;
            }
            if(slots.Length==0&&message.Length==0)GUI.Label(new Rect(22,76,w-44,38),"저장된 경력이 없습니다.",info);
            var area=new Rect(18,58,w-36,h-166);
            scroll=GUI.BeginScrollView(area,scroll,new Rect(0,0,area.width-20,slots.Length*48));
            for(int i=0;i<slots.Length;i++){
                var p=slots[i];var date=File.GetLastWriteTime(File.Exists(p)?p:p+".bak");
                if(GUI.Button(new Rect(0,i*48,area.width-20,44),CampaignSaveLocation.SlotLabel(i,p),button)){if(replacing)replaceTarget=p;else LoadSingle(p);}
            }
            GUI.EndScrollView();
        }else{
            if(GUI.Button(new Rect(22,74,w-44,50),"처음부터 하기",CasualUI.Action(18)))NewSingle();
            if(GUI.Button(new Rect(22,136,w-44,50),"불러오기",button))ShowSlots();
        }
        GUI.Label(new Rect(22,h-104,w-44,48),message,info);
        if(GUI.Button(new Rect(22,h-54,w-44,40),"돌아가기",button)){singlePage=listing?SinglePage.Choose:SinglePage.Closed;message="";}
        GUI.EndGroup();
    }
    void OnGUI(){
        GUI.skin.font=KoreanText.Font;
        if(Selected==Mode.Menu){
            if(singlePage!=SinglePage.Closed){DrawSinglePopup();return;}
            int choice=CasualMenuUI.Main();if(choice==1)singlePage=SinglePage.Choose;else if(choice==2)StartPvp();else if(choice==3)Application.Quit();return;
        }
    }
}

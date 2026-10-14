using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoadRulesTrial : MonoBehaviour {
    public DriveSession session;
    public static int selection=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadRules4")>=0?1:System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadRulesProtected2")>=0?2:0;
    public static bool Enabled {get{return System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadRulesTrial")>=0;}}
    public static string Description {get{return selection==0?"왕복 2차선 · 비보호 좌회전":selection==1?"왕복 4차선 · 방향별 직좌 동시신호":"왕복 2차선 · 방향별 직좌 동시신호";}}
    public static MapData CreateMap(){
        int lanes=selection==1?4:2;float width=lanes*3.5f;
        var mode=selection==0?LeftTurnMode.Permissive:LeftTurnMode.ProtectedShared;
        var roads=new System.Collections.Generic.List<MapRoad>();
        roads.Add(Road("중앙 도로",width,lanes,new[]{300f,40,300,80,300,200,300,400,300,520,300,560}));
        foreach(float z in new[]{200f,400})roads.Add(Road("교차 도로 "+z,width,lanes,new[]{40f,z,100,z,300,z,500,z,560,z}));
        foreach(float x in new[]{100f,500})roads.Add(Road("연결 도로 "+x,width,lanes,new[]{x,80f,x,200,x,400,x,520}));
        foreach(float z in new[]{80f,520})roads.Add(Road("외곽 도로 "+z,width,lanes,new[]{100f,z,300,z,500,z}));
        var stops=new MapStop[6];for(int i=0;i<6;i++)stops[i]=new MapStop{name="Stop "+(i+1),x=300,z=120+i*65,dx=0,dz=1,width=width};
        return new MapData{source="한국 신호 운영 규칙 시험장 (가상 지도)",width=600,depth=600,roads=roads.ToArray(),buildings=new MapBuilding[0],stops=stops,
            targets=new[]{new MapTarget{name="T1",x=300,z=200,leftTurnMode=mode},new MapTarget{name="T2",x=300,z=400,leftTurnMode=mode}}};
    }
    static MapRoad Road(string id,float width,int lanes,float[] coords){var points=new MapPoint[coords.Length/2];for(int i=0;i<points.Length;i++)points[i]=new MapPoint{x=coords[i*2],z=coords[i*2+1]};return new MapRoad{id=id,width=width,sourceLanes=lanes,points=points};}
    IEnumerator Start(){session.jobs.enabled=false;foreach(var person in session.jobs.people)person.SetActive(false);yield return new WaitForSeconds(.5f);session.assisted.SetActive(true);}
    void Update(){if(Time.timeScale==0)return;int next=Input.GetKeyDown(KeyCode.F6)?0:Input.GetKeyDown(KeyCode.F7)?1:Input.GetKeyDown(KeyCode.F8)?2:-1;if(next>=0){selection=next;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}}
    void OnGUI(){GUI.skin.font=KoreanText.Font;GUI.Box(new Rect(16,Screen.height-162,740,124),GUIContent.none);var style=new GUIStyle(GUI.skin.label){fontSize=16};style.normal.textColor=Color.white;
        GUI.Label(new Rect(28,Screen.height-156,710,27),"한국 도로 규칙 시험장 · "+Description,style);
        GUI.Label(new Rect(28,Screen.height-127,710,27),"F6: 2차선 비보호   F7: 4차선 보호   F8: 2차선 보호   R: 출발점",style);
        GUI.Label(new Rect(28,Screen.height-98,710,27),selection==0?"좌회전: 녹색 + 맞은편 차량 양보 / 적색 좌회전 금지":"좌회전: 녹색 화살표 / 반대편은 정지 · 직진과 공용 차로",style);
        GUI.Label(new Rect(28,Screen.height-69,710,27),"우회전: 적색이면 먼저 일시정지 · 차량·보행자 양보 (보행자는 아직 미구현)",style);
    }
}

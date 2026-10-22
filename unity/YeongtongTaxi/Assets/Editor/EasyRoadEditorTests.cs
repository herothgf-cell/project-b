using System;
using System.Linq;
using UnityEngine;
public static class EasyRoadEditorTests {
 static int count;
 static void Check(bool value,string text){if(!value)throw new Exception("EASY_EDITOR_FAIL "+text);count++;}
 public static void Run(){
  var a=new RoadMapLane{id="in-1",sourceId="road-a",ruleId="junction-a",number=1,points=new[]{P(20,0),P(20,30)},next=new[]{"left","straight"}};
  var b=new RoadMapLane{id="in-2",sourceId="road-a",ruleId="junction-a",number=2,points=new[]{P(24,0),P(24,30)},next=new[]{"straight"}};
  var join=new RoadMapLane{id="join:left",sourceId="road-a",ruleId="junction-a",turn=-1,points=new[]{P(20,30),P(0,32)},next=new string[0]};
  var d=new RoadMapDocument{lanes=new[]{a,b,join}};
  Check(RoadMapEasySelection.PickLane(d,new Vector3(20,0,15),3)==a,"map road selection ignores connectors");
  Check(RoadMapEasySelection.ApproachKey(a)==RoadMapEasySelection.ApproachKey(b),"same road approach groups lanes");
  Check(RoadMapEasySelection.LaneLabel(a).Contains("1차로")&&!RoadMapEasySelection.LaneLabel(a).Contains(a.id),"human lane label hides ids");
  Check(RoadMapEasySelection.Direction(a)=="남쪽 진입","compass heading label");
  var style=new GUIStyle{font=KoreanText.Font,fontSize=16,wordWrap=true,padding=new RectOffset(8,8,4,4)};var rect=new Rect(0,0,180,42);var fit=RoadMapEditorText.Fit(style,"마지막 정상 적용 백업 불러오기",rect);Check(fit.style.CalcHeight(new GUIContent(fit.text),rect.width)<=rect.height,"button text fits height");Check(fit.style.CalcSize(new GUIContent("↖ ↑ ↗")).y<=rect.height,"button glyph height");
  var host=new GameObject("Easy editor defaults");try{var editor=host.AddComponent<RoadMapEditor>();Check(editor.SimpleMode,"simple mode is default");}finally{UnityEngine.Object.DestroyImmediate(host);}
  Debug.Log("EASY_ROAD_EDITOR_TESTS_OK assertions="+count);
 }
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
}

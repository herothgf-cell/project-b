using System;
using System.IO;
using System.Linq;
using UnityEngine;
public static class RoadMapEditorTests {
 static int count;
 static void Check(bool value,string label){if(!value)throw new Exception("ROAD_EDITOR_FAIL "+label);count++;}
 static void Reject(Action action,string label){bool failed=false;try{action();}catch{failed=true;}Check(failed,label);}
 public static void Run(){
  MapCatalog.LockedRegion="eungye";var baseline=MapCatalog.Load();var before=JsonUtility.ToJson(baseline);
  var doc=RoadMapDocument.Create(baseline);Check(doc.lanes.Length==baseline.hdEdges.Length,"import every existing reviewed lane");
  Check(doc.lanes.Select(x=>x.id).Distinct().Count()==doc.lanes.Length,"stable ids");
  var history=new RoadMapEditHistory(doc);var lane=doc.lanes.First(x=>!x.disabled&&x.next.Length>1);string laneId=lane.id;
  history.Commit(doc);lane.width=3.2f;var undone=history.Undo(doc);Check(undone.lanes.First(x=>x.id==laneId).width!=3.2f,"undo document");Check(history.Redo(undone).lanes.First(x=>x.id==laneId).width==3.2f,"redo document");
  var composed=RoadMapComposer.Compose(baseline,doc);Check(JsonUtility.ToJson(baseline)==before,"compose preserves source");Check(composed.hdEdges.Length==baseline.hdEdges.Length,"T1 import no duplicate lanes");
  string dir=Path.GetFullPath("../../artifacts/road-map-editor-tests" );Directory.CreateDirectory(dir);string file=Path.Combine(dir,"draft.json");RoadMapStore.Save(file,doc);var loaded=RoadMapStore.Load(file);Check(loaded.lanes.Length==doc.lanes.Length&&loaded.baseHash==doc.baseHash,"storage roundtrip");
  doc.schema=99;Check(RoadMapValidator.Validate(baseline,doc).Any(x=>x.error),"unsupported schema blocks");doc.schema=1;
  doc.baseHash="wrong";Check(RoadMapValidator.Validate(baseline,doc).Any(x=>x.error),"base hash blocks");doc.baseHash=loaded.baseHash;
  lane.width=-1;Check(RoadMapValidator.Validate(baseline,doc).Any(x=>x.error),"negative width blocks");lane.width=3.5f;
  lane.next=new[]{"missing-lane"};Check(RoadMapValidator.Validate(baseline,doc).Any(x=>x.error),"missing connection blocks");doc=loaded;
  var program=new RoadMapSignalProgram{junctionId=baseline.junctionRules[0].id,stages=new[]{new RoadMapStage{green=12,yellow=3,clearance=2,groups=new[]{"a"}}},groups=new[]{new RoadMapSignalGroup{id="a",axis=0,sign=1,turn=0}}};
  Check(program.Color("a",0)==SignalColor.Green&&program.Color("a",12)==SignalColor.Yellow&&program.Color("a",15)==SignalColor.Red,"signal stage timing");
  program.stages[0].green=-1;doc.signals=new[]{program};Check(RoadMapValidator.Validate(baseline,doc).Any(x=>x.error),"invalid timing blocks");
  File.WriteAllText(file,"broken");Reject(()=>RoadMapStore.Load(file),"corrupt file rejected");
  Check(!RoadMapRuntime.Trial,"batch is not trial");
  Debug.Log("ROAD_MAP_EDITOR_TESTS_OK assertions="+count);
 }
}

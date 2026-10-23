using System;
using System.Linq;
using UnityEngine;
public static class ProjectMapRepairAuthoringTests {
 static float Length(MapPoint[] p){return new AssistedRoadNetwork.Path(p.Select(x=>x.V()).ToArray()).length;}
 public static void Run(){var p=ProjectMapStore.Load(ProjectMapStore.FindRoot(Application.dataPath));var d=p.document.Copy();var map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);var lane=d.lanes.First(l=>map.hdEdges.Any(e=>e.id==l.id&&Length(e.points)<Length(l.points)-1));string id=RoadMapAuthoringOperations.CreateFacility(d,"sign","제한속도",lane.points[0].V(),lane.id);var sign=d.facilities.First(f=>f.id==id);sign.rule=true;sign.speed=17;sign.endAt=Length(lane.points);map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);if(Mathf.Abs(Length(map.hdEdges.First(e=>e.id==lane.id).points)-Length(lane.points))>.1f)throw new Exception("REPAIR_AUTHORING_FAIL explicit rule lane split by automatic repair");if(RoadMapRules.SpeedLimit(map,lane.id,sign.endAt-1)!=17)throw new Exception("REPAIR_AUTHORING_FAIL downstream speed rule lost");Debug.Log("PROJECT_MAP_REPAIR_AUTHORING_TESTS_OK");}
 public static void Build(){Run();EungyeOnlyTests.Run();EungyeOnlyTests.NativeParity();ProjectMapSceneWindowTests.RenderVisual();PrototypeBuild.BuildHybrid();}
}

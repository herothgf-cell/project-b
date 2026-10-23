using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class ProjectMapUnifiedTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("UNIFIED_MAP_FAIL "+message);}
 public static void Run(){
  string root=ProjectMapStore.FindRoot(Application.dataPath),folder=Path.Combine(root,"artifacts/unity-traffic-player");Directory.CreateDirectory(folder);var package=ProjectMapStore.Load(root);string original=ProjectMapStore.Fingerprint(root);var d=package.document.Copy();
  var lane=d.lanes.First(l=>!l.disabled&&l.turn==0&&l.next.Length>1&&l.next.Any(id=>d.lanes.Any(n=>n.id==id&&n.turn==-1&&n.change==0)));
  RoadMapAuthoringOperations.SetTurn(d,lane.id,-1,false);
  string sign=RoadMapAuthoringOperations.CreateFacility(d,"sign","제한속도",lane.points[0].V(),lane.id);var f=d.facilities.First(x=>x.id==sign);f.rule=true;f.speed=17;f.startAt=0;f.endAt=Mathf.Min(5,new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray()).length);f.text="Unity native integration";
  string mark=RoadMapAuthoringOperations.CreateFacility(d,"line","",lane.points[0].V());
  var issues=RoadMapValidator.Validate(package.baseline,d);Check(!issues.Any(i=>i.error),"valid real-map edited fixture: "+string.Join(" / ",issues.Where(i=>i.error).Take(3).Select(i=>i.message)));
  var map=RoadMapComposer.Compose(package.baseline,d);var edge=map.hdEdges.First(e=>e.id==lane.id);Check(edge.next.All(i=>map.hdEdges[i].turn!=-1||map.hdEdges[i].change!=0),"runtime navigation graph removes left turn");Check(RoadMapRules.SpeedLimit(map,lane.id,1)==17,"runtime driving speed rule uses edited sign");
  var render=new ProjectMapAuthoringRenderer();try{render.Initialize(map);Check(render.Root.GetComponentsInChildren<Transform>().Any(t=>t.name=="사용자 시설 "+sign),"native preview displays actual sign mesh");Check(render.Root.GetComponentsInChildren<Transform>().Any(t=>t.name=="사용자 시설 "+mark),"native preview displays road marking mesh");}finally{render.Dispose();}
  var exported=ProjectMapPackage.Create(package.baseline,d);var loaded=ProjectMapPackage.Parse(JsonUtility.ToJson(exported));Check(loaded.document.facilities.Any(x=>x.id==sign),"portable project roundtrip includes new signs");
  d.projectRevision=package.contentHash;File.WriteAllText(Path.Combine(folder,"fixture.json"),d.ToJson());File.WriteAllText(Path.Combine(folder,"expected.json"),JsonUtility.ToJson(new UnityTrafficExpected{lane=lane.id,sign=sign,mark=mark}));Check(ProjectMapStore.Fingerprint(root)==original,"real canonical map untouched");
  Debug.Log("PROJECT_MAP_UNIFIED_TESTS_OK lane="+lane.id);
 }
}

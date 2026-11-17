using System;
using System.Linq;
using UnityEngine;

public static class AuthoredCrosswalkGeometryTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("AUTHORED_CROSSWALK_ENDPOINT: "+message);}
 public static void Run(){
  var effective=typeof(GeneratedCrosswalkGeometry).GetMethod("Effective");Check(effective!=null,"effective authored crossing geometry exists");
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();string before=map.editorDocument.ToJson();
  foreach(string id in new[]{"reviewed:P03:38:0:-1:crosswalk","reviewed:Y05:44:0:1:zebra","reviewed:Y05:44:0:-1:zebra"}){
   var source=map.editorDocument.facilities.Single(f=>f.id==id);var fitted=(RoadMapFacility)effective.Invoke(null,new object[]{map,source});
   Check(fitted.width>source.width+4,"full-road crossing reaches outer traffic lanes: "+id);
   Check(fitted.id==source.id&&fitted.axis==source.axis&&fitted.sign==source.sign&&fitted.yaw==source.yaw,"identity, direction and signal association retained");
   var root=new GameObject("authored crossing render test");try{
    RoadMapFacilities.Build(new RoadMapDocument{facilities=new[]{source}},root.transform,null,map);
    var crossing=root.transform.GetChild(0);Check((crossing.position-fitted.Position).sqrMagnitude<.0001f,"rendered crossing uses effective center");
    Check(crossing.childCount>0,"actual stripe meshes exist");
    foreach(Transform stripe in crossing)Check(Mathf.Abs(stripe.localPosition.x)+stripe.localScale.x*.5f<=fitted.width*.5f+.001f,"stripe mesh stays inside fitted pavement endpoints");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  foreach(string id in new[]{"reviewed:J5057-NW:slip-crosswalk","draft:green31:A:crosswalk","draft:green31:B:crosswalk","reviewed:Y05:west-island:crosswalk"}){
   var source=map.editorDocument.facilities.Single(f=>f.id==id);var fitted=(RoadMapFacility)effective.Invoke(null,new object[]{map,source});
   Check(fitted.width==source.width&&fitted.Position==source.Position,"single-direction island/slip crossing unchanged: "+id);
  }
  var slip=map.editorDocument.facilities.Single(f=>f.id=="reviewed:J5057-NW:slip-crosswalk");
  var detachedMap=new MapData{editorDocument=new RoadMapDocument{facilities=new[]{slip}}};
  var detached=ReviewedCrosswalkGeometry.Build(detachedMap);
  Check(detached.Count==1&&detached[0].InBand(slip.Position,0),"unassigned crossing still excludes paint in its stripe band without junction rules");
  Check(detached[0].reach==0&&!detached[0].JunctionSide(slip.Position,0),"detached island crossing cannot erase a corridor toward an inferred main junction");
  Check(before==map.editorDocument.ToJson(),"authored map and source evidence remain unchanged");
  Debug.Log("AUTHORED_CROSSWALK_ENDPOINT_TEST_OK full-road3/island-slip4/identity/source-preserved");
 }
}

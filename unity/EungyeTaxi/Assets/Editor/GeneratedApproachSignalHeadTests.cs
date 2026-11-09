using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Physical head assignments are document metadata, independent of movement rights.
public static class GeneratedApproachSignalHeadTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception("GENERATED_SIGNAL_HEAD: "+reason);}
 static RoadMapFacility Assignment(string id,int axis,int sign,string type,bool observed=false){return new RoadMapFacility{id=id,kind="signalHead",junctionId="head fixture",axis=axis,sign=sign,type=type,x=50,z=50,rule=false,evidence=new RoadMapEvidence{state=observed?"observed":"game",appearanceConfirmed=observed,url=observed?"fixture appearance evidence":"",month=observed?"2026-05":""}};}
 static TrafficJunction Junction(){return new TrafficJunction{name="head fixture",center=new Vector3(50,0,50),direction=Vector3.forward,leftTurnMode=LeftTurnMode.ProtectedShared,facility=new HybridJunctionRule{id="head fixture",signalMode=2}};}
 static SignalHeadPresentation.Head Generated(MapData map,TrafficJunction j,int axis,int sign){return (SignalHeadPresentation.Head)typeof(SignalHeadPresentation).GetMethod("Generated").Invoke(null,new object[]{map,j,axis,sign});}
 static RoadMapIssue[] Validate(MapData map,RoadMapDocument document){return ((System.Collections.Generic.List<RoadMapIssue>)typeof(SignalHeadPresentation).GetMethod("ValidateAssignments").Invoke(null,new object[]{map,document})).ToArray();}
 static int Add(TrafficSystem system,TrafficJunction j,Vector3 heading){typeof(TrafficSystem).GetMethod("AddApproach",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(system,new object[]{j,heading,5f,5f});return j.lamps.Count-1;}
 public static void Run(){
  Check(typeof(SignalHeadPresentation).GetMethod("Generated")!=null,"generated approaches need explicit physical head metadata resolver");
  Check(typeof(SignalHeadPresentation).GetMethod("ValidateAssignments")!=null,"physical head metadata is validated without changing the baseline schema");
  var three=Assignment("three",0,1,"3구",true);var four=Assignment("four",0,-1,"4구");
  var doc=new RoadMapDocument{facilities=new[]{three,four}};
  var map=new MapData{hdEdges=new HdEdge[0],editorDocument=doc,junctionRules=new[]{new HybridJunctionRule{id="head fixture",signalMode=2}}};var j=Junction();
  var head=Generated(map,j,0,1);Check(head.lamps==3&&head.verified,"matching observed approach gets three apertures and appearance provenance");
  head=Generated(map,j,0,-1);Check(head.lamps==4&&!head.verified,"opposite explicit four remains a game assignment");
  head=Generated(map,j,1,1);Check(head.lamps==4&&!head.verified,"unreviewed cross axis keeps legacy four");
  Check(Validate(map,doc).Length==0,"valid explicit assignments accepted");
  doc.facilities=new[]{three,Assignment("duplicate",0,1,"4구")};Check(Validate(map,doc).Any(i=>i.error),"duplicate approach assignment rejected");
  doc.facilities=new[]{Assignment("invalid type",0,1,"안내")};Check(Validate(map,doc).Any(i=>i.error),"metadata requires explicit three or four");
  doc.facilities=new[]{three};three.rule=true;Check(Validate(map,doc).Any(i=>i.error),"head metadata cannot grant a traffic rule");three.rule=false;
  three.junctionId="unknown";Check(Validate(map,doc).Any(i=>i.error),"unknown junction rejected");three.junctionId=j.name;
  doc.signals=new[]{new RoadMapSignalProgram{junctionId=j.name}};Check(Validate(map,doc).Any(i=>i.error),"authored programs use their existing vehicleSignal format instead");doc.signals=new RoadMapSignalProgram[0];
  doc.facilities=new[]{three,four};
  var host=new GameObject("generated head regression");var metadataRoot=new GameObject("head metadata must not render");
  try {
   // Obtain normal materials through the production builder, then build isolated heads.
   var system=host.AddComponent<TrafficSystem>();system.Build(ProjectMapStore.Load(ProjectMapBuild.Root).Compose(),false);
   typeof(TrafficSystem).GetField("roadMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(system,map);
   int a=Add(system,j,Vector3.forward);var physical=j.lamps[a][0].transform.parent;
   Check(physical.Find("Signal lamp Left")==null&&Mathf.Abs(physical.Find("Signal housing").localScale.x-2.3f)<.001f,"explicit three is physically three with a compact housing");
   head=SignalHeadPresentation.Resolve(map,j,0,1);Check(head.lamps==3&&head.verified&&!head.left,"HUD matches generated three and its evidence");
   Check(system.HasLeftMovement(j,0,1),"physical format does not revoke configured movement permission");
   a=Add(system,j,Vector3.back);physical=j.lamps[a][0].transform.parent;Check(physical.Find("Signal lamp Left")!=null&&Mathf.Abs(physical.Find("Signal housing").localScale.x-2.9f)<.001f,"explicit opposite four retains its aperture");
   var closed=Junction();closed.facility.allowLeft=false;Add(system,closed,Vector3.back);head=SignalHeadPresentation.Resolve(map,closed,0,-1);Check(head.lamps==4&&!head.left&&!system.HasLeftMovement(closed,0,-1),"explicit four with no left remains four without creating permission");
   a=Add(system,j,Vector3.right);Check(j.lamps[a][0].transform.parent.Find("Signal lamp Left")!=null,"unreviewed direction remains physically four");
   RoadMapFacilities.Build(doc,metadataRoot.transform,system);Check(metadataRoot.transform.childCount==0,"head metadata does not create duplicate lamps or poles");
   Check(map.junctionRules[0].allowLeft&&doc.signals.Length==0,"assigning appearance does not author a signal program or change movement rules");
  }finally{UnityEngine.Object.DestroyImmediate(metadataRoot);UnityEngine.Object.DestroyImmediate(host);}
  Debug.Log("GENERATED_SIGNAL_HEAD_OK");
 }
}

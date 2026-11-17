using System;
using System.Linq;
using UnityEngine;

// Restore the two-way bank-school road before its short junction fan-out.
public static class BankApproachR44 {
 const string Head="local:4442:8620:2062:5530", Outer="portal:A222BC004232:0:in";
 const string Evidence="https://map.naver.com/p?c=19,0,0,0,adh&p=j8Ur4TbCTTo_KOSprwLleA,34,-20,80,Float";
 public static ProjectMapPackage Candidate(ProjectMapPackage before){
  if(before.document.revision!=43&&before.document.revision!=44)throw new Exception("R44 requires r43 or its local r44 correction");
  if(before.contentHash!="e0f8ce711e8c1b5dcd814d0bea2c9f7f454d63baa28be4b3d8cf86df37b4d6bb"&&before.contentHash!="13fed27e57dbad1ab7d59f70d00423025d9ba105b3b9a708e12fda87f6f09f53")throw new Exception("Refuse to overwrite an unrelated or user-edited map");
  var doc=before.document.Copy();var inner=doc.lanes.Single(l=>l.id==BankApproachRegression.Inner);
  var lead=doc.lanes.Single(l=>l.id==BankApproachRegression.Lead);var head=doc.lanes.Single(l=>l.id==Head);
  var original=before.baseline.hdEdges.Single(l=>l.id==inner.id);
  var evidence=new RoadMapEvidence{state="observed",month="2026-05",checkedAt="2026-11-17",url=Evidence,appearanceConfirmed=true,ruleConfirmed=true,
   note="Bank-school road is 1+1 upstream of the short intersection fan-out. Remove r38's upstream 2.9m displacement. A 1.4m bulge only inside the short terminal retains distinct straight/left and outer-right lanes while keeping opposing centres over 3m apart. Dimensions are game geometry, not field measurement."};
  var nativePath=new AssistedRoadNetwork.Path(original.points.Select(p=>p.V()).ToArray());
  Y06EastApproachEdit.Assign(inner,Enumerable.Range(0,65).Select(i=>{float t=i/64f;float at=nativePath.length*t;return nativePath.Position(at)-TrafficSystem.Right(nativePath.Tangent(at))*(1.4f*Mathf.Sin(Mathf.PI*t));}),evidence);
  inner.fromNode=lead.toNode;head.next=new[]{lead.id};lead.number=1;
  lead.next=lead.next.Concat(new[]{inner.id}).Distinct().ToArray();lead.evidence=evidence;
  doc.facilities=doc.facilities.Where(f=>!f.id.StartsWith(BankApproachRegression.Lead+":dash:",StringComparison.Ordinal)).ToArray();
  var surface=doc.scene.objects.Single(o=>o.id=="road:polish:"+inner.id);surface.points=inner.points;
  var stop=doc.facilities.Single(f=>f.junctionId=="J1927_5181"&&f.kind=="stopLine"&&f.axis==0&&f.sign==1);
  var direction=Quaternion.Euler(0,stop.yaw,0)*Vector3.forward;
  Vector3 OnPlane(RoadMapLane lane){for(int i=1;i<lane.points.Length;i++){var a=lane.points[i-1].V();var b=lane.points[i].V();float x=Vector3.Dot(a-stop.Position,direction),y=Vector3.Dot(b-stop.Position,direction);if(x<=0&&y>0)return Vector3.Lerp(a,b,-x/(y-x));}throw new Exception("Missing bank stop plane "+lane.id);}
  var outer=doc.lanes.Single(l=>l.id==Outer);var aStop=OnPlane(inner);var bStop=OnPlane(outer);var center=(aStop+bStop)*.5f;
  stop.x=center.x;stop.z=center.z;stop.width=Vector3.Distance(aStop,bStop)+(inner.width+outer.width)*.5f;stop.evidence=evidence;
  doc.revision=44;doc.projectRevision="";doc.savedAt=DateTime.UtcNow.ToString("o");
  var errors=RoadMapValidator.Validate(before.baseline,doc).Where(i=>i.error).ToArray();
  if(errors.Length>0)throw new Exception(string.Join(" / ",errors.Take(10).Select(e=>e.ToString())));
  return ProjectMapPackage.Create(before.baseline,doc);
 }
 public static void Preview(){BankApproachRegression.Verify(Candidate(ProjectMapStore.Load(ProjectMapBuild.Root)));}
 public static void CorrectTerminal(){
  bool reproduced=false;try{BankApproachRegression.Run();}catch(Exception e){if(!e.Message.Contains("same-direction stop lanes too close"))throw;reproduced=true;Debug.Log("BANK_STOP_RED_CONFIRMED "+e.Message);}
  if(!reproduced)throw new Exception("Expected the narrow-terminal regression before correction");Apply();
 }
 public static void Apply(){var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var candidate=Candidate(ProjectMapStore.Load(root));BankApproachRegression.Verify(candidate);ProjectMapStore.Save(root,candidate,token);ProjectMapBuild.Prepare();Debug.Log("BANK_R44_APPLIED "+candidate.contentHash);}
 public static void BuildVerified(){BankApproachRegression.Run();ConfirmedCorridorsR43.Verify();RoadContinuityAudit.Run();JunctionRouteFeedbackTests.VerifyMigration();PrototypeBuild.BuildHybrid();}
}

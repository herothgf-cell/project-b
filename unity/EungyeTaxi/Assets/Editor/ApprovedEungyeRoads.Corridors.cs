using System;
using System.Linq;
using UnityEngine;

public static partial class ApprovedEungyeRoads {
 const string WestGate="reviewed:r47:P51";

 static void NorthTransition(RoadMapDocument doc){
  Vector3 At(float z,int slot)=>V(771.65f+(z-105)*.044f+slot*3.75f,z);
  Mask(doc,"A",At(106,4),At(207,4),33);
  for(int slot=1;slot<=6;slot++){
   if(slot!=3&&slot!=4)Dashed(doc,"A:"+slot,At(106,slot),At(207,slot));
  }
  Stroke(doc,"A:median:south",At(126,4),At(151,4),true);
  for(int i=0;i<16;i++){
   Vector3 Median(float t)=>Vector3.Lerp(At(151+31*t,4),At(151+31*t,3),Mathf.SmoothStep(0,1,t));
   Stroke(doc,"A:median:taper:"+i,Median(i/16f),Median((i+1)/16f),true);
  }
  Stroke(doc,"A:median:north",At(182,3),At(207,3),true);
  Dashed(doc,"A:west-inner",At(106,3),At(182,3));
  Dashed(doc,"A:east-inner",At(151,4),At(207,4));
  Stroke(doc,"A:edge:west",At(106,0),At(207,0));
  Stroke(doc,"A:edge:east",At(106,7),At(207,7));

  // Fill the 7.6m space between the two northbound centres, then feed the
  // existing J7917 middle lane. Both original through connections remain.
  var inner=Lane(doc,"local:7887:1047:7906:1473");
  var outer=Lane(doc,"local:7963:1044:7982:1469");
  var connector=Lane(doc,"join:reviewed:J7917:entry:12");
  var target=Lane(doc,connector.next.Single());
  var path=PathOf(outer);var tail=Split(doc,outer,4);
  var a=tail.points[0].V();var mid=inner.points.Last().V()+TrafficSystem.Right(path.Tangent(path.length))*3.75f;
  var added=Branch(doc,"A:north-through",outer,connector,Cubic(a,a+Vector3.forward*12,mid-Vector3.forward*12,mid,22).ToArray());
  added.number=2;
  added.toNode=Prefix+"A:north-through:end";
  connector.fromNode=added.toNode;
  Y06EastApproachEdit.Reconnect(connector,added,target,Evidence);
  tail.next=tail.next.Where(n=>n!=connector.id).ToArray();
  Surface(doc,"A:north-through",added.points.Select(p=>p.V()),3.5f);
 }

 static void WestTransition(RoadMapDocument doc){
  Vector3 At(float x,float offset)=>V(x,87.2f-(x-660)*.0035f+offset);
  Mask(doc,"B",At(647,0),At(766,0),17.5f);
  Dashed(doc,"B:west-divider",At(686,2),At(729,2));
  Dashed(doc,"B:east-divider",At(730,-2),At(765,-2));
  Stroke(doc,"B:median:west",At(686,-1.75f),At(710,-1.75f),true);
  SmoothMedian(doc,"B",x=>At(x,Mathf.SmoothStep(-1.75f,1.75f,Mathf.InverseLerp(710,746,x))),710,746);
  Stroke(doc,"B:median:east",At(746,1.75f),At(766,1.75f),true);
  Stroke(doc,"B:edge:north",At(687,5.5f),At(765,5.5f));
  Stroke(doc,"B:edge:south",At(687,-5.5f),At(765,-5.5f));

  var marker=Feature("B:gate",ReviewedMidblockControl.Kind,V(660,87.2f),Vector3.right,13.6f,1);
  marker.type=ReviewedMidblockControl.Type;marker.junctionId=WestGate;Add(doc,marker);
  string[] oldIds={"reviewed:P15:WB:crosswalk","reviewed:r42:P51:crosswalk-east","reviewed:r42:P51:crosswalk-west","reviewed:P15:WB:stop"};
  doc.facilities=doc.facilities.Where(f=>!oldIds.Contains(f.id)).ToArray();
  var sw=V(649,78.3f);var nw=V(649,96.2f);var se=V(671,78.3f);var ne=V(671,96.2f);

  var walks=new[]{Crossing(doc,"B:walk-west",sw,nw,2.6f),Crossing(doc,"B:walk-east",ne,se,2.6f),
   Crossing(doc,"B:walk-north",V(656.1f,99.2f),V(664.4f,99.2f),2.4f),
   Crossing(doc,"B:walk-south",V(665.1f,75.1f),V(655.5f,75.1f),2.4f),
   Crossing(doc,"B:walk-diagonal-a",sw,ne,1.5f),Crossing(doc,"B:walk-diagonal-b",se,nw,1.5f)};
  var groups=walks.Select((f,i)=>{
   f.junctionId=WestGate;f.axis=i<2?0:1;f.sign=i%2==0?1:-1;
   var across=Quaternion.Euler(0,f.yaw,0)*Vector3.right;
   Landing(doc,"B:walk-pad:"+i+":a",f.Position-across*f.width*.5f,-across,1.75f);
   Landing(doc,"B:walk-pad:"+i+":b",f.Position+across*f.width*.5f,across,1.75f);
   if(i<4)foreach(int side in new[]{-1,1}){
    var position=f.Position+across*side*(f.width*.5f+1.1f);
    var head=Feature("B:walk-head:"+i+":"+side,"pedestrianSignal",position,across*side,.65f,1);
    head.junctionId=WestGate;head.axis=f.axis;head.sign=f.sign;head.groupId=f.id;head.targetId=f.id;head.height=2.3f;
    Add(doc,head);
   }
   return new RoadMapSignalGroup{id=f.id,pedestrian=true,crosswalkId=f.id,axis=f.axis,sign=f.sign};
  }).ToList();
  var stage=new RoadMapStage{name="동시 보행",green=27,yellow=0,clearance=3,groups=groups.Select(g=>g.id).ToArray()};
  var stages=new System.Collections.Generic.List<RoadMapStage>();
  for(int axis=0;axis<2;axis++)foreach(int sign in new[]{1,-1}){
   var heading=(axis==0?Vector3.right:Vector3.back)*sign;
   var p=marker.Position-heading*14+TrafficSystem.Right(heading)*2.8f;
   string label="B:"+axis+":"+sign;
   var stop=Feature(label+":stop","stopLine",p,heading,5.5f,.34f);
   stop.axis=axis;stop.sign=sign;stop.junctionId=WestGate;Add(doc,stop);
   var ids=new System.Collections.Generic.List<string>();
   foreach(int turn in new[]{0,-1,1}){
    string id=Prefix+label+":"+turn;ids.Add(id);
    groups.Add(new RoadMapSignalGroup{id=id,axis=axis,sign=sign,turn=turn});
   }
   var head=Feature(label+":head","vehicleSignal",p+TrafficSystem.Right(heading)*3.4f,heading,2.3f,1);
   head.junctionId=WestGate;head.axis=axis;head.sign=sign;head.groupId=ids[0];head.targetId=stop.id;head.type="4구";
   Add(doc,head);stages.Add(new RoadMapStage{name="차량 "+label,green=12,groups=ids.ToArray()});
  }
  stages.Add(stage);doc.signals=doc.signals.Append(new RoadMapSignalProgram{junctionId=WestGate,groups=groups.ToArray(),stages=stages.ToArray()}).ToArray();
  Lane(doc,"join:reviewed:r42:P51:through").ruleId=WestGate;
  Lane(doc,"join:reviewed:P15:WB:through").ruleId=WestGate;
  foreach(var lane in doc.lanes.Where(l=>l.id.StartsWith("join:reviewed:r42:P51:",StringComparison.Ordinal)||l.id.StartsWith("join:reviewed:P15:WB:",StringComparison.Ordinal)))
   lane.ruleId=WestGate;
 }

 static void SmoothMedian(RoadMapDocument doc,string id,Func<float,Vector3> at,float start,float end){
  int sections=Mathf.CeilToInt((end-start)/2);
  for(int i=0;i<sections;i++)Stroke(doc,id+":median:curve:"+i,at(Mathf.Lerp(start,end,(float)i/sections)),at(Mathf.Lerp(start,end,(i+1f)/sections)),true);
 }

 static void ParkingLoop(RoadMapDocument doc){
  Vector3 At(float x,float offset)=>V(x,348-(x-581)*.226f+offset);
  Mask(doc,"C",At(582,0),At(710,0),13.6f);
  Dashed(doc,"C:westbound",At(587,1.72f),At(647,1.72f));
  Dashed(doc,"C:eastbound",At(647,-1.72f),At(707,-1.72f));
  SmoothMedian(doc,"C",x=>At(x,Mathf.SmoothStep(-1.72f,1.72f,Mathf.InverseLerp(632,660,x))),588,707);
  for(int sign=-1;sign<=1;sign+=2)Stroke(doc,"C:edge:"+sign,At(588,sign*5.2f),At(706,sign*5.2f));
  var eastern=At(590.2f,0);
  var across=V(.226f,1).normalized;
  CornerCrossing(doc,"C:east",eastern-across*5.75f,eastern+across*8.2f);
  CornerCrossing(doc,"C:south",V(569.1f,339.9f),V(581.6f,339.9f));

  var west=Lane(doc,"join:local:7241:2687:6592:3354:0");
  var target=Lane(doc,"reviewed:P20:left:part:1");
  var path=PathOf(target);
  var tail=Split(doc,target,Mathf.Min(path.length-10,63));
  var start=west.points.Last().V();
  var end=tail.points[0].V();
  var lead=V(634.8f,335.1f);
  var points=Cubic(start,start+Vector3.left*5,lead+V(6,-1.36f),lead,14)
   .Concat(Cubic(lead,lead+V(-21,4.75f),end+Vector3.forward*12,end,28).Skip(1)).ToArray();
  var branch=Branch(doc,"C:west-inner",west,tail,points);
  Surface(doc,"C:west-inner",points,3.4f);
  ReviewedJunctionMap.TrimSlipSidewalk(doc,branch,1.65f);
 }
 static void CornerCrossing(RoadMapDocument doc,string id,Vector3 a,Vector3 b){
  var side=(b-a).normalized;
  Landing(doc,id+":a",a,-side);Landing(doc,id+":b",b,side);
  Crossing(doc,id+":crosswalk",a,b,2.4f);
 }

 static void CommercialStreet(RoadMapDocument doc){
  Vector3 At(float x,float offset)=>V(x,257.6f-(x-575)*.005f+offset);
  Mask(doc,"D",At(587,0),At(708,0),16.2f);
  float Offset(float x)=>Mathf.SmoothStep(1.75f,-1.75f,Mathf.InverseLerp(590,616,x));
  SmoothMedian(doc,"D",x=>At(x,Offset(x)),590,644);
  Stroke(doc,"D:median:east",At(652,-1.75f),At(708,-1.75f),true);
  Dashed(doc,"D:west",At(612,1.8f),At(644,1.8f));
  Dashed(doc,"D:west-after",At(652,1.8f),At(708,1.8f));
  Dashed(doc,"D:east",At(587,-1.8f),At(614,-1.8f));
  for(int sign=-1;sign<=1;sign+=2)Stroke(doc,"D:edge:"+sign,At(587,sign*5.5f),At(708,sign*5.5f));

  const string eastOuter="local:5868:2510:7086:2503",east="local:5868:2554:7086:2547";
  var inner=Lane(doc,east+":part:2");
  var outer=Lane(doc,eastOuter);
  var retired=doc.lanes.Where(l=>l.id==eastOuter+":part:1"||l.id==eastOuter+":part:2"||
   l.id=="change:"+east+":"+eastOuter).ToArray();
  foreach(var lane in retired)lane.disabled=true;
  var ids=retired.Select(l=>l.id).ToArray();
  doc.facilities=doc.facilities.Where(f=>!ids.Contains(f.laneId)).ToArray();
  foreach(var lane in doc.lanes.Where(l=>!l.disabled&&l.next.Any(ids.Contains)))
   lane.next=lane.next.Where(n=>!ids.Contains(n)).ToArray();
  var exit=Lane(doc,"join:"+eastOuter+":0");
  inner.next=inner.next.Append(exit.id).ToArray();
  Y06EastApproachEdit.Reconnect(exit,inner,Lane(doc,exit.next.Single()),Evidence);

  var west=Lane(doc,"local:7087:2635:5869:2642:part:1");
  var westTail=Lane(doc,west.next.Single());
  var path=PathOf(westTail);
  var tail=Split(doc,westTail,path.length-9);
  var start=west.points.Last().V();
  var finish=tail.points[0].V();
  var middle=At(677,0.02f);
  var runout=At(615,.02f);
  var points=Cubic(start,start+Vector3.left*9,middle+Vector3.right*10,middle,16)
   .Concat(new[]{runout}).Concat(Cubic(runout,runout+Vector3.left*5,finish+Vector3.right*6,finish,13).Skip(1)).ToArray();
  var branch=Branch(doc,"D:west-inner",west,tail,points);
  Surface(doc,"D:west-inner",branch.points.Select(p=>p.V()),3.4f);
  var stop=doc.facilities.Single(f=>f.id=="observed:commercial-midblock:west-stop");
  stop.width=7.1f;stop.z=259.2f;
 }
}

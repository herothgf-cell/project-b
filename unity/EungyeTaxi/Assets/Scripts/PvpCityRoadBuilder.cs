using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Fictional A2 streets use the native directed lane/turn graph and signal rules.
// End tangents remain perpendicular at junctions; curves live between approaches.
public sealed class PvpCityRoadBuilder {
 sealed class Node {public string id;public Vector3 p;public bool signal,permissive;}
 sealed class Lane {public Node from,to;public int first,last,number,count;public int[] gates=new int[0],targets=new int[0];public float width;public Vector3 direction;}
 readonly Dictionary<string,Node> nodes=new Dictionary<string,Node>();
 readonly List<Lane> lanes=new List<Lane>();
 readonly List<RoadMapFacility> signs=new List<RoadMapFacility>();
 public readonly List<MapRoad> roads=new List<MapRoad>();
 public readonly List<HdEdge> edges=new List<HdEdge>();
 public readonly List<HdMark> marks=new List<HdMark>();
 public readonly List<HybridJunctionRule> rules=new List<HybridJunctionRule>();
 static readonly float[] X={200,733,1267,1800,2333,2867,3400},Z={200,600,1000,1400,1800,2200,2600};
 static string Key(int x,int z){return x+":"+z;}
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 Node Add(string id,Vector3 p,bool signal=false,bool permissive=false){var n=new Node{id=id,p=p,signal=signal,permissive=permissive};nodes.Add(id,n);return n;}
 public PvpCityRoadBuilder(){
  for(int z=0;z<7;z++)for(int x=0;x<7;x++)Add(Key(x,z),new Vector3(X[x]+(z%2==1?32*(x%3-1):0),0,Z[z]+(x%2==1?25*(z%3-1):0)),x>0&&x<6&&z>0&&z<6&&(z==3||x%2==0||z%2==0),z!=3&&(x==2&&z==1||x==4&&z==5));
  for(int z=0;z<7;z++)for(int x=0;x<6;x++)Street(nodes[Key(x,z)],nodes[Key(x+1,z)],z==3?8:z%2==0?4:2,Vector3.right,z%2==1&&z!=3?(x%2==0?28:-28):0);
  for(int x=0;x<7;x++)for(int z=0;z<6;z++){
   var a=nodes[Key(x,z)];var b=nodes[Key(x,z+1)];int count=x%2==0||x==3?4:2;
   if(z%2==0&&x<6){
    string block="alley:"+(x/2)+":"+(z/2);
    if(x%2==0){var entry=Add(block+":entry",Vector3.Lerp(a.p,b.p,.25f));var exit=Add(block+":exit",Vector3.Lerp(a.p,b.p,.75f));Street(a,entry,count,Vector3.forward);Street(entry,exit,count,Vector3.forward);Street(exit,b,count,Vector3.forward);}
    else{var tee=Add(block+":east",Vector3.Lerp(a.p,b.p,.5f));Street(a,tee,count,Vector3.forward);Street(tee,b,count,Vector3.forward);}
   }else Street(a,b,count,Vector3.forward,x%2==1?(z%2==0?24:-24):0);
  }
  for(int z=0;z<3;z++)for(int x=0;x<3;x++){
   string id="alley:"+x+":"+z;var entry=nodes[id+":entry"];var exit=nodes[id+":exit"];var east=nodes[id+":east"];
   float innerX=entry.p.x+175+(x+z)%2*35;
   var lower=Add(id+":lower",new Vector3(innerX,0,entry.p.z));var upper=Add(id+":upper",new Vector3(innerX,0,exit.p.z));var tee=Add(id+":tee",new Vector3(innerX,0,east.p.z));
   Street(entry,lower,2,Vector3.right);Street(lower,tee,2,Vector3.forward);Street(tee,upper,2,Vector3.forward);Street(upper,exit,2,Vector3.left);Street(tee,east,2,Vector3.right,(x+z)%2==0?15:-15);
  }
  foreach(var node in nodes.Values)Junction(node);
 }
 static AssistedRoadNetwork.Path Path(MapPoint[] points){return new AssistedRoadNetwork.Path(points.Select(p=>p.V()).ToArray());}
 public static MapPoint[] Slice(AssistedRoadNetwork.Path path,float a,float b){var result=new List<MapPoint>{P(path.Position(a))};float at=0;for(int i=1;i<path.points.Length;i++){at+=Vector3.Distance(path.points[i-1],path.points[i]);if(at>a+.01f&&at<b-.01f)result.Add(P(path.points[i]));}result.Add(P(path.Position(b)));return result.ToArray();}
 static MapPoint[] Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int count=24){var p=new MapPoint[count+1];for(int i=0;i<=count;i++){float t=(float)i/count,u=1-t;p[i]=P(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}return p;}
 static MapPoint[] Centre(Vector3 a,Vector3 b,Vector3 d,float bend){
  float length=Vector3.Dot(b-a,d);if(length<85)throw new InvalidOperationException("A2 road too short");
  var start=a+d*42;var end=b-d*42;float middleLength=length-84;var points=new List<MapPoint>{P(a),P(start)};
  if(Mathf.Abs(bend)<.1f)points.AddRange(Bezier(start,start+d*middleLength*.35f,end-d*middleLength*.35f,end,16).Skip(1));
  else{var mid=(start+end)*.5f+TrafficSystem.Right(d)*bend;points.AddRange(Bezier(start,start+d*middleLength*.23f,mid-d*middleLength*.18f,mid,16).Skip(1));points.AddRange(Bezier(mid,mid+d*middleLength*.18f,end-d*middleLength*.23f,end,16).Skip(1));}
  points.Add(P(b));return points.ToArray();
 }
 void Street(Node a,Node b,int count,Vector3 direction,float bend=0){
  string id=a.id+"-"+b.id,roadId="city-road:"+(a.id.StartsWith("alley:")||b.id.StartsWith("alley:")?"alley:":"")+id;var centre=Centre(a.p,b.p,direction,bend);var path=Path(centre);
  string name=count==8?"중앙역 대로":count==4?"도시 순환·연결도로":roadId.Contains("alley:")?"생활 골목":"주거 곡선도로";
  roads.Add(new MapRoad{id=roadId,name=name,width=count*3.5f,lanes=count,sourceLanes=count,points=centre});
  marks.Add(new HdMark{id="city-center:"+id,kind="501",type="111",points=Slice(path,22,path.length-22)});
  for(int side=-1;side<=1;side+=2)for(int line=1;line<count/2;line++)marks.Add(new HdMark{id="city-divider:"+id+":"+side+":"+line,kind="503",type="212",points=Offset(path,22,path.length-22,side*line*3.5f)});
  for(int sign=-1;sign<=1;sign+=2){
   var from=sign==1?a:b;var to=sign==1?b:a;var d=direction*sign;var directed=Path(sign==1?centre:centre.Reverse().ToArray());var family=new List<Lane>();
   for(int number=1;number<=count/2;number++){
    string laneId="city:"+from.id+">"+to.id+(number==count/2?"":":lane:"+number);var lanePath=Path(Offset(directed,34,directed.length-34,(number-.5f)*3.5f));int first=edges.Count;var lane=new Lane{from=from,to=to,first=first,last=first,number=number,count=count/2,width=count*3.5f,direction=d};
    bool change=count>2&&lanePath.length>170;
    if(change){
     // Three separated windows allow a fourth-lane driver to prepare a left
     // turn before this junction, without a detour through another block.
     int windows=count==8?3:1;var boundaries=new List<float>{0};
     for(int window=0;window<windows;window++){boundaries.Add(windows==1?30:10+window*50);boundaries.Add(windows==1?80:50+window*50);}boundaries.Add(lanePath.length);
     lane.gates=new int[windows];lane.targets=new int[windows];
     for(int part=0;part<boundaries.Count-1;part++){
      bool last=part==boundaries.Count-2;string partId=part==0?laneId+":head":last?laneId:laneId+":part:"+part;
      string startNode=part==0?laneId+":start":laneId+":boundary:"+part,endNode=last?laneId+":end":laneId+":boundary:"+(part+1);
      edges.Add(Edge(partId,roadId,name,startNode,endNode,number,Slice(lanePath,boundaries[part],boundaries[part+1]),last?new int[0]:new[]{first+part+1}));
     }
     lane.last=edges.Count-1;for(int window=0;window<windows;window++){lane.gates[window]=first+window*2;lane.targets[window]=first+window*2+2;}
    }else edges.Add(Edge(laneId,roadId,name,laneId+":start",laneId+":end",number,lanePath.points.Select(P).ToArray(),new int[0]));
    edges[lane.last].ruleId="city-junction:"+to.id;lanes.Add(lane);family.Add(lane);
   }
   foreach(var lane in family)foreach(var other in family.Where(l=>Mathf.Abs(l.number-lane.number)==1))for(int window=0;window<lane.gates.Length;window++){
    var head=edges[lane.gates[window]];var tail=edges[other.targets[window]];var begin=head.points.Last().V();var end=tail.points[0].V();var heading=(head.points.Last().V()-head.points[head.points.Length-2].V()).normalized;var finish=(tail.points[1].V()-tail.points[0].V()).normalized;int changeIndex=edges.Count;float handle=Vector3.Distance(begin,end)*.36f;
    var change=Edge(head.id+":change:"+other.number,roadId,name,head.toNode,tail.fromNode,other.number,Bezier(begin,begin+heading*handle,end-finish*handle,end,32),new[]{other.targets[window]});change.change=other.number-lane.number;edges.Add(change);head.next=head.next.Concat(new[]{changeIndex}).ToArray();
   }
  }
 }
 static MapPoint[] Offset(AssistedRoadNetwork.Path p,float start,float end,float offset){var points=new List<MapPoint>();for(float at=start;at<end;at+=4)points.Add(P(p.Position(at)+TrafficSystem.Right(p.Tangent(at))*offset));points.Add(P(p.Position(end)+TrafficSystem.Right(p.Tangent(end))*offset));return points.ToArray();}
 static HdEdge Edge(string id,string source,string name,string from,string to,int number,MapPoint[] points,int[] next){return new HdEdge{id=id,sourceId=source,roadName=name,fromNode=from,toNode=to,number=number,status="authored-fictional",points=points,next=next};}
 void Junction(Node node){
  string id="city-junction:"+node.id;var incoming=lanes.Where(l=>l.to==node).ToArray();var outgoing=lanes.Where(l=>l.from==node).ToArray();
  var approaches=incoming.GroupBy(l=>l.from.id).Select(g=>new HybridApproach{dx=g.First().direction.x,dz=g.First().direction.z,width=g.Max(l=>l.width)}).ToArray();
  rules.Add(new HybridJunctionRule{id=id,x=node.p.x,z=node.p.z,dx=0,dz=1,signalMode=node.signal?(node.permissive?1:2):0,stopLine=true,crosswalk=node.signal,approaches=approaches,status="authored-fictional",facilityStatus="authored-fictional",facilityEvidence="Approved fictional PvP A2 road design"});
  foreach(var a in incoming){var links=new List<int>();bool straight=outgoing.Any(b=>b.to!=a.from&&Vector3.Dot(a.direction,b.direction)>.9f);
   foreach(var b in outgoing){
    bool reverse=b.to==a.from;int turn=reverse?UturnMovement.Turn:Vector3.Dot(a.direction,b.direction)>.9f?0:Mathf.RoundToInt(Mathf.Sign(Vector3.Cross(a.direction,b.direction).y));
    if(reverse){if(!(node.id=="2:3"||node.id=="4:3")||a.count!=4||a.number!=1||b.number!=3)continue;}
    else if(turn==0){if(b.number!=Mathf.Min(a.number,b.count))continue;}
    else if(turn<0){if(straight&&a.number!=1||b.number!=Mathf.Min(a.number,b.count))continue;}
    else if(straight&&a.number!=a.count||b.number!=Mathf.Max(1,b.count-(a.count-a.number)))continue;
    var from=edges[a.last];var to=edges[b.first];var start=from.points.Last().V();var end=to.points[0].V();MapPoint[] points;
    if(reverse){points=UturnMovement.Geometry(RoadMapLane.From(from,edges.ToArray()),RoadMapLane.From(to,edges.ToArray())).Select(P).ToArray();int axis=Mathf.Abs(a.direction.z)>.7f?0:1;int sign=axis==0?(a.direction.z>0?1:-1):(a.direction.x>0?1:-1);var pos=start+TrafficSystem.Right(a.direction)*(a.width*.5f+1);
     signs.Add(new RoadMapFacility{id=from.id+":uturn-condition",kind="sign",type=UturnMovement.LeftOrPedestrianGreen,laneId=from.id,junctionId=id,axis=axis,sign=sign,rule=true,x=pos.x,z=pos.z,yaw=Quaternion.LookRotation(a.direction).eulerAngles.y,text="중앙대로 지정 차로",evidence=new RoadMapEvidence{state="game",note="Fictional A2 authored movement"}});
     var line=node.p-a.direction*34+TrafficSystem.Right(a.direction)*(a.width*.25f);signs.Add(new RoadMapFacility{id=from.id+":stop-line",kind="stopLine",junctionId=id,axis=axis,sign=sign,x=line.x,z=line.z,yaw=Quaternion.LookRotation(a.direction).eulerAngles.y,width=a.width*.5f-.3f,length=.4f,text=""});
    }else{float handle=Vector3.Distance(start,end)*.45f;points=Bezier(start,start+a.direction*handle,end-b.direction*handle,end,32);}
    links.Add(edges.Count);var join=Edge("join:"+id+":"+a.last+":"+b.first,id,"교차로",from.toNode,to.fromNode,b.number,points,new[]{b.first});join.ruleId=id;join.turn=turn;edges.Add(join);
    roads.Add(new MapRoad{id="city-join:"+a.last+":"+b.first,name="교차로",width=3.5f,lanes=1,sourceLanes=1,oneWay=1,suppressPaint=true,points=points});
   }edges[a.last].next=links.ToArray();
  }
 }
 public void AttachDocument(MapData map){map.editorDocument=new RoadMapDocument{id="pvp-city-a2-native",name="A2 가상 교통시설",mapId=map.mapId,lanes=map.hdEdges.Select(e=>RoadMapLane.From(e,map.hdEdges)).ToArray(),facilities=signs.ToArray()};}
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Fictional A3 streets use the native directed lane/turn graph and signal rules.
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
 // Collectors use a sparse, staggered frame. Central axes stay legible while
 // districts have independent internal topology rather than cloned block loops.
 public PvpCityRoadBuilder(){
  float[] x={200,1267,1800,2333,3400},z={200,1000,1400,1800,2600};
  for(int row=0;row<5;row++)for(int col=0;col<5;col++){
   var p=new Vector3(x[col],0,z[row]);
   if(col==1||col==3)p.x+=(row==1?-46:row==3?52:row==0?20:-18)*(col==1?1:-1);
   if(row==1||row==3)p.z+=(col==1?32:col==3?-38:0);
   Add(Key(col,row),p,col>0&&col<4&&row>0&&row<4,(col==1&&row==1)||(col==3&&row==3));
  }
  for(int row=0;row<5;row++)for(int col=0;col<4;col++){
   var a=nodes[Key(col,row)];var b=nodes[Key(col+1,row)];var m=Add("h:"+col+":"+row,Vector3.Lerp(a.p,b.p,.5f),row==2);
   Street(a,m,row==2?8:4,(m.p-a.p).normalized,row!=2&&Vector3.Distance(a.p,m.p)>300?10:0);Street(m,b,row==2?8:4,(b.p-m.p).normalized,row!=2&&Vector3.Distance(m.p,b.p)>300?-10:0);
  }
  for(int col=0;col<5;col++)for(int row=0;row<4;row++){
   var a=nodes[Key(col,row)];var b=nodes[Key(col,row+1)];var m=Add("v:"+col+":"+row,Vector3.Lerp(a.p,b.p,.5f),col==2);
   Street(a,m,col==2?6:4,(m.p-a.p).normalized,col!=2&&Vector3.Distance(a.p,m.p)>300?10:0);Street(m,b,col==2?6:4,(b.p-m.p).normalized,col!=2&&Vector3.Distance(m.p,b.p)>300?-10:0);
  }
  for(int row=0;row<4;row++)for(int col=0;col<4;col++){
   int dx=col==0?0:col==3?2:1,dz=row==0?0:row==3?2:1,district=dz*3+dx;
   District(col,row,district);
  }
  foreach(var node in nodes.Values)Junction(node);
 }
 void District(int col,int row,int district){
  string prefix="alley:a3:"+district+":"+col+":"+row+":";
  var sw=nodes[Key(col,row)].p;var se=nodes[Key(col+1,row)].p;var nw=nodes[Key(col,row+1)].p;var ne=nodes[Key(col+1,row+1)].p;
  Func<string,float,float,Node> point=(id,u,v)=>Add(prefix+id,Vector3.Lerp(Vector3.Lerp(sw,se,u),Vector3.Lerp(nw,ne,u),v));
  // Each ring is planar and convex. The different corner offsets, diagonals,
  // exits and split tees below are authored by district, not random jitter.
  float[][] corners={
   new[]{.25f,.24f,.73f,.18f,.78f,.74f,.23f,.79f}, // park: broad lake perimeter
   new[]{.27f,.23f,.77f,.31f,.68f,.78f,.20f,.67f}, // market: staggered maze
   new[]{.23f,.29f,.75f,.19f,.80f,.71f,.30f,.79f}, // terminal: separated entrances
   new[]{.22f,.30f,.74f,.19f,.80f,.67f,.29f,.78f}, // west: offset diagonal stems
   new[]{.24f,.27f,.77f,.21f,.70f,.79f,.19f,.69f}, // station: asymmetric back loop
   new[]{.20f,.20f,.73f,.31f,.80f,.78f,.28f,.70f}, // offices: diagonal shortcut
   new[]{.19f,.22f,.76f,.31f,.70f,.78f,.30f,.69f}, // northwest: fan
   new[]{.28f,.20f,.78f,.28f,.70f,.78f,.20f,.70f}, // hospital: staggered tees
   new[]{.21f,.28f,.72f,.19f,.80f,.70f,.28f,.80f}  // university: sweeping outer route
  };
  var q=corners[district];var a=point("a",q[0],q[1]);var b=point("b",q[2],q[3]);var c=point("c",q[4],q[5]);var d=point("d",q[6],q[7]);
  Action<Node,Node,float,int> link=(from,to,bend,travel)=>Street(from,to,2,(to.p-from.p).normalized,bend,travel);
  var south=nodes["h:"+col+":"+row];var north=nodes["h:"+col+":"+(row+1)];var west=nodes["v:"+col+":"+row];var east=nodes["v:"+(col+1)+":"+row];
  // The central axes divide some districts. Their cells intentionally use
  // different topology, rather than stamping the same loop on both sides.
  bool stationRear=district==4&&row==1&&col==2,stationNorth=district==4&&row==2&&col==1,stationFan=district==4&&row==2&&col==2;
  bool hospitalRear=district==7&&col==2,westNorth=district==3&&row==2,eastSouth=district==5&&row==1;
  Node stationTee=null;
  if(stationRear){stationTee=point("station-rear-tee",(q[0]+q[2])*.5f,(q[1]+q[3])*.5f);link(a,stationTee,0,0);link(stationTee,b,0,0);link(stationTee,c,0,0);}
  else if((district==7&&!hospitalRear)||(district==3&&!westNorth)){
   var tee=point("tee",(q[0]+q[2])*.5f,(q[1]+q[3])*.5f);link(a,tee,0,0);link(tee,b,0,0);link(tee,d,0,0);
  }else link(a,b,district==0?18:district==8?-16:0,0);
  if(stationNorth||hospitalRear){
   var tee=point(stationNorth?"station-north-tee":"hospital-rear-tee",(q[4]+q[6])*.5f,(q[5]+q[7])*.5f);
   link(c,tee,0,0);link(tee,d,0,0);link(tee,stationNorth?a:b,0,0);
  }else if(district==2){
   var rear=point("rear",(q[4]+q[6])*.5f,(q[5]+q[7])*.5f);link(c,rear,0,0);link(rear,d,0,0);link(rear,b,0,0);
  }else if(!westNorth)link(c,d,district==6?16:district==8?18:0,0);
  if(district==1){
   // Both market halves retain the same short one-way return contract, but
   // the east has an offset chicane and extra exit instead of a cloned cross.
   var down=point("southbound",(q[6]+q[0])*.5f,(q[7]+q[1])*.5f);var up=point("northbound",(q[2]+q[4])*.5f,(q[3]+q[5])*.5f);
   link(d,down,0,1);link(down,a,0,1);link(b,up,0,1);link(up,c,0,1);
   if(col==2){var chicane=point("market-east-chicane",.48f,.58f);link(down,chicane,0,0);link(chicane,up,0,0);link(chicane,b,0,0);}
   else link(down,up,0,0);
  }else{
   if(westNorth){var tee=point("west-north-tee",(q[2]+q[4])*.5f,(q[3]+q[5])*.5f);link(b,tee,0,0);link(tee,c,0,0);link(tee,a,0,0);}
   else if(!stationRear&&!stationFan&&!eastSouth)link(b,c,0,0);
   if(!stationNorth&&!stationFan&&!hospitalRear)link(d,a,0,0);
  }
  link(south,a,0,0);link(north,c,0,0);link(west,d,0,0);link(east,b,0,0);
  if(district==6){var fan=point("fan",.48f,.48f);link(a,fan,0,0);link(fan,c,0,0);link(fan,b,0,0);}
  else if(stationFan){var hub=point("station-east-fan",.49f,.50f);link(a,hub,0,0);link(b,hub,0,0);link(c,hub,0,0);link(d,hub,0,0);}
  else if(eastSouth){var hub=point("office-south-chicane",.58f,.45f);link(a,hub,0,0);link(hub,c,0,0);link(hub,d,0,0);}
  else if(district==5||district==8)link(a,c,district==8?12:0,0);
  else if(district==4&&!stationRear&&!stationNorth)link(b,d,0,0);
 }
 static AssistedRoadNetwork.Path Path(MapPoint[] points){return new AssistedRoadNetwork.Path(points.Select(p=>p.V()).ToArray());}
 public static MapPoint[] Slice(AssistedRoadNetwork.Path path,float a,float b){var result=new List<MapPoint>{P(path.Position(a))};float at=0;for(int i=1;i<path.points.Length;i++){at+=Vector3.Distance(path.points[i-1],path.points[i]);if(at>a+.01f&&at<b-.01f)result.Add(P(path.points[i]));}result.Add(P(path.Position(b)));return result.ToArray();}
 static MapPoint[] Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int count=24){var p=new MapPoint[count+1];for(int i=0;i<=count;i++){float t=(float)i/count,u=1-t;p[i]=P(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}return p;}
 static MapPoint[] Centre(Vector3 a,Vector3 b,Vector3 d,float bend){
  float length=Vector3.Dot(b-a,d);if(length<85)throw new InvalidOperationException("A3 road too short");
  var start=a+d*42;var end=b-d*42;float middleLength=length-84;var points=new List<MapPoint>{P(a),P(start)};
  if(Mathf.Abs(bend)<.1f)points.AddRange(Bezier(start,start+d*middleLength*.35f,end-d*middleLength*.35f,end,16).Skip(1));
  else{var mid=(start+end)*.5f+TrafficSystem.Right(d)*bend;points.AddRange(Bezier(start,start+d*middleLength*.23f,mid-d*middleLength*.18f,mid,16).Skip(1));points.AddRange(Bezier(mid,mid+d*middleLength*.18f,end-d*middleLength*.23f,end,16).Skip(1));}
  points.Add(P(b));return points.ToArray();
 }
 void Street(Node a,Node b,int count,Vector3 direction,float bend=0,int travel=0){
  // Subdivided arterial/alley links can be only 100 m long. Forcing 42 m
  // cardinal tangents at both ends squeezed their lateral offset into 16 m,
  // producing an unnecessary S-bend. Follow the real chord on these links;
  // junction curves below use that same direction at both lane endpoints.
  if(bend==0&&Vector3.Distance(a.p,b.p)<220)direction=(b.p-a.p).normalized;
  string id=a.id+"-"+b.id,roadId="city-road:"+(a.id.StartsWith("alley:")||b.id.StartsWith("alley:")?"alley:":"")+id;var centre=Centre(a.p,b.p,direction,bend);var path=Path(centre);
  string name=count==8?"중앙역 대로":count==6?"중앙 남북 연결대로":count==4?"도시 순환·연결도로":roadId.Contains("alley:")?"생활 골목":"주거 곡선도로";
  roads.Add(new MapRoad{id=roadId,name=name,width=count*3.5f,lanes=count,sourceLanes=count,oneWay=travel,points=centre});
  marks.Add(new HdMark{id="city-center:"+id,kind="501",type="111",points=Slice(path,22,path.length-22)});
  for(int side=-1;side<=1;side+=2)for(int line=1;line<count/2;line++)marks.Add(new HdMark{id="city-divider:"+id+":"+side+":"+line,kind="503",type="212",points=Offset(path,22,path.length-22,side*line*3.5f)});
  for(int sign=-1;sign<=1;sign+=2){
   if(travel!=0&&sign!=travel)continue;
   var from=sign==1?a:b;var to=sign==1?b:a;var d=direction*sign;var directed=Path(sign==1?centre:centre.Reverse().ToArray());var family=new List<Lane>();
   for(int number=1;number<=count/2;number++){
    string laneId="city:"+from.id+">"+to.id+(number==count/2?"":":lane:"+number);var lanePath=Path(Offset(directed,34,directed.length-34,(number-.5f)*3.5f));int first=edges.Count;var lane=new Lane{from=from,to=to,first=first,last=first,number=number,count=count/2,width=count*3.5f,direction=d};
    bool change=count>2&&lanePath.length>170;
    if(change){
     // Three separated windows allow a fourth-lane driver to prepare a left
     // turn before this junction, without a detour through another block.
     int windows=count/2-1;var boundaries=new List<float>{0};
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
  rules.Add(new HybridJunctionRule{id=id,x=node.p.x,z=node.p.z,dx=0,dz=1,signalMode=node.signal?(node.permissive?1:2):0,stopLine=true,crosswalk=node.signal,approaches=approaches,status="authored-fictional",facilityStatus="authored-fictional",facilityEvidence="Approved fictional PvP A3 road design"});
  foreach(var a in incoming){var links=new List<int>();bool straight=outgoing.Any(b=>b.to!=a.from&&Vector3.Dot(a.direction,b.direction)>.9f);
   foreach(var b in outgoing){
    bool reverse=b.to==a.from;int turn=reverse?UturnMovement.Turn:Vector3.Dot(a.direction,b.direction)>.9f?0:Mathf.RoundToInt(Mathf.Sign(Vector3.Cross(a.direction,b.direction).y));
    if(reverse){if(!(node.id=="1:2"||node.id=="3:2")||a.count!=4||a.number!=1||b.number!=3)continue;}
    else if(turn==0){if(b.number!=Mathf.Min(a.number,b.count))continue;}
    else if(turn<0){if(straight&&a.number!=1||b.number!=Mathf.Min(a.number,b.count))continue;}
    else if(straight&&a.number!=a.count||b.number!=Mathf.Max(1,b.count-(a.count-a.number)))continue;
    var from=edges[a.last];var to=edges[b.first];var start=from.points.Last().V();var end=to.points[0].V();MapPoint[] points;
    if(reverse){points=UturnMovement.Geometry(RoadMapLane.From(from,edges.ToArray()),RoadMapLane.From(to,edges.ToArray())).Select(P).ToArray();int axis=Mathf.Abs(a.direction.z)>.7f?0:1;int sign=axis==0?(a.direction.z>0?1:-1):(a.direction.x>0?1:-1);var pos=start+TrafficSystem.Right(a.direction)*(a.width*.5f+1);
     signs.Add(new RoadMapFacility{id=from.id+":uturn-condition",kind="sign",type=UturnMovement.LeftOrPedestrianGreen,laneId=from.id,junctionId=id,axis=axis,sign=sign,rule=true,x=pos.x,z=pos.z,yaw=Quaternion.LookRotation(a.direction).eulerAngles.y,text="중앙대로 지정 차로",evidence=new RoadMapEvidence{state="game",note="Fictional A3 authored movement"}});
     var line=node.p-a.direction*34+TrafficSystem.Right(a.direction)*(a.width*.25f);signs.Add(new RoadMapFacility{id=from.id+":stop-line",kind="stopLine",junctionId=id,axis=axis,sign=sign,x=line.x,z=line.z,yaw=Quaternion.LookRotation(a.direction).eulerAngles.y,width=a.width*.5f-.3f,length=.4f,text=""});
    }else{float handle=Vector3.Distance(start,end)*.45f;points=Bezier(start,start+a.direction*handle,end-b.direction*handle,end,32);}
    links.Add(edges.Count);var join=Edge("join:"+id+":"+a.last+":"+b.first,id,"교차로",from.toNode,to.fromNode,b.number,points,new[]{b.first});join.ruleId=id;join.turn=turn;edges.Add(join);
    roads.Add(new MapRoad{id="city-join:"+a.last+":"+b.first,name="교차로",width=3.5f,lanes=1,sourceLanes=1,oneWay=1,suppressPaint=true,points=points});
   }edges[a.last].next=links.ToArray();
  }
 }
 public void AttachDocument(MapData map){map.editorDocument=new RoadMapDocument{id="pvp-city-a3-native",name="A3 가상 교통시설",mapId=map.mapId,lanes=map.hdEdges.Select(e=>RoadMapLane.From(e,map.hdEdges)).ToArray(),facilities=signs.ToArray()};}
}

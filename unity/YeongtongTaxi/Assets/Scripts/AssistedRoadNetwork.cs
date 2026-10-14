using System.Collections.Generic;
using UnityEngine;

// A restricted, geometric driving network. It does not claim to encode real turn laws.
public sealed class AssistedRoadNetwork {
    public sealed class Path {
        public readonly Vector3[] points;
        readonly float[] distances;
        public float length {get{return distances[distances.Length-1];}}
        public Path(Vector3[] values){points=values;distances=new float[values.Length];for(int i=1;i<values.Length;i++)distances[i]=distances[i-1]+Vector3.Distance(values[i-1],values[i]);}
        public Vector3 Position(float distance){for(int i=1;i<points.Length;i++)if(distance<=distances[i])return Vector3.Lerp(points[i-1],points[i],Mathf.InverseLerp(distances[i-1],distances[i],distance));return points[points.Length-1];}
        public Vector3 Tangent(float distance){return (Position(Mathf.Min(length,distance+.25f))-Position(Mathf.Max(0,distance-.25f))).normalized;}
        public float Project(Vector3 position,out float error){position.y=0;float best=float.MaxValue,at=0;for(int i=1;i<points.Length;i++){var delta=points[i]-points[i-1];float t=Mathf.Clamp01(Vector3.Dot(position-points[i-1],delta)/Mathf.Max(.001f,delta.sqrMagnitude));float sq=(position-(points[i-1]+delta*t)).sqrMagnitude;if(sq<best){best=sq;at=Mathf.Lerp(distances[i-1],distances[i],t);}}error=Mathf.Sqrt(best);return at;}
    }
    public sealed class Lane {
        public int id,from,to,index,count,corridor;
        public float width;
        public Path path;
        public string sourceIds;
    }
    public sealed class Join {public Lane target;public Path path;public int turn;}
    sealed class Node {public Vector3 p;public float width;public readonly List<int> edges=new List<int>();}
    sealed class Segment {public int a,b;public float width;public int count;public string id;}
    public readonly List<Lane> lanes=new List<Lane>();
    readonly List<Node> nodes=new List<Node>();
    readonly List<Segment> segments=new List<Segment>();
    readonly Dictionary<string,int> keys=new Dictionary<string,int>();
    readonly Dictionary<int,List<Lane>> outgoing=new Dictionary<int,List<Lane>>();
    readonly Dictionary<string,Join> joins=new Dictionary<string,Join>();
    int NodeAt(Vector3 p){string key=Mathf.RoundToInt(p.x*10)+":"+Mathf.RoundToInt(p.z*10);int id;if(keys.TryGetValue(key,out id))return id;id=nodes.Count;keys[key]=id;nodes.Add(new Node{p=p});return id;}
    int Other(Segment edge,int node){return edge.a==node?edge.b:edge.a;}
    public void Build(MapData map){
        lanes.Clear();nodes.Clear();segments.Clear();keys.Clear();outgoing.Clear();joins.Clear();
        var seen=new HashSet<string>();
        foreach(var road in map.roads){if(road.width<9)continue;for(int i=1;i<road.points.Length;i++){
            int a=NodeAt(road.points[i-1].V()),b=NodeAt(road.points[i].V());if(a==b)continue;
            string key=Mathf.Min(a,b)+":"+Mathf.Max(a,b);if(!seen.Add(key))continue;
            int id=segments.Count;segments.Add(new Segment{a=a,b=b,width=road.width,count=road.DisplayLanes/2,id=road.id});
            nodes[a].edges.Add(id);nodes[b].edges.Add(id);nodes[a].width=Mathf.Max(nodes[a].width,road.width);nodes[b].width=Mathf.Max(nodes[b].width,road.width);
        }}
        var visited=new HashSet<int>();int corridor=0;
        for(int start=0;start<nodes.Count;start++){
            if(nodes[start].edges.Count==2)continue;
            foreach(int first in nodes[start].edges){if(visited.Contains(first))continue;
                var points=new List<Vector3>{nodes[start].p};var source=new HashSet<string>();int current=start,edgeId=first,end=start,count=2;float width=float.MaxValue;
                while(true){visited.Add(edgeId);var edge=segments[edgeId];source.Add(edge.id);width=Mathf.Min(width,edge.width);count=Mathf.Min(count,edge.count);end=Other(edge,current);points.Add(nodes[end].p);
                    if(nodes[end].edges.Count!=2)break;int next=nodes[end].edges[0]==edgeId?nodes[end].edges[1]:nodes[end].edges[0];if(visited.Contains(next))break;current=end;edgeId=next;
                }
                var center=new Path(points.ToArray());float trimA=nodes[start].edges.Count>1?nodes[start].width*.5f+3:2,trimB=nodes[end].edges.Count>1?nodes[end].width*.5f+3:2;
                if(center.length<trimA+trimB+8)continue;
                AddLanes(points,start,end,corridor,count,width,trimA,trimB,string.Join(",",new List<string>(source).ToArray()));
                points.Reverse();AddLanes(points,end,start,corridor,count,width,trimB,trimA,string.Join(",",new List<string>(source).ToArray()));corridor++;
            }
        }
    }
    void AddLanes(List<Vector3> centerPoints,int from,int to,int corridor,int count,float width,float trimA,float trimB,string source){
        var center=new Path(centerPoints.ToArray());
        for(int index=0;index<count;index++){
            float offset=(index+.5f)*width/(2*count);var points=new List<Vector3>();
            int samples=Mathf.CeilToInt((center.length-trimA-trimB)/2);
            for(int i=0;i<=samples;i++){float d=Mathf.Lerp(trimA,center.length-trimB,(float)i/samples);var direction=(center.Position(Mathf.Min(center.length,d+2))-center.Position(Mathf.Max(0,d-2))).normalized;points.Add(center.Position(d)+TrafficSystem.Right(direction)*offset);}
            var lane=new Lane{id=lanes.Count,from=from,to=to,corridor=corridor,index=index,count=count,width=width/(count*2),path=new Path(points.ToArray()),sourceIds=source};lanes.Add(lane);
            if(!outgoing.ContainsKey(from))outgoing[from]=new List<Lane>();outgoing[from].Add(lane);
        }
    }
    public Lane Adjacent(Lane lane,int side){foreach(var other in lanes)if(other.corridor==lane.corridor&&other.from==lane.from&&other.index==lane.index+side)return other;return null;}
    public Lane FindLane(Vector3 position,Vector3 heading,out float progress){
        Lane best=null;float cost=3;progress=0;
        foreach(var lane in lanes){float error;float at=lane.path.Project(position,out error);if(error>=cost||Vector3.Dot(heading,lane.path.Tangent(at))<.75f)continue;cost=error;best=lane;progress=at;}
        return best;
    }
    public List<Lane> FindRoute(Lane start,Lane goal,int excludedCorridor){
        var result=new List<Lane>();if(start==null||goal==null)return result;
        var cost=new float[lanes.Count];var previous=new int[lanes.Count];var visited=new bool[lanes.Count];
        for(int i=0;i<cost.Length;i++){cost[i]=float.PositiveInfinity;previous[i]=-1;}cost[start.id]=0;
        for(int step=0;step<lanes.Count;step++){
            int current=-1;float best=float.PositiveInfinity;
            for(int i=0;i<cost.Length;i++)if(!visited[i]&&cost[i]<best){best=cost[i];current=i;}
            if(current<0)break;if(current==goal.id){for(int i=current;i>=0;i=previous[i])result.Add(lanes[i]);result.Reverse();return result;}visited[current]=true;
            var lane=lanes[current];var nexts=new List<Lane>();var distances=new List<float>();
            for(int turn=-1;turn<=1;turn++){var join=Connection(lane,turn);if(join!=null){nexts.Add(join.target);distances.Add(lane.path.length+join.path.length);}}
            // A lane change requires enough road length to complete before the junction.
            if(lane.path.length>30)foreach(int side in new[]{-1,1}){var adjacent=Adjacent(lane,side);if(adjacent!=null){nexts.Add(adjacent);distances.Add(25);}}
            for(int i=0;i<nexts.Count;i++){var next=nexts[i];if(next.corridor==excludedCorridor)continue;float value=best+distances[i];if(value<cost[next.id]){cost[next.id]=value;previous[next.id]=current;}}
        }
        return result;
    }
    public List<Lane> Alternative(List<Lane> primary){
        var best=new List<Lane>();float distance=float.PositiveInfinity;if(primary.Count<3)return best;
        foreach(var lane in primary){if(lane.corridor==primary[0].corridor||lane.corridor==primary[primary.Count-1].corridor)continue;
            var candidate=FindRoute(primary[0],primary[primary.Count-1],lane.corridor);float length=0;foreach(var item in candidate)length+=item.path.length;
            if(candidate.Count>0&&length<distance){best=candidate;distance=length;}
        }return best;
    }
    public Join Connection(Lane lane,int turn){
        string key=lane.id+":"+turn;Join cached;if(joins.TryGetValue(key,out cached))return cached;
        if(turn<0&&lane.index!=0||turn>0&&lane.index!=lane.count-1)return null;
        List<Lane> options;if(!outgoing.TryGetValue(lane.to,out options))return null;
        Lane chosen=null;float best=1000;var forward=lane.path.Tangent(lane.path.length);
        foreach(var next in options){if(next.corridor==lane.corridor)continue;float angle=Vector3.SignedAngle(forward,next.path.Tangent(0),Vector3.up);if(Mathf.Abs(angle)>145)continue;
            int direction=Mathf.Abs(angle)<35?0:angle<0?-1:1;if(direction!=turn)continue;
            int wanted=turn<0?0:turn>0?next.count-1:Mathf.Min(lane.index,next.count-1);if(next.index!=wanted)continue;
            float score=Mathf.Abs(angle-(turn*90));if(score<best){best=score;chosen=next;}
        }
        if(chosen==null)return null;
        var a=lane.path.Position(lane.path.length);var b=chosen.path.Position(0);float handle=Vector3.Distance(a,b)*.55f;
        var c=a+forward*handle;var d=b-chosen.path.Tangent(0)*handle;var points=new Vector3[33];
        for(int i=0;i<points.Length;i++){float t=(float)i/(points.Length-1),u=1-t;points[i]=u*u*u*a+3*u*u*t*c+3*u*t*t*d+t*t*t*b;}
        cached=new Join{target=chosen,path=new Path(points),turn=turn};joins[key]=cached;return cached;
    }
}

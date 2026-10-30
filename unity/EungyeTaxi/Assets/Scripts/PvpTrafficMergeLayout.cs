using System;
using System.Collections.Generic;
using UnityEngine;

// Runtime-only authored traffic metadata. Campaign map serialization is unchanged.
public sealed class NativeTrafficMergeInterval {
    public int edge;public float begin,end;public bool merging,clear;
}
public sealed class NativeTrafficMergeZone {
    public string id;public Vector3 direction,exit;public Bounds bounds;
    public NativeTrafficMergeInterval[] intervals;
}
public static class PvpTrafficMergeLayout {
    sealed class Builder {
        public string id;public Vector3 direction;
        public readonly Dictionary<int,NativeTrafficMergeInterval> intervals=new Dictionary<int,NativeTrafficMergeInterval>();
        public void Add(int edge,float begin,float end,bool merging=false,bool clear=false){
            NativeTrafficMergeInterval prior;
            if(intervals.TryGetValue(edge,out prior)){prior.begin=Mathf.Min(prior.begin,begin);prior.end=Mathf.Max(prior.end,end);prior.merging|=merging;prior.clear|=clear;}
            else intervals[edge]=new NativeTrafficMergeInterval{edge=edge,begin=begin,end=end,merging=merging,clear=clear};
        }
    }
    public static NativeTrafficMergeZone[] Create(MapData map){
        if(map.mapId!="pvp-city-a")return null;
        var graph=new HdRoadGraph(map);var groups=new Dictionary<string,Builder>();var previous=new List<int>[graph.edges.Length];
        for(int i=0;i<previous.Length;i++)previous[i]=new List<int>();
        for(int i=0;i<graph.edges.Length;i++)foreach(int next in graph.edges[i].next)previous[next].Add(i);
        for(int i=0;i<graph.edges.Length;i++){
            var edge=graph.edges[i];if(edge.change==0)continue;
            int marker=edge.id.LastIndexOf(":change:",StringComparison.Ordinal);if(marker<0)continue;
            string key=edge.id.Substring(0,marker);int lane=key.IndexOf(":lane:",StringComparison.Ordinal);
            if(lane>=0){int end=key.IndexOf(':',lane+6);key=end<0?key.Substring(0,lane):key.Remove(lane,end-lane);}
            Builder group;if(!groups.TryGetValue(key,out group)){group=new Builder{id="lane:"+key,direction=graph.paths[i].Tangent(0)};groups[key]=group;}
            group.Add(i,0,graph.paths[i].length,true);
            foreach(int head in previous[i]){
                group.Add(head,Mathf.Max(0,graph.paths[head].length-8),graph.paths[head].length);
                foreach(int middle in graph.edges[head].next)if(graph.edges[middle].change==0){
                    group.Add(middle,0,graph.paths[middle].length);
                    foreach(int tail in graph.edges[middle].next)group.Add(tail,0,Mathf.Min(8,graph.paths[tail].length),false,true);
                }
            }
            foreach(int tail in edge.next)group.Add(tail,0,Mathf.Min(8,graph.paths[tail].length),false,true);
        }
        for(int i=0;i<graph.edges.Length;i++){
            if(!graph.edges[i].id.EndsWith(":bay:pocket",StringComparison.Ordinal))continue;
            var stop=Array.Find(map.stops,s=>s.laneEdge==i);
            float departure=Mathf.Min(graph.paths[i].length-8,(stop==null?graph.paths[i].length*.5f:stop.laneAt)+2);
            var group=new Builder{id="bay:"+graph.edges[i].id,direction=graph.paths[i].Tangent(departure)};
            // A route ending at the passenger kerb has no departure span and
            // cannot own the through road while legitimately parked there.
            group.Add(i,departure,graph.paths[i].length,true);
            foreach(int head in previous[i])foreach(int through in graph.edges[head].next)if(through!=i){
                float error;float at=graph.paths[through].Project(graph.paths[i].Position(departure),out error);
                group.Add(through,at,graph.paths[through].length);group.direction=graph.paths[through].Tangent(at);
            }
            foreach(int tail in graph.edges[i].next)group.Add(tail,0,Mathf.Min(8,graph.paths[tail].length),false,true);
            groups[group.id]=group;
        }
        var result=new List<NativeTrafficMergeZone>();
        foreach(var group in groups.Values){
            bool initialized=false;var bounds=new Bounds();Vector3 exit=Vector3.zero;float furthest=float.NegativeInfinity;
            foreach(var interval in group.intervals.Values){
                var path=graph.paths[interval.edge];
                for(float at=interval.begin;at<=interval.end+.5f;at+=.5f){
                    var p=path.Position(Mathf.Min(interval.end,at));if(!initialized){bounds=new Bounds(p,Vector3.zero);initialized=true;}else bounds.Encapsulate(p);
                }
                bounds.Encapsulate(path.Position(interval.begin)-group.direction*30);
                if(interval.clear){var p=path.Position(interval.end);float ahead=Vector3.Dot(p,group.direction);if(ahead>furthest){furthest=ahead;exit=p;}}
            }
            bounds.Expand(new Vector3(12,8,12));
            result.Add(new NativeTrafficMergeZone{id=group.id,direction=group.direction,exit=exit,bounds=bounds,intervals=new List<NativeTrafficMergeInterval>(group.intervals.Values).ToArray()});
        }
        return result.ToArray();
    }
}

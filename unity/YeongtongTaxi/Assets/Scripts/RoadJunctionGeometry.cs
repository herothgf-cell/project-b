using System.Collections.Generic;
using UnityEngine;

// Geometric openings only: these do not infer traffic restrictions from the source map.
public static class RoadJunctionGeometry {
    public sealed class Opening {public Vector3 center;public float radius;}
    sealed class Node {public Vector3 center;public float halfWidth;public readonly HashSet<string> neighbours=new HashSet<string>();}
    sealed class Segment {public Vector3 a,b;public float halfWidth;public string road;}
    static string Key(Vector3 p){return Mathf.RoundToInt(p.x*10)+":"+Mathf.RoundToInt(p.z*10);}
    public static List<Opening> Build(MapData map){
        var nodes=new Dictionary<string,Node>();
        var segments=new List<Segment>();
        foreach(var road in map.roads){if((road.id??"").StartsWith("polish:"))continue;for(int i=1;i<road.points.Length;i++){
            var a=road.points[i-1].V();var b=road.points[i].V();if(Key(a)==Key(b))continue;
            foreach(var p in new[]{a,b}){string key=Key(p);Node node;if(!nodes.TryGetValue(key,out node)){node=new Node{center=p};nodes.Add(key,node);}node.halfWidth=Mathf.Max(node.halfWidth,road.width*.5f);node.neighbours.Add(Key(p==a?b:a));}
            if(!road.suppressPaint&&!(road.id??"").StartsWith("fuel:"))segments.Add(new Segment{a=a,b=b,halfWidth=road.width*.5f,road=road.id});
        }
        }var result=new List<Opening>();foreach(var node in nodes.Values)if(node.neighbours.Count>=3)result.Add(new Opening{center=node.center,radius=node.halfWidth+1});
        // Some source centre lines cross inside segments instead of sharing a
        // vertex. Open paint there too; this does not add a graph connection.
        var seen=new HashSet<string>();
        for(int i=0;i<segments.Count;i++)for(int j=i+1;j<segments.Count;j++){
            var a=segments[i];var b=segments[j];if(a.road==b.road||Mathf.Abs(Vector3.Dot((a.b-a.a).normalized,(b.b-b.a).normalized))>.85f)continue;
            if(Mathf.Max(a.a.x,a.b.x)<Mathf.Min(b.a.x,b.b.x)||Mathf.Max(b.a.x,b.b.x)<Mathf.Min(a.a.x,a.b.x)||Mathf.Max(a.a.z,a.b.z)<Mathf.Min(b.a.z,b.b.z)||Mathf.Max(b.a.z,b.b.z)<Mathf.Min(a.a.z,a.b.z))continue;
            RoadGeometry.Point crossing;if(!RoadGeometry.TryCrossing(new RoadGeometry.Point(a.a.x,a.a.z),new RoadGeometry.Point(a.b.x,a.b.z),new RoadGeometry.Point(b.a.x,b.a.z),new RoadGeometry.Point(b.b.x,b.b.z),out crossing))continue;
            var point=new Vector3(crossing.x,0,crossing.z);if(!seen.Add(Key(point))||result.Exists(o=>(o.center-point).sqrMagnitude<1))continue;
            result.Add(new Opening{center=point,radius=Mathf.Max(a.halfWidth,b.halfWidth)+1});
        }return result;
    }
}

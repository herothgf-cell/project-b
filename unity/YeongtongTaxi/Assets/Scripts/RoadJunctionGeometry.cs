using System.Collections.Generic;
using UnityEngine;

// Geometric openings only: these do not infer traffic restrictions from the source map.
public static class RoadJunctionGeometry {
    public sealed class Opening {public Vector3 center;public float radius;}
    sealed class Node {public Vector3 center;public float halfWidth;public readonly HashSet<string> neighbours=new HashSet<string>();}
    static string Key(Vector3 p){return Mathf.RoundToInt(p.x*10)+":"+Mathf.RoundToInt(p.z*10);}
    public static List<Opening> Build(MapData map){
        var nodes=new Dictionary<string,Node>();
        foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){
            var a=road.points[i-1].V();var b=road.points[i].V();if(Key(a)==Key(b))continue;
            foreach(var p in new[]{a,b}){string key=Key(p);Node node;if(!nodes.TryGetValue(key,out node)){node=new Node{center=p};nodes.Add(key,node);}node.halfWidth=Mathf.Max(node.halfWidth,road.width*.5f);node.neighbours.Add(Key(p==a?b:a));}
        }
        var result=new List<Opening>();foreach(var node in nodes.Values)if(node.neighbours.Count>=3)result.Add(new Opening{center=node.center,radius=node.halfWidth+1});return result;
    }
}

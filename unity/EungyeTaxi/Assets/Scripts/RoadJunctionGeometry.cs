using System.Collections.Generic;
using UnityEngine;

// Geometric openings only: these do not infer traffic restrictions from the source map.
public static class RoadJunctionGeometry {
    // Physical paint exclusion rectangles, independent of routing permissions.
    public sealed class Clearance {
        public string junctionId,kind;public Vector3 center,forward;public float halfWidth,halfDepth;
        public bool Contains(Vector3 point,float extra=0){var delta=point-center;return Mathf.Abs(Vector3.Dot(delta,forward))<=halfDepth+extra&&Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(forward)))<=halfWidth+extra;}
        public bool Overlaps(Vector3 point,Vector3 heading,float width,float depth){
            var right=TrafficSystem.Right(forward);var side=TrafficSystem.Right(heading);var delta=point-center;
            return !Separated(forward,delta,right,heading,side,width,depth)&&!Separated(right,delta,right,heading,side,width,depth)&&!Separated(heading,delta,right,heading,side,width,depth)&&!Separated(side,delta,right,heading,side,width,depth);
        }
        bool Separated(Vector3 axis,Vector3 delta,Vector3 right,Vector3 heading,Vector3 side,float width,float depth){float ours=halfDepth*Mathf.Abs(Vector3.Dot(forward,axis))+halfWidth*Mathf.Abs(Vector3.Dot(right,axis)),theirs=depth*Mathf.Abs(Vector3.Dot(heading,axis))+width*Mathf.Abs(Vector3.Dot(side,axis));return Mathf.Abs(Vector3.Dot(delta,axis))>ours+theirs;}
    }
    public static List<Clearance> PaintClearances(MapData map,List<TrafficJunction> live=null){
        var result=new List<Clearance>();var ids=new HashSet<string>();
        foreach(var j in map.junctionRules??new HybridJunctionRule[0]){
            if(j==null)continue;var axis=new Vector3(j.dx,0,j.dz).normalized;if(axis.sqrMagnitude<.5f)axis=Vector3.forward;
            float[] widths={3,3};foreach(var a in j.approaches??new HybridApproach[0]){var d=new Vector3(a.dx,0,a.dz).normalized;int k=Mathf.Abs(Vector3.Dot(d,axis))>.7f?0:1;widths[k]=Mathf.Max(widths[k],a.width*.5f);}
            ids.Add(j.id??"");result.Add(new Clearance{junctionId=j.id,kind="core",center=j.V(),forward=axis,halfWidth=widths[0],halfDepth=widths[1]});
            foreach(var a in j.approaches??new HybridApproach[0]){
                var d=new Vector3(a.dx,0,a.dz).normalized;if(d.sqrMagnitude<.5f)continue;int k=Mathf.Abs(Vector3.Dot(d,axis))>.7f?0:1;
                int sign=Vector3.Dot(d,k==0?axis:TrafficSystem.Right(axis))>=0?1:-1;float reach=widths[1-k];bool crossing=false,stop=false;
                foreach(var f in map.editorDocument==null?new RoadMapFacility[0]:map.editorDocument.facilities??new RoadMapFacility[0]){
                    if(f==null||f.junctionId!=j.id||f.axis!=k||f.sign!=sign||(f.kind!="crosswalk"&&f.kind!="stopLine"))continue;
                    if(f.kind=="crosswalk")crossing=true;else stop=true;
                    // Extend to the actual outer edge, including lateral edits.
                    var fd=Quaternion.Euler(0,f.yaw,0)*Vector3.forward;var toward=j.V()-f.Position;if(Vector3.Dot(fd,toward)<0)fd=-fd;
                    float along=Vector3.Dot(toward,fd);if(along<0)continue;
                    float depth=Mathf.Max(.2f,f.length*.5f);
                    result.Add(new Clearance{junctionId=j.id,kind=f.kind,center=f.Position+fd*(along-depth)*.5f,forward=fd,halfWidth=Mathf.Max(.2f,f.width*.5f),halfDepth=(along+depth)*.5f});
                }
                if(j.crosswalk&&!crossing)reach=Mathf.Max(reach,widths[1-k]+TrafficJunction.CrosswalkCentreOffset+TrafficJunction.CrosswalkDepth*.5f);
                if(j.stopLine&&!stop)reach=Mathf.Max(reach,widths[1-k]+4.2f);
                result.Add(new Clearance{junctionId=j.id,kind="approach",center=j.V()-d*reach*.5f,forward=d,halfWidth=Mathf.Max(1.5f,a.width*.5f),halfDepth=reach*.5f});
            }
        }
        if(live!=null)foreach(var j in live){if(ids.Contains(j.name??""))continue;var axis=j.direction.normalized;if(axis.sqrMagnitude<.5f)axis=Vector3.forward;result.Add(new Clearance{junctionId=j.name,kind="core",center=j.center,forward=axis,halfWidth=j.halfWidth[0],halfDepth=j.halfWidth[1]});}
        return result;
    }
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

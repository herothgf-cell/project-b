using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Independent lane-pair sampling: do not infer expected dividers from the
// renderer's own cross-section/grouping, which could repeat the same omission.
public static class LaneWidthPresentationAudit
{
    class Lane {public HdEdge edge;public AssistedRoadNetwork.Path path;}
    class Stroke {public Vector3 p,d;public float length;}
    public static void Run()
    {
        var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();
        EungyeConnectivityRepair.Apply(map,package.baseline);
        var lanes=map.hdEdges.Where(e=>!e.disabled&&e.turn==0&&e.change==0&&e.status=="provisional"&&!e.id.StartsWith("join:")&&!e.id.StartsWith("fuel:"))
            .Select(e=>new Lane{edge=e,path=new AssistedRoadNetwork.Path(e.points.Select(p=>p.V()).ToArray())}).ToArray();
        var strokes=new List<Stroke>();RoadPaintGeometry.Paint(map,null,(name,p,d,w,l,y)=>{if(name.Contains("divider"))strokes.Add(new Stroke{p=p,d=d,length=l});});
        var clearances=RoadJunctionGeometry.PaintClearances(map);var openings=RoadJunctionGeometry.Build(map);
        openings.RemoveAll(o=>clearances.Any(c=>c.kind=="core"&&(c.center-o.center).sqrMagnitude<4));
        var crossings=ReviewedCrosswalkGeometry.Build(map);var unmarked=new ReviewedUnmarkedRoadPaint(map);
        var rows=new List<string>{"lane,other,x,z,spacing,dividerDistance,result"};int samples=0,missing=0;
        foreach(var lane in lanes){
            var neighbours=lanes.Where(n=>n!=lane&&n.edge.sourceId.Split(',').Intersect(lane.edge.sourceId.Split(',')).Any()).ToArray();
            for(float at=5;at<lane.path.length-5;at+=5){var p=lane.path.Position(at);var d=lane.path.Tangent(at);Lane nearest=null;Vector3 q=Vector3.zero;float gap=5.2f;
                foreach(var other in neighbours){float err;float s=other.path.Project(p,out err);var v=other.path.Position(s);var h=other.path.Tangent(s);float side=Vector3.Dot(v-p,TrafficSystem.Right(d));
                    if(side<2.4f||side>=gap||Vector3.Dot(d,h)<.97f||Mathf.Abs(Vector3.Dot(v-p,d))>.5f||s<2||s>other.path.length-2)continue;
                    gap=side;nearest=other;q=v;
                }
                if(nearest==null)continue;var centre=(p+q)*.5f;
                if(RoadPaintGeometry.InOpening(centre,clearances,openings,crossings,1)||unmarked.SuppressesSynthetic("Lane divider",centre,d,.13f,1))continue;
                float distance=1000;foreach(var stroke in strokes){if(Mathf.Abs(Vector3.Dot(stroke.d,d))<.9f)continue;var delta=centre-stroke.p;float along=Mathf.Abs(Vector3.Dot(delta,stroke.d));float across=Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(stroke.d)));if(across>.8f)continue;distance=Mathf.Min(distance,Mathf.Max(0,along-stroke.length*.5f));}
                samples++;bool fail=distance>4;if(fail)missing++;
                rows.Add($"{lane.edge.id},{nearest.edge.id},{centre.x:F2},{centre.z:F2},{gap:F2},{distance:F2},{(fail?"missing":"ok")}");
            }
        }
        File.WriteAllLines(Path.Combine(ProjectMapBuild.Root,"artifacts/color-road-survey/lane-width-presentation.csv"),rows);
        Debug.Log($"LANE_WIDTH_AUDIT samples={samples} missing={missing}");
    }
    public static void NativeHandoff(){
        var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);
        var root=new GameObject("Native handoff audit");
        try{HdRouteTrial.DrawMarkings(map,root.transform);var filter=root.transform.Find("원본 노면선 0").GetComponent<MeshFilter>();var vertices=filter.sharedMesh.vertices;var point=new Vector3(801.45f,0,341.34f);float best=float.MaxValue;
            for(int i=0;i<vertices.Length;i+=4){var c=(vertices[i]+vertices[i+1]+vertices[i+2]+vertices[i+3])*.25f;c.y=0;best=Mathf.Min(best,Vector3.Distance(point,c));}
            Debug.Log("LANE_WIDTH_NATIVE_HANDOFF nearestWhiteQuad="+best);
            if(best>4)throw new Exception("Native handoff needs screen review");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}

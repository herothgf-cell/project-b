using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
// Audit the actual clipped yellow paint primitives, not just corridor midpoints.
public static class RoadMarkingAudit {
 [Serializable] public sealed class Sample {public float x,z,gap;public string markId,kind;}
 [Serializable] public sealed class Report {public string contentHash,scope;public int runtimeLanes,importedMarks,generatedYellowSegments,coincidentSamples,nearSamples,duplicateGeneratedCentres;public Sample[] importedProximity;}
 sealed class Source {public HdMark mark;public AssistedRoadNetwork.Path path;public Bounds bounds;}
 public static void Run(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();var sources=new List<Source>();
  foreach(var m in map.hdMarks??new HdMark[0]){if(!m.type.StartsWith("1")||m.points.Length<2)continue;var ps=m.points.Select(p=>p.V()).ToArray();var b=new Bounds(ps[0],Vector3.zero);foreach(var p in ps)b.Encapsulate(p);b.Expand(12);sources.Add(new Source{mark=m,path=new AssistedRoadNetwork.Path(ps),bounds=b});}
  var root=new GameObject("Whole-map marking audit");var report=new Report{contentHash=ProjectMapStore.Load(ProjectMapBuild.Root).contentHash,runtimeLanes=map.hdEdges.Length,importedMarks=map.hdMarks.Length,scope="Every generated yellow strip centre (paint samples <=1m), after junction clipping, compared with imported yellow polylines within6m. Baseline marks retain source roles; proximity is not proof of an error or a field facility."};var nearby=new List<Sample>();var centres=new HashSet<Vector2Int>();
  try{var traffic=root.AddComponent<TrafficSystem>();traffic.Build(map,false);
   RoadPaintGeometry.Paint(map,traffic.junctions,(name,p,dir,w,l,yellow)=>{if(!yellow)return;report.generatedYellowSegments++;if(!centres.Add(new Vector2Int(Mathf.RoundToInt(p.x*100),Mathf.RoundToInt(p.z*100))))report.duplicateGeneratedCentres++;
    float closest=6.001f;Source source=null;foreach(var s in sources){if(!s.bounds.Contains(p))continue;float error;s.path.Project(p,out error);if(error<closest){closest=error;source=s;}}
    if(source!=null&&closest<=6){if(closest<=.25f)report.coincidentSamples++;else report.nearSamples++;nearby.Add(new Sample{x=p.x,z=p.z,gap=closest,markId=source.mark.id,kind=source.mark.kind});}
   });
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  report.importedProximity=nearby.ToArray();string path=Path.Combine(ProjectMapBuild.Root,"artifacts/road-markings-runtime-audit.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
  Debug.Log("ROAD_MARKING_AUDIT_OK lanes="+report.runtimeLanes+" generatedYellow="+report.generatedYellowSegments+" imported="+report.importedMarks+" coincident="+report.coincidentSamples+" near="+report.nearSamples+" duplicateGeneratedCentres="+report.duplicateGeneratedCentres);
 }
}

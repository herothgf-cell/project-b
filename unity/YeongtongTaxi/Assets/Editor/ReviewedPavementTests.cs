using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
public static class ReviewedPavementTests {
 public static void Run(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();var data=ReviewedPavement.Load();if(data.surfaces.Length!=8)throw new Exception("Expected eight source surfaces");
  using(var hash=SHA256.Create()){string actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes("Assets/Resources/eungye-hybrid-map.json"))).Replace("-","").ToLowerInvariant();if(actual!=data.baseMapSha256)throw new Exception("Pavement source map changed; regenerate and review");}
  var graph=new HdRoadGraph(map);int samples=0;
  for(int e=0;e<graph.paths.Length;e++){if(graph.edges[e].disabled)continue;var path=graph.paths[e];for(float at=0;at<=path.length;at+=1){var p=path.Position(at);if(p.x<435||p.x>575||p.z<375||p.z>515)continue;var right=TrafficSystem.Right(path.Tangent(at));foreach(float side in new[]{-1.1f,0,1.1f}){samples++;if(ReviewedPavement.Contains(map,p+right*side))throw new Exception("Pavement blocks shared lane "+e);}}}
  int faces=0;foreach(var s in data.surfaces)for(int i=0;i<s.triangles.Length;i+=3){var a=s.points[s.triangles[i]].V();var b=s.points[s.triangles[i+1]].V();var c=s.points[s.triangles[i+2]].V();if(Vector3.Cross(b-a,c-a).y<=0)throw new Exception("Pavement winding");if(!ReviewedPavement.Contains(map,(a+b+c)/3))throw new Exception("Pavement containment");faces++;}
  File.WriteAllText("../../artifacts/reviewed-pavement-test.txt","surfaces=8 faces="+faces+" sharedLaneFootprintSamples="+samples+" blocked=0\n");
 }
}

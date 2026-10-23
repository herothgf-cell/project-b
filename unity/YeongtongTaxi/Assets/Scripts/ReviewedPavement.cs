using System;
using UnityEngine;

// NGII A4 polygons reviewed against Naver roadview. Lane-clearance edits remain provisional.
public static class ReviewedPavement {
 [Serializable]public sealed class Surface {public string id,kind;public MapPoint[] points;public int[] triangles;}
 [Serializable]public sealed class Data {public string baseMapSha256,status;public Surface[] surfaces;}
 static Data cached;
 public static Data Load(){if(cached==null){var text=Resources.Load<TextAsset>("eungye-j5057-pavement");cached=text?JsonUtility.FromJson<Data>(text.text):new Data{surfaces=new Surface[0]};}return cached;}
 static float Cross(Vector3 a,Vector3 b,Vector3 p){return (b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);}
 public static bool Contains(MapData map,Vector3 p){
  if(ProjectMapScene.Active(map))return ProjectMapScene.OnArea(map,p,true);
  if(map.mapId!="eungye"||!map.hybrid||p.x<435||p.x>575||p.z<375||p.z>515)return false;
  foreach(var s in Load().surfaces)for(int i=0;i<s.triangles.Length;i+=3){var a=s.points[s.triangles[i]].V();var b=s.points[s.triangles[i+1]].V();var c=s.points[s.triangles[i+2]].V();if(Cross(a,b,p)<=.0001f&&Cross(b,c,p)<=.0001f&&Cross(c,a,p)<=.0001f)return true;}return false;
 }
 public static void Draw(MapData map,Transform parent){
  if(ProjectMapScene.Active(map))return;
  if(map.mapId!="eungye"||!map.hybrid)return;
  var walk=WorldBuilder.Mat("NGII reviewed pavement",new Color(.60f,.57f,.51f));var cycle=WorldBuilder.Mat("NGII reviewed cycle surface",new Color(.46f,.21f,.18f));
  foreach(var s in Load().surfaces){var mesh=new Mesh();var vertices=new Vector3[s.points.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=s.points[i].V(.14f);mesh.vertices=vertices;mesh.triangles=s.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
   var obj=new GameObject("NGII 부속구간 · "+s.kind+" · "+s.id+" · 차로 여유 임시 보정");obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=s.kind=="보도"?walk:cycle;obj.AddComponent<MeshCollider>().sharedMesh=mesh;
  }
 }
}

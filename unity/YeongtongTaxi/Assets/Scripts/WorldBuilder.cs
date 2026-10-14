using System.Collections.Generic;
using UnityEngine;
public class WorldBuilder : MonoBehaviour {
    public MapData map;
    public static Material Mat(string name, Color c) {
        var m = new Material(Shader.Find("Standard")); m.name=name; m.color=c; m.SetFloat("_Glossiness",.12f); return m;
    }
    public static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent=null, bool solid=true) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mat; if(!solid) Destroy(g.GetComponent<Collider>()); return g;
    }
    public void Build() {
        map=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("drive-map").text);
        var ground=Mat("Ground",new Color(.33f,.40f,.32f)); var asphalt=Mat("Road",new Color(.16f,.18f,.20f));
        var stripe=Mat("Paint",new Color(.88f,.79f,.39f)); var building=Mat("Buildings",new Color(.70f,.67f,.60f));
        Box("Terrain",new Vector3(map.width/2,-.5f,map.depth/2),new Vector3(map.width+80,1,map.depth+80),ground,transform);
        foreach(var road in map.roads) {
            for(int i=1;i<road.points.Length;i++) {
                var a=road.points[i-1].V(.025f); var b=road.points[i].V(.025f); var d=b-a;
                var obj=Box("Road "+road.id,(a+b)*.5f,new Vector3(road.width,.04f,d.magnitude),asphalt,transform,false); obj.transform.rotation=Quaternion.LookRotation(d);
            }
            foreach(var p in road.points) {
                var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder); disc.name="Road join"; disc.transform.SetParent(transform); disc.transform.position=p.V(.025f); disc.transform.localScale=new Vector3(road.width,.02f,road.width); disc.GetComponent<Renderer>().sharedMaterial=asphalt; Destroy(disc.GetComponent<Collider>());
            }
        }
        foreach(var b in map.buildings) {
            var vertices=new List<Vector3>(); var indices=new List<int>(); int n=b.points.Length;
            for(int i=0;i<n;i++) vertices.Add(b.points[i].V(b.height));
            for(int i=0;i<b.triangles.Length;i+=3) { indices.Add(b.triangles[i+2]); indices.Add(b.triangles[i+1]); indices.Add(b.triangles[i]); }
            float area=0; for(int i=0;i<n;i++){var a=b.points[i];var c=b.points[(i+1)%n];area+=a.x*c.z-c.x*a.z;}
            for(int i=0;i<n;i++) {
                int j=(i+1)%n,k=vertices.Count; vertices.Add(b.points[i].V()); vertices.Add(b.points[j].V()); vertices.Add(b.points[j].V(b.height)); vertices.Add(b.points[i].V(b.height));
                indices.AddRange(area>0?new[]{k,k+2,k+1,k,k+3,k+2}:new[]{k,k+1,k+2,k,k+2,k+3});
            }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj=new GameObject("Building "+b.id); obj.transform.SetParent(transform); obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=building; obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        foreach(var t in map.targets) {
            var m=Mat(t.name,t.name=="T1"?new Color(.1f,.75f,.9f):new Color(1,.5f,.12f));
            var label=new GameObject(t.name+" label"); label.transform.position=t.V(8); label.transform.SetParent(transform); var text=label.AddComponent<TextMesh>(); text.text=t.name; text.fontSize=64; text.characterSize=.15f; text.anchor=TextAnchor.MiddleCenter; text.color=m.color; label.AddComponent<FaceCamera>();
        }
        var barrier=Mat("Test tile boundary",new Color(.85f,.39f,.12f));
        Box("North boundary",new Vector3(map.width/2,.6f,map.depth+2),new Vector3(map.width+8,1.2f,1),barrier,transform);
        Box("South boundary",new Vector3(map.width/2,.6f,-2),new Vector3(map.width+8,1.2f,1),barrier,transform);
        Box("West boundary",new Vector3(-2,.6f,map.depth/2),new Vector3(1,1.2f,map.depth+8),barrier,transform);
        Box("East boundary",new Vector3(map.width+2,.6f,map.depth/2),new Vector3(1,1.2f,map.depth+8),barrier,transform);
    }
}
public class FaceCamera : MonoBehaviour { void LateUpdate() { if(Camera.main) transform.rotation=Camera.main.transform.rotation; } }

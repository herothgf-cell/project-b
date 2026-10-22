using System.Collections.Generic;
using UnityEngine;
[System.Serializable] public class SceneOverrides {public string[] hiddenBuildings;}
public class WorldBuilder : MonoBehaviour {
    public MapData map;
    public static Material Mat(string name, Color c) {
        var m = new Material(Shader.Find("Standard")); m.name=name; m.color=c; m.SetFloat("_Glossiness",.12f); return m;
    }
    public static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent=null, bool solid=true) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mat; if(!solid) Destroy(g.GetComponent<Collider>()); return g;
    }
    // Render static geometry in spatial chunks; collider footprints remain individual.
    // This avoids a GameObject/renderer for every 15m segment of a district map.
    sealed class GeometryBatch {
        public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<int> indices=new List<int>();
        public void Add(IList<Vector3> points,IList<int> faces){int offset=vertices.Count;for(int i=0;i<points.Count;i++)vertices.Add(points[i]);for(int i=0;i<faces.Count;i++)indices.Add(offset+faces[i]);}
    }
    static GeometryBatch Batch(Dictionary<Vector2Int,GeometryBatch> batches,Vector3 position){var key=new Vector2Int(Mathf.FloorToInt(position.x/128),Mathf.FloorToInt(position.z/128));GeometryBatch batch;if(!batches.TryGetValue(key,out batch)){batch=new GeometryBatch();batches[key]=batch;}return batch;}
    void RenderBatches(Dictionary<Vector2Int,GeometryBatch> batches,string name,Material material){foreach(var item in batches){var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(item.Value.vertices);mesh.SetTriangles(item.Value.indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var obj=new GameObject(name+" "+item.Key);obj.transform.SetParent(transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;}}
    public void Build() {
        map=RoadRulesTrial.Enabled?RoadRulesTrial.CreateMap():MapCatalog.Load();
        FuelStationLayout.Apply(map);RestaurantSiteLayout.Apply(map);
        MapSupplements.DrawCrossings(map,transform);
        var ground=Mat("Ground",new Color(.33f,.40f,.32f)); var asphalt=Mat("Road",new Color(.16f,.18f,.20f));
        var building=Mat("Buildings",new Color(.70f,.67f,.60f));
        Box("Terrain",new Vector3(map.width/2,-.5f,map.depth/2),new Vector3(map.width+80,1,map.depth+80),ground,transform);
        if(map.areas!=null)foreach(var area in map.areas){
            if(area.points==null||area.triangles==null)continue;
            var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;var vertices=new Vector3[area.points.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=area.points[i].V(.02f);
            var triangles=(int[])area.triangles.Clone();for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i];triangles[i]=triangles[i+2];triangles[i+2]=swap;}
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var obj=new GameObject(area.name);obj.transform.SetParent(transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=Mat(area.kind,new Color(.13f,.42f,.59f));
        }
        var roadBatches=new Dictionary<Vector2Int,GeometryBatch>();
        var buildingBatches=new Dictionary<Vector2Int,GeometryBatch>();
        foreach(var road in map.roads) {
            for(int i=1;i<road.points.Length;i++) {
                var a=road.points[i-1].V(.045f);var b=road.points[i].V(.045f);var dir=(b-a).normalized;
                var right=TrafficSystem.Right(dir)*road.width*.5f;
                Batch(roadBatches,(a+b)*.5f).Add(new[]{a-right,b-right,b+right,a+right},new[]{0,1,2,0,2,3});
            }
            foreach(var p in road.points) {
                const int sides=12;var vertices=new Vector3[sides+1];var indices=new int[sides*3];vertices[0]=p.V(.045f);
                for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;vertices[i+1]=vertices[0]+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*road.width*.5f;indices[i*3]=0;indices[i*3+1]=(i+1)%sides+1;indices[i*3+2]=i+1;}
                Batch(roadBatches,p.V()).Add(vertices,indices);
            }
        }
        RenderBatches(roadBatches,"도로 표면",asphalt);if(map.editorDocument!=null)foreach(var r in map.roads){if(!r.id.StartsWith("editor:")||!System.Array.Exists(r.points,p=>p.elevation>.001f))continue;var vertices=new List<Vector3>();var faces=new List<int>();for(int i=1;i<r.points.Length;i++){var a=r.points[i-1].V(.045f);var b=r.points[i].V(.045f);var side=TrafficSystem.Right((b-a).normalized)*r.width*.5f;int n=vertices.Count;vertices.AddRange(new[]{a-side,b-side,b+side,a+side});faces.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();var collider=new GameObject("사용자 높이 차로 충돌 "+r.id);collider.transform.SetParent(transform);collider.AddComponent<MeshCollider>().sharedMesh=mesh;}
        if(map.HasHd)HdRouteTrial.DrawMarkings(map,transform);
        foreach(var b in map.buildings) {
            if(SceneBuildingVisibility.Hidden(map,b.id)){Debug.Log("PROVISIONAL_BUILDING_HIDDEN "+b.id+" · 원본 보존 / 생성 차로 겹침 검토 대기");continue;}
            var vertices=new List<Vector3>(); var indices=new List<int>(); int n=b.points.Length;
            for(int i=0;i<n;i++) vertices.Add(b.points[i].V(b.height));
            for(int i=0;i<b.triangles.Length;i+=3) { indices.Add(b.triangles[i+2]); indices.Add(b.triangles[i+1]); indices.Add(b.triangles[i]); }
            float area=0; for(int i=0;i<n;i++){var a=b.points[i];var c=b.points[(i+1)%n];area+=a.x*c.z-c.x*a.z;}
            for(int i=0;i<n;i++) {
                int j=(i+1)%n,k=vertices.Count; vertices.Add(b.points[i].V()); vertices.Add(b.points[j].V()); vertices.Add(b.points[j].V(b.height)); vertices.Add(b.points[i].V(b.height));
                indices.AddRange(area>0?new[]{k,k+2,k+1,k,k+3,k+2}:new[]{k,k+1,k+2,k,k+2,k+3});
            }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj=new GameObject("Building "+b.id);obj.transform.SetParent(transform);obj.AddComponent<MeshCollider>().sharedMesh=mesh;
            Batch(buildingBatches,mesh.bounds.center).Add(vertices,indices);
        }
        RenderBatches(buildingBatches,"건물 외형",building);
        FuelStationLayout.Draw(map,transform);RestaurantSiteLayout.Draw(map,transform);
        foreach(var t in map.targets) {
            var m=Mat(t.name,t.name=="T1"?new Color(.1f,.75f,.9f):new Color(1,.5f,.12f));
            var label=new GameObject(t.name+" label"); label.transform.position=t.V(8); label.transform.SetParent(transform); var text=label.AddComponent<TextMesh>(); text.text=t.name; text.fontSize=64; text.characterSize=.15f; text.anchor=TextAnchor.MiddleCenter; text.color=m.color; label.AddComponent<FaceCamera>();
        }
        if(map.landmarks!=null)foreach(var landmark in map.landmarks){
            var label=new GameObject("지명 "+landmark.name);label.transform.SetParent(transform);label.transform.position=landmark.V(7);
            var text=label.AddComponent<TextMesh>();text.text=landmark.name;text.font=KoreanText.Font;text.fontSize=48;text.characterSize=.12f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.8f,1,.85f);
            if(text.font)label.GetComponent<Renderer>().sharedMaterial=text.font.material;label.AddComponent<FaceCamera>();
        }
        var barrier=Mat("Test tile boundary",new Color(.85f,.39f,.12f));
        Box("North boundary",new Vector3(map.width/2,.6f,map.depth+2),new Vector3(map.width+8,1.2f,1),barrier,transform);
        Box("South boundary",new Vector3(map.width/2,.6f,-2),new Vector3(map.width+8,1.2f,1),barrier,transform);
        Box("West boundary",new Vector3(-2,.6f,map.depth/2),new Vector3(1,1.2f,map.depth+8),barrier,transform);
        Box("East boundary",new Vector3(map.width+2,.6f,map.depth/2),new Vector3(1,1.2f,map.depth+8),barrier,transform);
    }
}
public class FaceCamera : MonoBehaviour { void LateUpdate() { if(Camera.main) transform.rotation=Camera.main.transform.rotation; } }

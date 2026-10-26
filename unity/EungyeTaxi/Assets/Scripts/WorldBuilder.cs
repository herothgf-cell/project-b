using System.Collections.Generic;
using UnityEngine;
[System.Serializable] public class SceneOverrides {public string[] hiddenBuildings;}
public class WorldBuilder : MonoBehaviour {
    public MapData map;
    public static System.Action<Material> MaterialCreated;
    public static Material Mat(string name, Color c) {
        var m = new Material(Shader.Find("Standard")); m.name=name; m.color=c; m.SetFloat("_Glossiness",.12f); if(MaterialCreated!=null)MaterialCreated(m); return m;
    }
    public static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent=null, bool solid=true) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mat; if(!solid){if(Application.isPlaying)Destroy(g.GetComponent<Collider>());else DestroyImmediate(g.GetComponent<Collider>());} return g;
    }
    // Render static geometry in spatial chunks; collider footprints remain individual.
    // This avoids a GameObject/renderer for every 15m segment of a district map.
    public sealed class GeometryBatch {
        public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<int> indices=new List<int>();
        public void Add(IList<Vector3> points,IList<int> faces){int offset=vertices.Count;for(int i=0;i<points.Count;i++)vertices.Add(points[i]);for(int i=0;i<faces.Count;i++)indices.Add(offset+faces[i]);}
    }
    public static GeometryBatch Batch(Dictionary<Vector2Int,GeometryBatch> batches,Vector3 position){var key=new Vector2Int(Mathf.FloorToInt(position.x/128),Mathf.FloorToInt(position.z/128));GeometryBatch batch;if(!batches.TryGetValue(key,out batch)){batch=new GeometryBatch();batches[key]=batch;}return batch;}
    public void RenderBatches(Dictionary<Vector2Int,GeometryBatch> batches,string name,Material material,bool solid=false){foreach(var item in batches){var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(item.Value.vertices);mesh.SetTriangles(item.Value.indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var obj=new GameObject(name+" "+item.Key);obj.transform.SetParent(transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;if(solid)obj.AddComponent<MeshCollider>().sharedMesh=mesh;}}
    public void Build() {
        var watch=System.Diagnostics.Stopwatch.StartNew();
        map=RoadRulesTrial.Enabled?RoadRulesTrial.CreateMap():MapCatalog.Load();
        Debug.Log("STARTUP_WORLD map_ms="+watch.ElapsedMilliseconds);watch.Restart();
        BuildStatic(map);
        Debug.Log("STARTUP_WORLD geometry_ms="+watch.ElapsedMilliseconds);
    }
    public void BuildStatic(MapData source,bool collisions=true,bool decorations=true,bool includeGeometry=true) {
        map=source;
        if(decorations)PassengerStopCatalog.Apply(map);
        if(decorations){FuelStationLayout.Apply(map);RestaurantSiteLayout.Apply(map);}
        if(decorations)MapSupplements.DrawCrossings(map,transform);
        var ground=Mat("Ground",new Color(.33f,.40f,.32f)); var asphalt=Mat("Road",SurfaceColor("road"));
        var building=Mat("Buildings",SurfaceColor("building"));
        if(decorations)Box("Terrain",new Vector3(map.width/2,-.5f,map.depth/2),new Vector3(map.width+80,1,map.depth+80),ground,transform);
        var areaBatches=new Dictionary<string,Dictionary<Vector2Int,GeometryBatch>>();
        if(includeGeometry&&map.areas!=null)foreach(var area in map.areas){
            if(area.points==null||area.triangles==null)continue;
            Dictionary<Vector2Int,GeometryBatch> batches;if(!areaBatches.TryGetValue(area.kind,out batches)){batches=new Dictionary<Vector2Int,GeometryBatch>();areaBatches.Add(area.kind,batches);}
            AppendArea(area,batches);
        }
        foreach(var entry in areaBatches){var color=SurfaceColor(entry.Key);RenderBatches(entry.Value,entry.Key,Mat(entry.Key,color),collisions&&entry.Key=="sidewalk");}
        var roadBatches=new Dictionary<Vector2Int,GeometryBatch>();
        var buildingBatches=new Dictionary<Vector2Int,GeometryBatch>();
        if(includeGeometry)foreach(var road in map.roads) {
            AppendRoad(road,roadBatches);
        }
        RenderBatches(roadBatches,"도로 표면",asphalt);if(collisions&&map.editorDocument!=null)foreach(var r in map.roads){if(!r.id.StartsWith("editor:")||!System.Array.Exists(r.points,p=>p.elevation>.001f))continue;var vertices=new List<Vector3>();var faces=new List<int>();for(int i=1;i<r.points.Length;i++){var a=r.points[i-1].V(.045f);var b=r.points[i].V(.045f);var side=TrafficSystem.Right((b-a).normalized)*r.width*.5f;int n=vertices.Count;vertices.AddRange(new[]{a-side,b-side,b+side,a+side});faces.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();var collider=new GameObject("사용자 높이 차로 충돌 "+r.id);collider.transform.SetParent(transform);collider.AddComponent<MeshCollider>().sharedMesh=mesh;}
        if(decorations&&map.HasHd)HdRouteTrial.DrawMarkings(map,transform);
        if(decorations)ReviewedRoadFacilities.Draw(map,transform,collisions);
        if(includeGeometry)foreach(var b in map.buildings) {
            if(SceneBuildingVisibility.Hidden(map,b.id)){Debug.Log("PROVISIONAL_BUILDING_HIDDEN "+b.id+" · 원본 보존 / 생성 차로 겹침 검토 대기");continue;}
            var geometry=BuildingGeometry(b);
            if(collisions){var mesh=new Mesh();mesh.SetVertices(geometry.vertices);mesh.SetTriangles(geometry.indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var obj=new GameObject("Building "+b.id);obj.transform.SetParent(transform);obj.AddComponent<MeshCollider>().sharedMesh=mesh;}
            Batch(buildingBatches,GeometryBounds(geometry).center).Add(geometry.vertices,geometry.indices);
        }
        RenderBatches(buildingBatches,"건물 외형",building);
        if(!decorations)return;
        PassengerStopCatalog.Draw(map,transform);FuelStationLayout.Draw(map,transform);RestaurantSiteLayout.Draw(map,transform);ProjectMapScene.DrawServices(map,transform);
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
    public static Color SurfaceColor(string kind){return kind=="road"?new Color(.16f,.18f,.20f):kind=="building"?new Color(.70f,.67f,.60f):kind=="water"?new Color(.13f,.42f,.59f):kind=="land"?new Color(.39f,.47f,.32f):kind=="sidewalk"?new Color(.65f,.63f,.58f):new Color(.23f,.25f,.26f);}
    public static Bounds GeometryBounds(GeometryBatch geometry){if(geometry.vertices.Count==0)return new Bounds();var bounds=new Bounds(geometry.vertices[0],Vector3.zero);foreach(var point in geometry.vertices)bounds.Encapsulate(point);return bounds;}
    public static void AppendArea(MapArea area,Dictionary<Vector2Int,GeometryBatch> batches){
            float top=area.kind=="sidewalk"?Mathf.Max(.02f,area.height):area.kind=="water"||area.kind=="land"?.02f:.052f;
            var vertices=new List<Vector3>();foreach(var p in area.points)vertices.Add(p.V(top));var faces=new List<int>();
            for(int i=0;i<area.triangles.Length;i+=3)faces.AddRange(new[]{area.triangles[i+2],area.triangles[i+1],area.triangles[i]});
            if(area.kind=="sidewalk"){float winding=0;for(int i=0;i<area.points.Length;i++){var a=area.points[i];var b=area.points[(i+1)%area.points.Length];winding+=a.x*b.z-b.x*a.z;}
             for(int i=0;i<area.points.Length;i++){int j=(i+1)%area.points.Length,n=vertices.Count;vertices.AddRange(new[]{area.points[i].V(),area.points[j].V(),area.points[j].V(top),area.points[i].V(top)});faces.AddRange(winding>0?new[]{n,n+2,n+1,n,n+3,n+2}:new[]{n,n+1,n+2,n,n+2,n+3});}}
            Batch(batches,ProjectMapScene.Center(area.points)).Add(vertices,faces);
    }
    public static void AppendRoad(MapRoad road,Dictionary<Vector2Int,GeometryBatch> batches){
            for(int i=1;i<road.points.Length;i++) {
                var a=road.points[i-1].V(.045f);var b=road.points[i].V(.045f);var dir=(b-a).normalized;
                var right=TrafficSystem.Right(dir)*road.width*.5f;
                Batch(batches,(a+b)*.5f).Add(new[]{a-right,b-right,b+right,a+right},new[]{0,1,2,0,2,3});
            }
            foreach(var p in road.points) {
                const int sides=12;var vertices=new Vector3[sides+1];var indices=new int[sides*3];vertices[0]=p.V(.045f);
                for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;vertices[i+1]=vertices[0]+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*road.width*.5f;indices[i*3]=0;indices[i*3+1]=(i+1)%sides+1;indices[i*3+2]=i+1;}
                Batch(batches,p.V()).Add(vertices,indices);
            }
    }
    public static GeometryBatch BuildingGeometry(MapBuilding b){
            var vertices=new List<Vector3>(); var indices=new List<int>(); int n=b.points.Length;
            for(int i=0;i<n;i++) vertices.Add(b.points[i].V(b.height));
            for(int i=0;i<b.triangles.Length;i+=3) { indices.Add(b.triangles[i+2]); indices.Add(b.triangles[i+1]); indices.Add(b.triangles[i]); }
            float area=0; for(int i=0;i<n;i++){var a=b.points[i];var c=b.points[(i+1)%n];area+=a.x*c.z-c.x*a.z;}
            for(int i=0;i<n;i++) {
                int j=(i+1)%n,k=vertices.Count; vertices.Add(b.points[i].V()); vertices.Add(b.points[j].V()); vertices.Add(b.points[j].V(b.height)); vertices.Add(b.points[i].V(b.height));
                indices.AddRange(area>0?new[]{k,k+2,k+1,k,k+3,k+2}:new[]{k,k+1,k+2,k,k+2,k+3});
            }
        var geometry=new GeometryBatch();geometry.Add(vertices,indices);return geometry;
    }

}
public class FaceCamera : MonoBehaviour { void LateUpdate() { if(Camera.main) transform.rotation=Camera.main.transform.rotation; } }

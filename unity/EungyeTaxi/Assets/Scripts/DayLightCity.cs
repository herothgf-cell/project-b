using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The road glow and window faces are chunked meshes. No street lamp adds a
// real Unity Light, a collider, or an update loop.
internal sealed class DayLightCity : IDisposable {
 const int LampLimit=140,BuildingsPerCell=6;
 sealed class PatchBatch {
  public readonly List<Vector3> vertices=new List<Vector3>();
  public readonly List<Vector2> uv=new List<Vector2>();
  public readonly List<int> indices=new List<int>();
  public void Quad(Vector3 centre,Vector3 across,Vector3 along){
   int n=vertices.Count;
   vertices.Add(centre-across-along);vertices.Add(centre+across-along);vertices.Add(centre+across+along);vertices.Add(centre-across+along);
   uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);
   indices.Add(n);indices.Add(n+1);indices.Add(n+2);indices.Add(n);indices.Add(n+2);indices.Add(n+3);
  }
 }
 readonly GameObject root;
 readonly List<Mesh> meshes=new List<Mesh>();
 readonly Material lamp,road,windows,pole;
 readonly Texture2D radial,panes;
 public Material LensMaterial {get;private set;}
 public int LampCount {get;private set;}
 public int WindowCount {get;private set;}

 public DayLightCity(MapData map,TrafficSystem traffic,Transform owner){
  root=new GameObject("Shift night surfaces");root.transform.SetParent(owner,false);root.SetActive(false);
  var shader=Resources.Load<Shader>("LocalLightSurface");
  radial=Texture("Street lamp ground falloff",48,(u,v)=>{
   float distance=2*Mathf.Sqrt(Mathf.Pow(u-.5f,2)+Mathf.Pow(v-.5f,2));
   float alpha=Mathf.Clamp01(1-distance);return alpha*alpha;
  });
  panes=Texture("Lit window panes",24,(u,v)=>{
   if(u<.055f||u>.945f||v<.065f||v>.945f)return 0;
   if(Mathf.Abs(u-.51f)<.022f)return .19f;
   return Mathf.Lerp(.71f,1,v);
  });
  pole=Surface(shader,"Street lamp posts",Texture2D.whiteTexture,new Color(.19f,.22f,.26f,1));
  lamp=Surface(shader,"Street lamp faces",Texture2D.whiteTexture,new Color(1,.78f,.34f,1));
  road=Surface(shader,"Street lamp road glow",radial,new Color(1,.71f,.30f,0));
  windows=Surface(shader,"Lit building windows",panes,new Color(1,.73f,.31f,0));
  LensMaterial=Surface(shader,"Vehicle headlamp lens",radial,new Color(1,.9f,.66f,0));
  BuildStreetLamps(map,traffic);
  BuildWindows(map);
 }
 static Material Surface(Shader shader,string name,Texture texture,Color color){
  return new Material(shader){name=name,mainTexture=texture,color=color};
 }
 static Texture2D Texture(string name,int size,Func<float,float,float> opacity){
  var result=new Texture2D(size,size,TextureFormat.RGBA32,false,true){name=name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
  var pixels=new Color32[size*size];
  for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(255*opacity((x+.5f)/size,(y+.5f)/size)));
  result.SetPixels32(pixels);result.Apply(false,true);return result;
 }
 static Vector2Int Cell(Vector3 point){return new Vector2Int(Mathf.FloorToInt(point.x/144),Mathf.FloorToInt(point.z/144));}
 static PatchBatch Batch(Dictionary<Vector2Int,PatchBatch> batches,Vector3 point){
  var key=Cell(point);
  if(!batches.TryGetValue(key,out var batch)){batch=new PatchBatch();batches.Add(key,batch);}
  return batch;
 }
 void Render(string name,Dictionary<Vector2Int,PatchBatch> batches,Material material){
  foreach(var batch in batches.Values){
   var mesh=new Mesh{name=name};
   mesh.SetVertices(batch.vertices);mesh.SetUVs(0,batch.uv);mesh.SetTriangles(batch.indices,0);mesh.RecalculateBounds();meshes.Add(mesh);
   var chunk=new GameObject(name);chunk.transform.SetParent(root.transform,false);
   chunk.AddComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=chunk.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
   renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
  }
 }
 void BuildStreetLamps(MapData map,TrafficSystem traffic){
  var positions=new List<Vector3>();
  var posts=new Dictionary<Vector2Int,PatchBatch>();
  var bulbs=new Dictionary<Vector2Int,PatchBatch>();
  var pools=new Dictionary<Vector2Int,PatchBatch>();
  foreach(string kind in new[]{"parking","restaurant","rest"})foreach(var service in ProjectMapScene.Services(map,kind)){
   Vector3 side=TrafficSystem.Right(service.Direction);
   AddLamp(service.Position+side*5.8f,-side,positions,posts,bulbs,pools);
  }
  if(map.editorDocument!=null)foreach(var facility in map.editorDocument.facilities){
   if(facility.kind!="sign"&&facility.kind!="pedestrianSignal"&&facility.kind!="vehicleSignal")continue;
   var heading=Quaternion.Euler(0,facility.yaw,0)*Vector3.forward;
   AddLamp(facility.Position-heading*3.2f,TrafficSystem.Right(heading),positions,posts,bulbs,pools);
  }
  if(traffic)foreach(var junction in traffic.junctions){
   if(!junction.signalized||junction.halfWidth.Length<2)continue;
   Vector3 heading=traffic.AxisDirection(junction,0),side=TrafficSystem.Right(heading);
   for(int sign=-1;sign<=1;sign+=2){
    Vector3 kerb=junction.center+sign*(heading*(junction.halfWidth[1]+10)+side*(junction.halfWidth[0]+1.8f));
    AddLamp(kerb,-sign*side,positions,posts,bulbs,pools);
   }
  }
  foreach(var stop in map.stops){
   Vector3 side=TrafficSystem.Right(stop.Direction);
   AddLamp(stop.Kerb+side*3.6f+stop.Direction*7,-side,positions,posts,bulbs,pools);
  }
  LampCount=positions.Count;
  Render("Street lamp posts",posts,pole);Render("Street lamp glow",bulbs,lamp);Render("Warm pools on the road",pools,road);
 }
 static void AddLamp(Vector3 point,Vector3 towardRoad,List<Vector3> locations,Dictionary<Vector2Int,PatchBatch> posts,Dictionary<Vector2Int,PatchBatch> bulbs,Dictionary<Vector2Int,PatchBatch> pools){
  if(locations.Count>=LampLimit||towardRoad.sqrMagnitude<.01f)return;
  foreach(var prior in locations)if((prior-point).sqrMagnitude<32*32)return;
  locations.Add(point);towardRoad.Normalize();
  Vector3 side=TrafficSystem.Right(towardRoad);
  var shaft=Batch(posts,point);var housing=Batch(bulbs,point);var spill=Batch(pools,point);
  var top=point+Vector3.up*5.4f;
  shaft.Quad(point+Vector3.up*2.7f,side*.065f,Vector3.up*2.7f);
  shaft.Quad(point+Vector3.up*2.7f,towardRoad*.065f,Vector3.up*2.7f);
  shaft.Quad(top+towardRoad*.48f,towardRoad*.51f,Vector3.up*.055f);
  var head=top+towardRoad*.93f;
  housing.Quad(head,towardRoad*.34f,Vector3.up*.115f);
  housing.Quad(head,side*.16f,towardRoad*.25f);
  spill.Quad(point+towardRoad*3.5f+Vector3.up*.088f,side*6.2f,towardRoad*6.6f);
 }
 void BuildWindows(MapData map){
  var masks=new Dictionary<Vector2Int,PatchBatch>();
  var perCell=new Dictionary<Vector2Int,int>();
  foreach(var building in map.buildings){
   if(building.height<6||building.points.Length<3||SceneBuildingVisibility.Hidden(map,building.id))continue;
   uint seed=DrunkRideState.Hash(building.id??building.name??"window");
   if(seed%5>1)continue;
   Vector2Int cell=Cell(building.points[0].V());
   perCell.TryGetValue(cell,out int selected);
   if(selected>=BuildingsPerCell)continue;
   float area=0;
   for(int i=0;i<building.points.Length;i++){var a=building.points[i];var b=building.points[(i+1)%building.points.Length];area+=a.x*b.z-b.x*a.z;}
   int rows=Mathf.Min(6,Mathf.FloorToInt((building.height-1)/2.8f));
   var batch=Batch(masks,building.points[0].V());
   int before=WindowCount;
   for(int i=0;i<building.points.Length;i++){
    Vector3 start=building.points[i].V(),end=building.points[(i+1)%building.points.Length].V();
    Vector3 line=end-start;line.y=0;float span=line.magnitude;
    if(span<5||span>110)continue;
    Vector3 right=line/span,outward=TrafficSystem.Right(right)*(area>=0?1:-1);
    int columns=Mathf.Min(10,Mathf.FloorToInt((span-1)/2.3f));
    for(int row=0;row<rows;row++)for(int column=0;column<columns;column++){
     uint cellSeed=seed^(uint)(i*467+row*15713+column*93637);
     cellSeed=(cellSeed^(cellSeed>>16))*2246822519u;
     if(cellSeed%7>1)continue;
     var centre=Vector3.Lerp(start,end,(column+1f)/(columns+1))+Vector3.up*(1.9f+row*2.75f)+outward*.035f;
     batch.Quad(centre,right*.67f,Vector3.up*.65f);WindowCount++;
    }
   }
   if(WindowCount>before)perCell[cell]=selected+1;
  }
  Render("Lit windows",masks,windows);
 }
 public void UpdateGlow(float glow){
  bool shown=glow>=.035f;
  if(root.activeSelf!=shown)root.SetActive(shown);
  lamp.color=new Color(1,.78f,.34f,.88f*glow);
  road.color=new Color(1,.71f,.34f,.46f*glow);
  windows.color=new Color(1,.77f,.37f,.87f*glow);
  LensMaterial.color=new Color(1,.91f,.71f,.98f*glow);
 }
 public void Dispose(){
  if(root)UnityEngine.Object.Destroy(root);
  foreach(var mesh in meshes)UnityEngine.Object.Destroy(mesh);
  UnityEngine.Object.Destroy(pole);UnityEngine.Object.Destroy(lamp);UnityEngine.Object.Destroy(road);UnityEngine.Object.Destroy(windows);UnityEngine.Object.Destroy(LensMaterial);
  UnityEngine.Object.Destroy(radial);UnityEngine.Object.Destroy(panes);
 }
}

internal sealed class DayLightHeadlamps : IDisposable {
 readonly GameObject root;
 readonly Transform owner;
 readonly Light[] spots;
 readonly Transform lenses;
 readonly bool player;
 Transform target;
 bool targetTaxi;
 public bool Exists {get{return root;}}
 public DayLightHeadlamps(Transform owner,Material lens,bool player){
  this.owner=owner;this.player=player;
  root=new GameObject(player?"Player headlamps":"Nearby headlamps");root.transform.SetParent(owner,false);
  root.SetActive(false);
  spots=new Light[player?2:1];
  for(int i=0;i<spots.Length;i++){
   var beam=new GameObject("Headlamp beam");beam.transform.SetParent(root.transform,false);
   var spot=beam.AddComponent<Light>();spots[i]=spot;spot.type=LightType.Spot;
   spot.shadows=LightShadows.None;spot.range=player?24:15;spot.spotAngle=player?47:56;
   spot.renderMode=player?LightRenderMode.ForcePixel:LightRenderMode.Auto;
   spot.color=new Color(1,.92f,.75f);
   spot.cullingMask=~((1<<8)|(1<<9)|(1<<10)|(1<<30));
  }
  var glints=new GameObject("Headlamp lenses");lenses=glints.transform;lenses.SetParent(root.transform,false);
  for(int sign=-1;sign<=1;sign+=2){
   var shell=TaxiArt.Part(lenses,"Lit headlamp",new Vector3(sign*.63f,0,0),new Vector3(.28f,.14f,.025f),.012f,lens);
   shell.layer=player?8:0;
   var renderer=shell.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
 }
 public void Assign(Transform vehicle,bool taxi,float power){
  if(!root)return;
  if(!vehicle){
   if(root.activeSelf)root.SetActive(false);
   if(target){root.transform.SetParent(owner,false);target=null;}
   return;
  }
  if(target!=vehicle||targetTaxi!=taxi){
   target=vehicle;targetTaxi=taxi;root.transform.SetParent(vehicle,false);
   root.transform.localPosition=new Vector3(0,taxi?.57f:.66f,taxi?2.1f:2.13f);
   root.transform.localRotation=Quaternion.identity;
   for(int i=0;i<spots.Length;i++){
    float side=spots.Length==1?0:i==0?-.6f:.6f;
    spots[i].transform.localPosition=new Vector3(side,0,.03f);
    spots[i].transform.localRotation=Quaternion.Euler(6,(i==0?-1:1)*(player?1.7f:0),0);
   }
   lenses.localPosition=new Vector3(0,0,.027f);
  }
  Strength(power);
  if(!root.activeSelf)root.SetActive(true);
 }
 public void Strength(float power){
  if(!root||!target)return;
  for(int i=0;i<spots.Length;i++)if(spots[i])spots[i].intensity=power*(player?.94f:1.1f);
 }
 public void Dispose(){if(root)UnityEngine.Object.Destroy(root);}
}

public partial class TrafficSystem {
 struct NightReadableMaterial {
  public Material material;
  public Shader originalShader;
  public Texture originalTexture;
  public Color originalColor,originalEmission,nightColor;
  public string[] originalKeywords;
  public int originalQueue;
 }
 readonly List<NightReadableMaterial> nightReadableMaterials=new List<NightReadableMaterial>();
 Shader readabilityShader;
 public bool NightReadabilityActive {get;private set;}
 public void RegisterNightReadable(Material material,Color? nightColor=null){
  if(!material)return;
  var item=new NightReadableMaterial{
   material=material,originalShader=material.shader,originalTexture=material.mainTexture,originalColor=material.color,
   originalEmission=material.HasProperty("_EmissionColor")?material.GetColor("_EmissionColor"):Color.black,
   originalKeywords=material.shaderKeywords,originalQueue=material.renderQueue,nightColor=nightColor??material.color
  };
  nightReadableMaterials.Add(item);
  if(NightReadabilityActive)ApplyNightReadability(item,true);
 }
 public void SetNightReadability(bool active){
  if(active==NightReadabilityActive)return;
  if(active&&!readabilityShader)readabilityShader=Resources.Load<Shader>("LocalLightSurface");
  if(active&&!readabilityShader)return;
  NightReadabilityActive=active;
  foreach(var item in nightReadableMaterials)ApplyNightReadability(item,active);
 }
 void ApplyNightReadability(NightReadableMaterial item,bool active){
  var material=item.material;
  if(!material)return;
  if(active){
   // The stocked Player strips Standard emission variants that are only
   // enabled from code. Use the bundled unlit pass on the already-lit material.
   material.shader=readabilityShader;material.renderQueue=-1;
   material.mainTexture=Texture2D.whiteTexture;material.color=item.nightColor;
  }else{
   material.shader=item.originalShader;material.renderQueue=item.originalQueue;
   material.mainTexture=item.originalTexture;material.color=item.originalColor;
   material.shaderKeywords=item.originalKeywords;
   if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",item.originalEmission);
  }
 }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Visual-only variants of the existing round taxi (art-source/taxi): body proportions, paint and a
// dashboard ornament. Colliders, wheels and physics are never changed here.
public sealed class VehicleAppearance : MonoBehaviour {
 readonly Dictionary<Renderer,Material[]> originals=new Dictionary<Renderer,Material[]>();
 readonly List<Material> owned=new List<Material>();
 Transform exterior;GameObject outsideOrnament,cockpitOrnament;Vector3 baseScale=Vector3.one;
 public string Model {get;private set;}public int Color {get;private set;}public bool Ornament {get;private set;}
 static bool Paint(Material m){if(!m)return false;string n=m.name;return n.StartsWith("Taxi_Paint",StringComparison.Ordinal)||n.StartsWith("Taxi sunflower",StringComparison.Ordinal);}
 public static VehicleAppearance For(Transform taxi){var a=taxi.GetComponent<VehicleAppearance>();return a?a:taxi.gameObject.AddComponent<VehicleAppearance>();}
 public static void Apply(TaxiController taxi,PvpVehicleLook look){if(taxi)For(taxi.transform).Set(look==null?PvpVehicleLook.Default:look);}
 public static void Apply(TaxiController taxi,VehicleState state){if(taxi)For(taxi.transform).Set(new PvpVehicleLook{model=VehicleCatalog.Of(state).id,color=state==null?0:state.color,ornament=state!=null&&state.ornamentOwned&&state.ornamentPlaced});}
 void Capture(){
  if(exterior)return;exterior=transform.Find("Taxi exterior");if(!exterior)return;baseScale=exterior.localScale;
  foreach(var r in exterior.GetComponentsInChildren<Renderer>(true))originals[r]=r.sharedMaterials;
 }
 public void Set(PvpVehicleLook look){
  Capture();var spec=VehicleCatalog.Get(look.model)??VehicleCatalog.Standard;int color=look.color==1?1:0;
  Model=spec.id;Color=color;Ornament=look.ornament;
  if(exterior){
   exterior.localScale=new Vector3(baseScale.x*spec.scaleX,baseScale.y*spec.scaleY,baseScale.z*spec.scaleZ);
   foreach(var m in owned)if(m)Destroy(m);owned.Clear();
   var rgb=spec.colors[color];var tint=new Color(rgb[0],rgb[1],rgb[2]);bool standard=spec.id==VehicleCatalog.Starter&&color==0;
   foreach(var pair in originals){if(!pair.Key)continue;var mats=(Material[])pair.Value.Clone();for(int i=0;i<mats.Length;i++)if(!standard&&Paint(mats[i])){var copy=new Material(mats[i]){name=mats[i].name+" · "+spec.id+color};copy.color=tint;owned.Add(copy);mats[i]=copy;}pair.Key.sharedMaterials=mats;}
  }
  SetOrnament();
 }
 void SetOrnament(){
  if(!outsideOrnament&&exterior)outsideOrnament=Build(exterior.parent,new Vector3(.12f,.98f,.78f),8);
  var cockpit=transform.Find("Taxi cockpit");if(!cockpitOrnament&&cockpit)cockpitOrnament=Build(cockpit,new Vector3(.55f,-.43f,.62f),10);
  if(outsideOrnament)outsideOrnament.SetActive(Ornament);if(cockpitOrnament)cockpitOrnament.SetActive(Ornament);
 }
 // A small potted plant: neutral, readable at cockpit distance, no collider.
 static GameObject Build(Transform parent,Vector3 at,int layer){
  var root=new GameObject("Dashboard ornament");root.transform.SetParent(parent,false);root.transform.localPosition=at;
  TaxiArt.Part(root.transform,"Ornament pot",Vector3.zero,new Vector3(.07f,.06f,.07f),.02f,WorldBuilder.Mat("Ornament pot",new Color(.78f,.45f,.3f)));
  TaxiArt.Part(root.transform,"Ornament leaves",new Vector3(0,.06f,0),new Vector3(.09f,.08f,.09f),.04f,WorldBuilder.Mat("Ornament leaves",new Color(.3f,.62f,.3f)));
  foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;root.SetActive(false);return root;
 }
 void OnDestroy(){foreach(var m in owned)if(m)Destroy(m);}
}

// Account-level PVP unlock file, separate from the campaign checkpoint. The campaign only adds to
// it; PVP only reads it (plus a local selection preference in PlayerPrefs).
public static class VehicleUnlockStore {
 public const string FileName="pvp-vehicle-unlocks.json";const string SelectionKey="pvp.vehicleLook.v1";
 static string overridePath;
 public static void UseDirectory(string directory){overridePath=string.IsNullOrEmpty(directory)?null:Path.Combine(directory,FileName);}
 public static string FilePath {get{string arg=ContentTuningRuntime.Argument("-vehicleUnlocks");if(!string.IsNullOrEmpty(arg))return Path.GetFullPath(arg);return overridePath??Path.Combine(CampaignSaveLocation.DirectoryFor(Path.GetDirectoryName(Application.persistentDataPath)),FileName);}}
 public static string LastError {get;private set;}
 public static VehicleUnlocks Load(){
  LastError="";string path=FilePath;try{if(!File.Exists(path))return new VehicleUnlocks();var u=JsonUtility.FromJson<VehicleUnlocks>(File.ReadAllText(path));if(u==null)throw new ArgumentException("empty");if(u.models==null)u.models=new string[0];if(u.colors==null)u.colors=new string[0];u.Validate();return u;}
  catch(Exception e){LastError="PVP 외형 목록 읽기 실패: "+e.Message;Debug.LogWarning(LastError+" ["+path+"]");return new VehicleUnlocks();}
 }
 // Union with the stored list; a corrupt file is preserved aside instead of overwritten silently.
 public static bool Record(VehicleState state){
  string path=FilePath;try{
   VehicleUnlocks current;try{current=File.Exists(path)?JsonUtility.FromJson<VehicleUnlocks>(File.ReadAllText(path)):new VehicleUnlocks();if(current==null)throw new ArgumentException("empty");current.Validate();}
   catch(Exception){if(File.Exists(path))File.Copy(path,path+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"),true);current=new VehicleUnlocks();}
   if(!current.Record(state)&&File.Exists(path))return true;
   Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(current,true));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);LastError="";return true;
  }catch(Exception e){LastError="PVP 외형 저장 실패: "+e.Message;Debug.LogWarning(LastError);return false;}
 }
 public static PvpVehicleLook Selected {
  get{try{
   // Test fixtures pin the selection per process ("model:color:ornament"); ownership still applies.
   string pinned=ContentTuningRuntime.Argument("-pvpVehicleLook");if(!string.IsNullOrEmpty(pinned)){var p=pinned.Split(':');return PvpVehicleLook.Owned(new PvpVehicleLook{model=p[0],color=p.Length>1&&p[1]=="1"?1:0,ornament=p.Length>2&&p[2]=="1"},Load());}
   var raw=PlayerPrefs.GetString(SelectionKey,"");var look=raw.Length==0?null:JsonUtility.FromJson<PvpVehicleLook>(raw);return PvpVehicleLook.Owned(look,Load());}catch(Exception){return PvpVehicleLook.Default;}}
  set{PlayerPrefs.SetString(SelectionKey,JsonUtility.ToJson(PvpVehicleLook.Sanitize(value)));PlayerPrefs.Save();}
 }
}

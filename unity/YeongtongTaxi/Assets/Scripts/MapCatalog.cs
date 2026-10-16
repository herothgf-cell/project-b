using UnityEngine;

public static class MapCatalog {
 static readonly System.Collections.Generic.Dictionary<string,bool> availability=new System.Collections.Generic.Dictionary<string,bool>();
 public static readonly string[] Ids={"yeongtong","eungye"};
 public static string Name(string id){return id=="route42"?"은계 · 국도 42호선 차로 시험":id=="eungye"?"시흥 은계지구":"영통·반달공원";}
 static string Resource(string id){return id=="eungye"?(Resources.Load<TextAsset>("eungye-hybrid-map")?"eungye-hybrid-map":"eungye-map"):"drive-map";}
 public static bool Available(string id){bool ready;if(availability.TryGetValue(id,out ready))return ready;ready=false;var asset=Resources.Load<TextAsset>(Resource(id));
  if(asset)try{var map=JsonUtility.FromJson<MapData>(asset.text);ready=map!=null&&map.roads!=null&&map.roads.Length>0&&map.targets!=null&&map.targets.Length>=2&&map.stops!=null&&map.stops.Length>=4;}catch{ready=false;}
  availability[id]=ready;return ready;
 }
 public static string NextRegion {get{return PlayerPrefs.GetString("next-region","yeongtong");}}
 public static bool SelectNext(string id){if(!Available(id))return false;PlayerPrefs.SetString("next-region",id);PlayerPrefs.Save();return true;}
 public static MapData Load(){
  if(HdRouteTrial.Enabled){var hd=Resources.Load<TextAsset>("route42-map");if(!hd)throw new System.InvalidOperationException("tools/import_hdmap.py 로 국도42호선 자료를 생성하세요.");return JsonUtility.FromJson<MapData>(hd.text);}
  var args=System.Environment.GetCommandLineArgs();string region=NextRegion;foreach(var arg in args)if(arg.EndsWith("Test")||arg=="-assistedTrial")region="yeongtong";
  if(System.Array.IndexOf(args,"-eungye")>=0||System.Array.IndexOf(args,"-eungyeTest")>=0||System.Array.IndexOf(args,"-hybridTest")>=0)region="eungye";
  if(!Available(region)){Debug.LogWarning(Name(region)+" 지도 자료/검토 지점 미준비 · 영통으로 시작합니다");region="yeongtong";}
  var asset=Resources.Load<TextAsset>(Resource(region));if(!asset)throw new System.InvalidOperationException("지도 JSON이 없습니다. tools/import_ngii_map.py build --config config/maps/yeongtong.json 을 실행하세요.");
  var result=JsonUtility.FromJson<MapData>(asset.text);result.mapId=region;return result;
 }
}

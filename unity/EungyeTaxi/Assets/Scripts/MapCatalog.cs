using UnityEngine;

// The shipping project has one district. Old preferences cannot select another map.
public static class MapCatalog {
 public static string LockedRegion="";
 public static string[] Ids {get{return new[]{"eungye"};}}
 public static string Name(string id){return id=="pvp-city-a"?"확장 A2 · PVP 도시":id=="route42"?"은계 · 국도 42호선 차로 시험":id=="eungye"?"시흥 은계지구":"지도";}
 static string Resource {get{return Resources.Load<TextAsset>("eungye-hybrid-map")?"eungye-hybrid-map":"eungye-map";}}
 public static bool Available(string id){if(id!="eungye")return false;if(Resources.Load<TextAsset>(ProjectMapStore.ResourceName))return true;var asset=Resources.Load<TextAsset>(Resource);if(!asset)return false;try{var map=JsonUtility.FromJson<MapData>(asset.text);return map!=null&&map.roads!=null&&map.roads.Length>0&&map.targets!=null&&map.targets.Length>=2&&map.stops!=null&&map.stops.Length>=4;}catch{return false;}}
 public static string NextRegion {get{return "eungye";}}
 public static bool SelectNext(string id){if(PvpSimulationMode.Active||RoadMapRuntime.Trial||id!="eungye"||!Available(id))return false;PlayerPrefs.SetString("next-region","eungye");PlayerPrefs.Save();return true;}
 public static MapData Load(){if(PvpSimulationMode.Active&&PvpSimulationMode.Expanded)return PvpCityMap.Create();
  if(HdRouteTrial.Enabled){var hd=Resources.Load<TextAsset>("route42-map");if(!hd)throw new System.InvalidOperationException("tools/import_hdmap.py 로 은계 국도42호선 자료를 생성하세요.");return JsonUtility.FromJson<MapData>(hd.text);}
  var project=Resources.Load<TextAsset>(ProjectMapStore.ResourceName);if(project){var package=ProjectMapPackage.Parse(project.text);var composed=RoadMapRuntime.Apply(package.baseline,package.document,package.contentHash);EungyeConnectivityRepair.Apply(composed,RoadMapRuntime.Baseline);return composed;}
  if(!Available("eungye"))throw new System.InvalidOperationException("은계 프로젝트 지도가 없습니다. 정식 config/maps/project/eungye.project.json을 준비하고 Taxi > Build Eungye Hybrid를 실행하세요.");
  var asset=Resources.Load<TextAsset>(Resource);var result=JsonUtility.FromJson<MapData>(asset.text);result.mapId="eungye";MapSupplements.Apply(result,asset.bytes);ReviewedLaneLayout.Apply(result);result=RoadMapRuntime.Apply(result);CorridorPolish.Apply(result);EungyeConnectivityRepair.Apply(result,RoadMapRuntime.Baseline);return result;
 }
}

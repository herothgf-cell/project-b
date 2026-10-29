using UnityEngine;
namespace Pvp.Map {
public static class PvpMapLoader {
 public static PvpMapRuntime Build(PvpMapDefinition definition,Transform parent=null){
  var map=new PvpMapRuntime(definition);var root=new GameObject("PVP district "+definition.MapId);if(parent!=null)root.transform.SetParent(parent,false);
  var resources=root.AddComponent<PvpMapVisualResources>();
  PvpDistrictScenery.Build(map,root.transform,resources);
  return map;
 }

}}

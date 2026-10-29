using System.Collections.Generic;
using UnityEngine;

namespace Pvp.Map {
// A session owns generated visual materials through its map root. Destroying
// that root also releases resources on repeated practice-session entry/exit.
public sealed class PvpMapVisualResources : MonoBehaviour {
 readonly List<Material> materials = new List<Material>();
 public Material CreateMaterial(string shaderName, Color color) {
  var material = new Material(Shader.Find(shaderName));
  material.color = color;
  materials.Add(material);
  return material;
 }
 void OnDestroy() {
  foreach (var material in materials) {
   if (material == null) continue;
   if (Application.isPlaying) Object.Destroy(material);
   else Object.DestroyImmediate(material);
  }
  materials.Clear();
 }
}
}

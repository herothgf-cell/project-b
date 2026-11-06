using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class TaxiBlenderImport {
 [Serializable] public class Document {public int version;public Surface[] materials;public Geometry[] meshes;}
 [Serializable] public class Surface {public string name;public float[] color;public float metallic,smoothness;}
 [Serializable] public class Geometry {public string name,group,material;public float[] vertices,normals;public int[] triangles;}
 const string Folder="Assets/Resources/TaxiConcept";
 [MenuItem("Taxi/Art/Import Blender Taxi")]
 public static void Import(){
  string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../art-source/taxi/unity-meshes.json"));
  var doc=JsonUtility.FromJson<Document>(File.ReadAllText(path));if(doc.version!=1)throw new Exception("Unknown taxi mesh schema");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var materials=new Dictionary<string,Material>();foreach(var s in doc.materials){string p=Folder+"/"+s.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,p);}mat.color=new Color(s.color[0],s.color[1],s.color[2],1).gamma;mat.SetFloat("_Metallic",s.metallic);mat.SetFloat("_Glossiness",s.smoothness);EditorUtility.SetDirty(mat);materials.Add(s.name,mat);}
  int total=0;foreach(string group in new[]{"body","wheel"}){var root=new GameObject(group=="body"?"Taxi exterior":"Tyre visual");try{
   foreach(var g in doc.meshes){if(g.group!=group)continue;if(g.vertices.Length!=g.normals.Length||g.vertices.Length%3!=0)throw new Exception("Invalid taxi mesh vectors");var vertices=new Vector3[g.vertices.Length/3];var normals=new Vector3[vertices.Length];for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(g.vertices[i*3],g.vertices[i*3+1],g.vertices[i*3+2]);normals[i]=new Vector3(g.normals[i*3],g.normals[i*3+1],g.normals[i*3+2]);if(float.IsNaN(vertices[i].x)||float.IsInfinity(vertices[i].x)||normals[i].sqrMagnitude<.9f)throw new Exception("Invalid taxi vertex");}
    string p=Folder+"/"+g.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,p);}mesh.Clear();mesh.name=g.name;mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=g.triangles;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);var child=new GameObject(g.material);child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=materials[g.material];total+=g.triangles.Length/3;
   }
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+(group=="body"?"Body":"Wheel")+".prefab");
  }finally{UnityEngine.Object.DestroyImmediate(root);}}
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("TAXI_BLENDER_IMPORT_OK triangles="+total+" materials="+materials.Count);
 }
}

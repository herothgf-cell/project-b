using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 2026-11-23 build size: Resources UI artwork shipped mostly uncompressed (RGBA32/RGB24, ~25 MB of the
// 54 MB resources.assets). Windows players use BC7 at best quality for the large images (8 bits per pixel).
// BC7 needs every stored mip level to be a multiple of 4 pixels, otherwise Unity silently keeps RGBA32:
// - facility photos were cropped once to 832x468 (ScaleAndCrop draws them, so the aspect must stay);
// - the portrait sheet (drawn with 0.5 UV cells) and the room glow layers (stretched into fixed room
//   rectangles) are stored at the nearest power of two.
// Small images and the room backdrop (already DXT1 via its power-of-two scale) keep their defaults.
public static class UiTextureCompression {
 static readonly Dictionary<string,bool> Targets=new Dictionary<string,bool>{
  // path, store at nearest power of two
  {"Assets/Resources/UI/passenger-portraits.png",true},
  {"Assets/Resources/room-lamp-0-glow.png",true},{"Assets/Resources/room-lamp-1-glow.png",true},
  {"Assets/Resources/WorkshopParts/comfort-damper.png",false},{"Assets/Resources/WorkshopParts/cream-stripe.png",false},
  {"Assets/Resources/WorkshopParts/eco-filter.png",false},{"Assets/Resources/WorkshopParts/touring-spoiler.png",false},
  {"Assets/Resources/UI/Facilities/diner.jpg",false},{"Assets/Resources/UI/Facilities/fuel.jpg",false},
  {"Assets/Resources/UI/Facilities/wash.jpg",false},{"Assets/Resources/UI/Facilities/workshop.jpg",false},
  {"Assets/Resources/UI/Facilities/rest.jpg",false},{"Assets/Resources/UI/Facilities/parking.jpg",false},
  {"Assets/Resources/UI/Facilities/garage.jpg",false},{"Assets/Resources/UI/Facilities/room.jpg",false},
 };
 static string[] Paths(){
  return Directory.GetFiles("Assets/Resources","*.*",SearchOption.AllDirectories)
   .Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase))
   .Select(p=>p.Replace('\\','/')).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
 }
 public static void Apply(){
  int changed=0;
  foreach(var path in Paths()){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   var standalone=importer.GetPlatformTextureSettings("Standalone");bool dirty=false;
   if(Targets.TryGetValue(path,out bool powerOfTwo)){
    if(!standalone.overridden||standalone.format!=TextureImporterFormat.BC7||standalone.compressionQuality!=100){
     standalone.overridden=true;standalone.format=TextureImporterFormat.BC7;standalone.compressionQuality=100;standalone.maxTextureSize=importer.maxTextureSize;
     importer.SetPlatformTextureSettings(standalone);dirty=true;
    }
    if(powerOfTwo&&importer.npotScale!=TextureImporterNPOTScale.ToNearest){importer.npotScale=TextureImporterNPOTScale.ToNearest;dirty=true;}
   }else if(standalone.overridden){standalone.overridden=false;importer.SetPlatformTextureSettings(standalone);dirty=true;}
   if(dirty){importer.SaveAndReimport();changed++;}
  }
  Debug.Log("UI_TEXTURE_COMPRESSION changed="+changed);
  Report();
 }
 // Lists the stored format and size each Resources image takes in a Windows player.
 public static void Report(){
  var storage=Type.GetType("UnityEditor.TextureUtil, UnityEditor").GetMethod("GetStorageMemorySizeLong",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
  long total=0;var missed=new List<string>();
  foreach(var path in Paths()){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);long bytes=(long)storage.Invoke(null,new object[]{texture});total+=bytes;
   if(Targets.ContainsKey(path)&&texture.format!=TextureFormat.BC7)missed.Add(path);
   Debug.Log("UI_TEXTURE "+path+" "+texture.width+"x"+texture.height+" "+texture.format+" "+(bytes/1048576f).ToString("F2")+"MB");
  }
  Debug.Log("UI_TEXTURE_TOTAL "+(total/1048576f).ToString("F2")+"MB");
  if(missed.Count>0)throw new Exception("BC7 not applied (size must be a multiple of 4 at every mip): "+string.Join(", ",missed));
 }
}

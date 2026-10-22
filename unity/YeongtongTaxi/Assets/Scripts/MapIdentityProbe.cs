using System.Collections;
using System.IO;
using UnityEngine;
public class MapIdentityProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-mapIdentityCheck")>=0)new GameObject("Map identity check").AddComponent<MapIdentityProbe>();}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);
  var session=FindObjectOfType<DriveSession>();var map=session.world.map;
  var polish=CorridorPolish.For(map);
  bool ok=polish!=null&&FuelStationLayout.Rest(map)!=null&&map.mapId=="eungye"&&map.hybrid&&map.coordinateStatus=="verified"&&map.width==2100&&map.depth==1750&&map.stops.Length==12&&map.roads.Length==889+polish.ribbons+FuelStationLayout.For(map).Count*3+(FuelStationLayout.Rest(map)!=null?3:0)+(RestaurantSiteLayout.For(map)!=null?3:0)&&FuelStationLayout.For(map).Count==3;
  string text="success="+ok+"\nmap="+map.mapId+" hybrid="+map.hybrid+" extent="+map.width+"x"+map.depth+" roads="+map.roads.Length+" stops="+map.stops.Length+"\nsource="+map.source;
  Directory.CreateDirectory(Path.GetFullPath("artifacts"));
  File.WriteAllText(Path.GetFullPath("artifacts/map-identity.txt"),text);Debug.Log(text);
  if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureMap")>=0){session.business.preparing=false;session.business.enabledCampaign=false;session.business.taxPrompt=false;Time.timeScale=1;session.taxi.automated=true;session.taxi.autoBrake=true;yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/eungye-direct-launch.png"));yield return new WaitForSecondsRealtime(1);}
  Application.Quit(ok?0:1);
 }
}

using System.Collections;
using System.IO;
using UnityEngine;
public class RoadPresentationProbe : MonoBehaviour {
    public DriveSession session;
    IEnumerator Start(){
        yield return new WaitForSeconds(1);session.drivingCamera.enabled=false;
        var j=session.world.map.hybrid?session.traffic.junctions.Find(x=>x.name=="J5750_2576"):session.traffic.junctions[0];var forward=j.facility!=null&&j.facility.approaches.Length>0?new Vector3(j.facility.approaches[0].dx,0,j.facility.approaches[0].dz):session.traffic.mainDirection;var cam=Camera.main;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts"));
        string prefix=session.world.map.hybrid?"facility":"road";
        Capture(cam,j.center-forward*35+Vector3.up*9,j.center+Vector3.up*2,Path.Combine(folder,prefix+"-junction-view.png"));
        var root=j.lamps[0][0].transform.parent;var face=root.TransformPoint(new Vector3(j.lamps[0][0].transform.localPosition.x+.9f,3.7f,0));
        Capture(cam,face-forward*7,face,Path.Combine(folder,prefix+"-sign-front.png"));
        Capture(cam,face+forward*7,face,Path.Combine(folder,prefix+"-sign-back.png"));
        Debug.Log("ROAD_PRESENTATION_CAPTURE_OK device="+SystemInfo.graphicsDeviceName);Application.Quit(0);
    }
    void Capture(Camera cam,Vector3 eye,Vector3 target,string path){
        cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));cam.fieldOfView=50;
        var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;cam.targetTexture=texture;cam.Render();RenderTexture.active=texture;
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);texture.Release();Destroy(texture);
    }
}

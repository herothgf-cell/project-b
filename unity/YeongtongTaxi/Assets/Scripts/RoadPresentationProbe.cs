using System.Collections;
using System.IO;
using UnityEngine;
public class RoadPresentationProbe : MonoBehaviour {
    public DriveSession session;
    IEnumerator Start(){
        yield return new WaitForSeconds(1);session.drivingCamera.enabled=false;
        var j=session.traffic.junctions[0];var forward=session.traffic.mainDirection;var cam=Camera.main;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts"));
        Capture(cam,j.center-forward*26+Vector3.up*7,j.center+Vector3.up*2,Path.Combine(folder,"road-junction-view.png"));
        var root=j.lamps[1][0].transform.parent;
        Capture(cam,root.position-forward*7+Vector3.up*4,root.position+Vector3.up*4,Path.Combine(folder,"road-sign-front.png"));
        Capture(cam,root.position+forward*7+Vector3.up*4,root.position+Vector3.up*4,Path.Combine(folder,"road-sign-back.png"));
        Debug.Log("ROAD_PRESENTATION_CAPTURE_OK device="+SystemInfo.graphicsDeviceName);Application.Quit(0);
    }
    void Capture(Camera cam,Vector3 eye,Vector3 target,string path){
        cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));cam.fieldOfView=50;
        var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;cam.targetTexture=texture;cam.Render();RenderTexture.active=texture;
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);texture.Release();Destroy(texture);
    }
}

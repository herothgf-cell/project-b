using UnityEngine;
public class AssistCamera : MonoBehaviour {
 public DriveSession session; public string Mode {get;private set;}=""; Camera cameraView;
 public bool Visible {get{return cameraView&&cameraView.enabled;}}
 void Awake(){cameraView=gameObject.AddComponent<Camera>();cameraView.depth=2;cameraView.rect=new Rect(.745f,.06f,.24f,.25f);cameraView.fieldOfView=70;cameraView.nearClipPlane=.1f;cameraView.farClipPlane=650;cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.48f,.65f,.8f);cameraView.cullingMask=~((1<<8)|(1<<10));cameraView.enabled=false;}
 void LateUpdate(){if(!session)return;var taxi=session.taxi;int side=session.signals.direction;
  bool reverse=taxi.Speed<-.25f||(taxi.ReverseRequested&&taxi.Speed<.3f);
  Mode=reverse?"REAR CAMERA":side<0?"LEFT CAMERA":side>0?"RIGHT CAMERA":"";cameraView.enabled=!session.business.ShowingSummary&&Mode!=""&&(!session.drivingCamera.FirstPerson||reverse);if(!cameraView.enabled)return;
  var t=taxi.transform;transform.position=t.TransformPoint(reverse?new Vector3(0,1,-2.2f):new Vector3(side*.96f,1.05f,.65f));
  transform.rotation=t.rotation*Quaternion.Euler(10,reverse?180:side*115,0);
 }
}

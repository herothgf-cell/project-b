using UnityEngine;
public class CockpitMirrors : MonoBehaviour {
 public DriveSession session;public Camera[] views=new Camera[3];public RenderTexture[] textures=new RenderTexture[3];public int renderedFrames;float nextFrame;
 public bool Visible {get{return session&&!session.business.ShowingSummary&&session.drivingCamera.FirstPerson&&!session.drivingCamera.RearView;}}
 void Start(){for(int i=0;i<3;i++){var obj=new GameObject(new[]{"좌측 사이드미러","룸미러","우측 사이드미러"}[i]);obj.transform.SetParent(transform);var c=obj.AddComponent<Camera>();views[i]=c;textures[i]=new RenderTexture(i==1?512:384,192,16);textures[i].name=obj.name;textures[i].Create();c.targetTexture=textures[i];c.enabled=false;c.fieldOfView=i==1?40:65;c.nearClipPlane=.15f;c.farClipPlane=180;c.cullingMask=~((1<<8)|(1<<10));c.allowHDR=false;c.clearFlags=CameraClearFlags.Skybox;c.backgroundColor=new Color(.57f,.74f,.84f);}}
 void LateUpdate(){if(!Visible||Time.unscaledTime<nextFrame)return;nextFrame=Time.unscaledTime+1f/30;var t=session.taxi.transform;
  for(int i=0;i<3;i++){int side=i-1;var c=views[i];c.transform.position=t.TransformPoint(i==1?new Vector3(0,1.25f,.5f):new Vector3(side*.99f,1.15f,.65f));c.transform.rotation=t.rotation*Quaternion.Euler(5,i==0?-155:i==2?155:180,0);c.Render();}renderedFrames++;
 }
 public Rect Frame(int i){return i==1?new Rect(Screen.width*.44f,18,Screen.width*.20f,80):new Rect(i==0?16:Screen.width-226,Screen.height*.46f,210,115);}
 void OnGUI(){if(!Visible||textures[0]==null)return;GUI.depth=-20;for(int i=0;i<3;i++){var r=Frame(i);GUI.color=new Color(.06f,.07f,.08f);GUI.DrawTexture(new Rect(r.x-4,r.y-4,r.width+8,r.height+8),Texture2D.whiteTexture);GUI.color=Color.white;GUI.DrawTextureWithTexCoords(r,textures[i],new Rect(1,0,-1,1));var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=12,alignment=TextAnchor.MiddleCenter};label.normal.textColor=Color.white;GUI.Box(new Rect(r.x,r.y+r.height-20,r.width,20),GUIContent.none);GUI.Label(new Rect(r.x,r.y+r.height-20,r.width,20),new[]{"좌측 사이드미러","룸미러","우측 사이드미러"}[i],label);}}
 void OnDestroy(){foreach(var texture in textures)if(texture){texture.Release();Destroy(texture);}}
}

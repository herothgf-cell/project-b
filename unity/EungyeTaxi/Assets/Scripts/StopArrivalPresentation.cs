using UnityEngine;

// Only the current passenger target is highlighted; no collider or route waypoint.
public sealed class StopArrivalPresentation:MonoBehaviour {
 public DriveSession session;
 GameObject bay,labelObject;TextMesh label;Renderer[] strokes;Material material;
 public StopArrivalHint Current {get{return StopArrivalGuidance.For(session);}}
 void Build(){
  material=WorldBuilder.Mat("Active passenger bay",MapPresentation.Pickup);bay=new GameObject("현재 승하차 정차 칸");bay.transform.SetParent(transform,false);
  foreach(float x in new[]{-1.1f,1.1f})WorldBuilder.Box("칸 테두리",new Vector3(x,.13f,0),new Vector3(.14f,.025f,9),material,bay.transform,false);
  foreach(float z in new[]{-4.45f,4.45f})WorldBuilder.Box("칸 끝",new Vector3(0,.13f,z),new Vector3(2.2f,.025f,.14f),material,bay.transform,false);
  WorldBuilder.Box("방향",new Vector3(0,.14f,.8f),new Vector3(.14f,.025f,2.2f),material,bay.transform,false);
  foreach(int side in new[]{-1,1}){var wing=WorldBuilder.Box("방향 화살표",new Vector3(side*.27f,.14f,1.4f),new Vector3(.14f,.025f,.9f),material,bay.transform,false);wing.transform.localRotation=Quaternion.Euler(0,-side*45,0);}
  strokes=bay.GetComponentsInChildren<Renderer>();labelObject=new GameObject("현재 승하차 행동 안내");labelObject.transform.SetParent(transform,false);label=labelObject.AddComponent<TextMesh>();label.fontSize=64;label.characterSize=.065f;label.anchor=TextAnchor.LowerCenter;KoreanText.Apply(label);labelObject.AddComponent<FaceCamera>();
 }
 void LateUpdate(){
  if(!session||!session.StartupReady)return;if(!bay)Build();var h=Current;bool show=h.Visible&&h.Distance<180&&!session.business.preparing&&!session.PracticeBusy;
  bay.SetActive(show);labelObject.SetActive(show&&h.Distance<65);if(!show)return;
  bay.transform.SetPositionAndRotation(h.Stop.Kerb,Quaternion.LookRotation(h.Stop.Direction));Color color=h.Ready?new Color(.12f,.85f,.4f):h.Pickup?MapPresentation.Pickup:MapPresentation.Passenger;
  foreach(var r in strokes)r.sharedMaterial.color=color;
  labelObject.transform.position=h.Stop.Kerb+h.Stop.Direction*4.8f+Vector3.up*1.6f;label.color=color;label.text=(h.Pickup?"승차 정차 칸":"하차 정차 칸")+"\n"+(h.Ready?h.Action:Mathf.CeilToInt(h.Distance)+"m · 화살표 방향으로 정차");
 }
 void OnDestroy(){if(material)Destroy(material);}
}

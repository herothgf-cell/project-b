using UnityEngine;

// 2026-11-25 map review markings batch 2: paint-only road surface marks stored as "line" facilities
// with their own type, plus a crosswalk type that removes a generated crosswalk.
// - SchoolZonePaving: red anti-skid surface of an approach lane (width x length), drawn just under the
//   generated lane paint (y .071) so arrows and lines stay visible on top.
// - RoadText: text lying on the road ("30", "어린이보호구역"), read by a driver moving along +z.
// - CrosswalkAhead: white diamond warning before a crossing (width across, length along).
// - Removed (crosswalk type): no stripes; also suppresses the generated crosswalk, its pedestrian
//   heads and ambient crossing on that approach (TrafficSystem, AmbientPedestrianPlacement).
// None of these is read by lane, signal or law logic.
public static class RoadSurfaceMarks {
 public const string SchoolZonePaving="보호구역 적색 포장",RoadText="노면 글자",CrosswalkAhead="횡단보도 예고",Removed="없음";
 public static bool IsRemovedCrosswalk(RoadMapFacility f){return f!=null&&f.kind=="crosswalk"&&f.type==Removed;}
 public static bool Build(RoadMapFacility f,Transform root,Material white){
  if(f.kind=="crosswalk"&&f.type==Removed)return true;
  if(f.kind!="line")return false;
  if(f.type==SchoolZonePaving){
   var red=WorldBuilder.Mat("School zone paving",new Color(.55f,.13f,.1f));
   WorldBuilder.Box("보호구역 적색 포장",new Vector3(0,.064f,0),new Vector3(f.width,.002f,f.length),red,root,false);return true;}
  if(f.type==CrosswalkAhead){
   float hw=f.width*.5f,hl=f.length*.5f,side=Mathf.Sqrt(hw*hw+hl*hl),angle=Mathf.Atan2(hw,hl)*Mathf.Rad2Deg;
   foreach(var s in new[]{new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1)}){
    var edge=WorldBuilder.Box("횡단보도 예고",new Vector3(s.x*hw*.5f,.084f,s.y*hl*.5f),new Vector3(.2f,.014f,side),white,root,false);
    edge.transform.localRotation=Quaternion.Euler(0,-s.x*s.y*angle,0);}
   return true;}
  if(f.type==RoadText){
   var label=new GameObject("노면 글자");label.transform.SetParent(root,false);label.transform.localPosition=new Vector3(0,.09f,0);label.transform.localRotation=Quaternion.Euler(90,0,0);
   var mesh=label.AddComponent<TextMesh>();mesh.text=f.text;mesh.color=f.text.Contains("보호")?new Color(1,.85f,.1f):Color.white;mesh.fontSize=80;mesh.characterSize=.1f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;
   var font=KoreanText.BoldFont;mesh.font=font;font.RequestCharactersInTexture(f.text,mesh.fontSize,mesh.fontStyle);
   // Depth-tested text: the font's own GUI material ignores depth and drew "30" over the taxi (play 2026-11-26).
   label.AddComponent<RoadSignLabel>();
   var size=label.GetComponent<Renderer>().localBounds.size;if(size.x>0&&size.y>0)mesh.characterSize*=Mathf.Min(f.width/size.x,f.length/size.y);
   return true;}
  return false;
 }
}

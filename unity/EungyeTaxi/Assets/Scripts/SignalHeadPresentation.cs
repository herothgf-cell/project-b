using System.Linq;

// Physical apertures are independent of legal movements and signal timing.
// Legacy authored and generated heads have four apertures, including an unlit left lens.
public static class SignalHeadPresentation {
 public struct Head {
  public int lamps,turn;public bool verified,left;
  public string Caption {get{return lamps==0?"형식 미확인":lamps+"구 · "+(verified?"현장 확인":"게임 배치");}}
 }
 public static int LampCount(string type){return type=="3구"?3:4;}
 public static Head Resolve(MapData map,TrafficJunction junction,int axis,int sign){
  if(junction==null||!junction.signalized)return new Head();
  if(junction.editorProgram!=null){
   var document=map==null?null:map.editorDocument;
   if(document==null)return new Head();
   // Match group direction (the same identity used by RoadMapLamp), not proximity.
   var heads=document.facilities.Where(f=>f.kind=="vehicleSignal"&&f.junctionId==junction.name)
    .Select(f=>new {facility=f,group=junction.editorProgram.groups.FirstOrDefault(g=>!g.pedestrian&&g.id==f.groupId)})
    .Where(x=>(x.group==null?x.facility.axis:x.group.axis)==axis&&(x.group==null?x.facility.sign:x.group.sign)==sign).ToArray();
   if(heads.Length==0)return new Head();
   var first=heads.FirstOrDefault(x=>x.group!=null&&x.group.turn==0)??heads[0];var f=first.facility;
   int lamps=LampCount(f.type);
   // Conflicting head formats require an explicit assignment; do not pick a random one.
   if(heads.Any(x=>LampCount(x.facility.type)!=lamps))return new Head();
   bool explicitType=f.type=="3구"||f.type=="4구";
   return new Head{lamps=lamps,turn=first.group==null?0:first.group.turn,left=lamps==4&&junction.editorProgram.groups.Any(g=>!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==-1),verified=explicitType&&f.evidence!=null&&f.evidence.appearanceConfirmed&&!string.IsNullOrEmpty(f.evidence.url)};
  }
  for(int i=0;i<junction.lamps.Count;i++)if(junction.lampAxes[i]==axis&&junction.lampSigns[i]==sign){
   var lamp=junction.lamps[i];if(lamp==null||lamp.Length==0||!lamp[0])return new Head();
   bool fourth=lamp[0].transform.parent.Find("Signal lamp Left")!=null;
   return new Head{lamps=fourth?4:3,left=fourth&&junction.leftLamps[i].Length>0};
  }
  return new Head();
 }
}

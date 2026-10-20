using System.Collections.Generic;
using UnityEngine;

// Labels stay inside the minimap and avoid other labels and the player/goal markers.
public static class LawZoneMapLayout {
 static Rect Candidate(float x,float y,Vector2 size,Rect bounds){return new Rect(Mathf.Clamp(x,bounds.xMin,bounds.xMax-size.x),Mathf.Clamp(y,bounds.yMin,bounds.yMax-size.y),size.x,size.y);}
 static bool Clear(Rect r,List<Rect> occupied){foreach(var o in occupied)if(r.Overlaps(new Rect(o.x-2,o.y-2,o.width+4,o.height+4)))return false;return true;}
 static void Consider(Rect r,Vector2 anchor,List<Rect> occupied,ref Rect best,ref float score){if(!Clear(r,occupied))return;float d=(r.center-anchor).sqrMagnitude;if(d<score){score=d;best=r;}}
 public static Rect Place(Vector2 anchor,Vector2 size,Rect bounds,List<Rect> occupied){
  Rect best=Candidate(anchor.x+9,anchor.y-size.y/2,size,bounds);float score=float.MaxValue;
  foreach(float x in new[]{anchor.x+10,anchor.x-size.x-10,anchor.x-size.x/2})foreach(float y in new[]{anchor.y-size.y/2,anchor.y-size.y-10,anchor.y+10})Consider(Candidate(x,y,size,bounds),anchor,occupied,ref best,ref score);
  for(float y=bounds.yMin;y<=bounds.yMax-size.y;y+=8)for(float x=bounds.xMin;x<=bounds.xMax-size.x;x+=8)Consider(new Rect(x,y,size.x,size.y),anchor,occupied,ref best,ref score);
  return best;
 }
}

using UnityEngine;

// Pedestrians wait on the same raised sidewalk that ends the painted crossing.
public static class CrosswalkLanding {
 public static bool Try(MapData map,RoadMapFacility crossing,out Vector3 a,out Vector3 b){
  a=b=Vector3.zero;if(map==null||map.areas==null||crossing==null)return false;
  var across=Quaternion.Euler(0,crossing.yaw,0)*Vector3.right;
  bool Side(int sign,out Vector3 p){p=Vector3.zero;
   for(float outside=.6f;outside<=2.5f;outside+=.2f){var q=crossing.Position+across*sign*(crossing.width*.5f+outside);
    foreach(var area in map.areas){if(area.kind!="sidewalk"||area.height<.045f||area.points==null)continue;
     bool fits=true;foreach(float x in new[]{-.3f,.3f})foreach(float z in new[]{-.3f,.3f})if(!ProjectMapScene.Contains(area.points,q+new Vector3(x,0,z)))fits=false;
     if(fits){q.y=area.height;p=q;return true;}
    }
   }return false;
  }
  return Side(-1,out a)&&Side(1,out b);
 }
}

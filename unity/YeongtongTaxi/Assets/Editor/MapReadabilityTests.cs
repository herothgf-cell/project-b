using System;
using System.Globalization;
using UnityEngine;

public static class MapReadabilityTests {
 static void Check(bool value,string message){if(!value)throw new Exception("MAP_READABILITY: "+message);}
 public static void Run(){
  Check(RoadNavigation.FormatEta(1)=="1분"&&RoadNavigation.FormatEta(60)=="1분"&&RoadNavigation.FormatEta(61)=="2분","ETA rounds up to minutes");
  Check(RoadNavigation.FormatEta(0)=="0분"&&RoadNavigation.FormatEta(float.PositiveInfinity)=="계산 중","ETA handles zero/unavailable route");
  var culture=CultureInfo.CurrentCulture;
  try{
   CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
   var map=new MapData{mapId="eungye"};var p=new Vector3(123.45678f,10,987.6543f);
   var junctions=new[]{new TrafficJunction{name="J-near",center=new Vector3(123,400,987)},new TrafficJunction{name="J-far",center=Vector3.zero}};
   var location=FullMapOverlay.DescribeLocation(map,p,450,junctions);
   Check(location.Contains("시흥 은계지구 (eungye)")&&location.Contains("J-near"),"copy text contains region and closest horizontal junction");
   Check(location.Contains("x="+p.x.ToString("R",CultureInfo.InvariantCulture))&&location.Contains("z="+p.z.ToString("R",CultureInfo.InvariantCulture)),"copy text preserves exact invariant coordinates");
   Check(location.Contains("동=90): 90"),"heading normalized in degrees");
  }finally{CultureInfo.CurrentCulture=culture;}
  foreach(var words in new[]{"횡단보도\n게임 설정","↰\n비보호","비보호 좌회전\n대향차 양보 · 게임"}){
   var go=new GameObject("Fitted sign test");
   try{
    var text=go.AddComponent<TextMesh>();text.text=words;KoreanText.Fit(text,2.78f,.93f);
    var size=text.GetComponent<Renderer>().localBounds.size;
    Check(size.x>0&&size.y>0&&size.x<=2.79f&&size.y<=.94f,"Korean sign fits physical board: "+words+" / "+size);
    Check(text.GetComponent<RoadSignLabel>()!=null,"sign uses depth-aware text material");
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  Debug.Log("MAP_READABILITY_TEST_OK minutes invariantCoordinates nearestJunction KoreanSignFit");
 }
}

using System;
using System.Globalization;
using UnityEngine;

public static class MapReadabilityTests {
 static void Check(bool value,string message){if(!value)throw new Exception("MAP_READABILITY: "+message);}
 public static void Run(){
  var clip=new Rect(0,0,280,280);Vector2 a,b;
  Check(HudGlyph.TryClipMapLine(new Vector2(-100,140),new Vector2(380,140),clip,out a,out b)&&Vector2.Distance(a,new Vector2(0,140))<.001f&&Vector2.Distance(b,new Vector2(280,140))<.001f,"long map line clips to minimap instead of crossing adjacent UI");
  Check(HudGlyph.TryClipMapLine(new Vector2(140,380),new Vector2(140,-100),clip,out a,out b)&&Vector2.Distance(a,new Vector2(140,280))<.001f&&Vector2.Distance(b,new Vector2(140,0))<.001f,"vertical reverse line retains direction after clipping");
  Check(HudGlyph.TryClipMapLine(new Vector2(-20,-20),new Vector2(300,300),clip,out a,out b)&&a.sqrMagnitude<.001f&&Vector2.Distance(b,new Vector2(280,280))<.001f,"diagonal clip preserves corner endpoints");
  Check(!HudGlyph.TryClipMapLine(new Vector2(-10,10),new Vector2(-10,270),clip,out a,out b),"parallel line outside map is rejected");
  Check(HudGlyph.TryClipMapLine(new Vector2(40,60),new Vector2(40,60),clip,out a,out b)&&a==b,"zero-length in-map line retains its square cap");
  Check(!HudGlyph.TryClipMapLine(new Vector2(float.NaN,10),Vector2.one,clip,out a,out b),"invalid line cannot corrupt GUI transform");
  var raster=new MapRouteRaster(16,16);var red=new Color32(255,0,0,255);var blue=new Color32(0,120,255,255);
  raster.Line(new Vector2(-20,3),new Vector2(30,3),red,3);
  Check(raster.Pixels[(15-3)*16].r==255&&raster.Pixels[(15-3)*16+15].r==255&&raster.Pixels[3*16].a==0,"raster clips long lines and flips top-down GUI Y into texture rows");
  raster.Clear();raster.Line(new Vector2(4,4),new Vector2(12,4),red,7);raster.Line(new Vector2(4,4),new Vector2(12,4),blue,3);
  Check(raster.Pixels[(15-4)*16+8].b==255&&raster.Pixels[(15-1)*16+8].r==255&&raster.Pixels[(15-4)*16+1].r==255&&raster.Pixels[(15-4)*16+15].r==255,"raster retains two-pass colors, width and square endpoint caps");
  raster.Clear();raster.Line(new Vector2(8,8),new Vector2(8,8),blue,4);int painted=0;foreach(var pixel in raster.Pixels)if(pixel.a>0)painted++;
  Check(painted==16,"zero-length raster line has exactly one width-by-width cap");raster.Clear();raster.Line(new Vector2(-5,0),new Vector2(-5,15),red,3);Check(Array.TrueForAll(raster.Pixels,p=>p.a==0),"off-map raster stroke cannot write pixels outside clip");
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

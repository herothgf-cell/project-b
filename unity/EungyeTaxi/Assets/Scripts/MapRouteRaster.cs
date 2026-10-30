using System;
using UnityEngine;

// Bounded CPU layer for the live minimap. Input coordinates have GUI's top-down Y;
// texture rows are bottom-up. The caller owns and reuses both this buffer and texture.
public sealed class MapRouteRaster {
 public readonly int Width,Height;public readonly Color32[] Pixels;
 public MapRouteRaster(int width,int height){if(width<=0||height<=0)throw new ArgumentException("Invalid raster size");Width=width;Height=height;Pixels=new Color32[width*height];}
 public void Clear(){Array.Clear(Pixels,0,Pixels.Length);}
 public void Line(Vector2 a,Vector2 b,Color32 color,int width){
  if(width<=0)return;Vector2 start,end;if(!HudGlyph.TryClipMapLine(a,b,new Rect(0,0,Width,Height),out start,out end))return;
  int x=Mathf.RoundToInt(start.x),y=Mathf.RoundToInt(start.y),endX=Mathf.RoundToInt(end.x),endY=Mathf.RoundToInt(end.y);
  int dx=Math.Abs(endX-x),dy=-Math.Abs(endY-y),sx=x<endX?1:-1,sy=y<endY?1:-1,error=dx+dy;
  while(true){Square(x,y,width,color);if(x==endX&&y==endY)break;int twice=2*error;if(twice>=dy){error+=dy;x+=sx;}if(twice<=dx){error+=dx;y+=sy;}}
 }
 void Square(int x,int y,int width,Color32 color){
  int minX=Mathf.Max(0,Mathf.CeilToInt(x-width*.5f)),maxX=Mathf.Min(Width-1,Mathf.CeilToInt(x+width*.5f)-1);
  int minY=Mathf.Max(0,Mathf.CeilToInt(y-width*.5f)),maxY=Mathf.Min(Height-1,Mathf.CeilToInt(y+width*.5f)-1);
  for(int row=minY;row<=maxY;row++){int at=(Height-1-row)*Width;for(int col=minX;col<=maxX;col++)Pixels[at+col]=color;}
 }
}

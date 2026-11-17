using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Windows owns successfully submitted GMEM_MOVEABLE handles. Never free them
// after SetClipboardData; failed/unsubmitted allocations remain ours.
public static class RoadImageClipboard {
 public static byte[] Dib(int width,int height,Color32[] pixels){
  if(width<=0||height<=0||pixels.Length!=checked(width*height))throw new ArgumentException("Invalid image dimensions");
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((short)1);writer.Write((short)32);writer.Write(0);writer.Write(checked(width*height*4));writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
   // Unity GetPixels32 and positive-height DIB both start at the bottom row.
   foreach(var c in pixels){writer.Write(c.b);writer.Write(c.g);writer.Write(c.r);writer.Write((byte)255);}return stream.ToArray();
  }
 }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
 [DllImport("user32.dll")]static extern bool OpenClipboard(IntPtr window);
 [DllImport("user32.dll")]static extern bool CloseClipboard();
 [DllImport("user32.dll")]static extern bool EmptyClipboard();
 [DllImport("user32.dll")]static extern IntPtr SetClipboardData(uint format,IntPtr data);
 [DllImport("user32.dll")]static extern IntPtr GetClipboardData(uint format);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern uint RegisterClipboardFormat(string format);
 [DllImport("user32.dll")]static extern IntPtr GetActiveWindow();
 [DllImport("user32.dll")]static extern bool IsClipboardFormatAvailable(uint format);
 [DllImport("kernel32.dll")]static extern IntPtr GlobalAlloc(uint flags,UIntPtr bytes);
 [DllImport("kernel32.dll")]static extern IntPtr GlobalLock(IntPtr handle);
 [DllImport("kernel32.dll")]static extern bool GlobalUnlock(IntPtr handle);
 [DllImport("kernel32.dll")]static extern IntPtr GlobalFree(IntPtr handle);
 static IntPtr Allocate(byte[] bytes){var handle=GlobalAlloc(2,(UIntPtr)bytes.Length);if(handle==IntPtr.Zero)throw new OutOfMemoryException();var ptr=GlobalLock(handle);if(ptr==IntPtr.Zero){GlobalFree(handle);throw new InvalidOperationException("Clipboard memory lock failed");}try{Marshal.Copy(bytes,0,ptr,bytes.Length);}finally{GlobalUnlock(handle);}return handle;}
 public static bool HasImage {get{return IsClipboardFormatAvailable(8)&&IsClipboardFormatAvailable(RegisterClipboardFormat("PNG"));}}
 public static string ReadText(){if(!OpenClipboard(IntPtr.Zero))return "";try{var h=GetClipboardData(13);if(h==IntPtr.Zero)return "";var p=GlobalLock(h);if(p==IntPtr.Zero)return "";try{return Marshal.PtrToStringUni(p);}finally{GlobalUnlock(h);}}finally{CloseClipboard();}}
#else
 public static bool HasImage {get{return false;}}
 public static string ReadText(){return GUIUtility.systemCopyBuffer;}
#endif
 public static bool CopyText(string value){
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
  IntPtr text=IntPtr.Zero;bool opened=false;
  try{var window=GetActiveWindow();if(window==IntPtr.Zero)window=System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;text=Allocate(Encoding.Unicode.GetBytes(value+"\0"));if(window==IntPtr.Zero||!OpenClipboard(window))return false;opened=true;if(!EmptyClipboard()||SetClipboardData(13,text)==IntPtr.Zero)return false;text=IntPtr.Zero;return true;}
  catch(Exception e){Debug.LogWarning("Road location clipboard: "+e.Message);return false;}
  finally{if(opened)CloseClipboard();if(text!=IntPtr.Zero)GlobalFree(text);}
#else
  GUIUtility.systemCopyBuffer=value;return GUIUtility.systemCopyBuffer==value;
#endif
 }
 public static bool Copy(Texture2D image,string description,out string error){
  error="";
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
  IntPtr dib=IntPtr.Zero,png=IntPtr.Zero,text=IntPtr.Zero;bool opened=false;
  try{
   dib=Allocate(Dib(image.width,image.height,image.GetPixels32()));png=Allocate(image.EncodeToPNG());text=Allocate(Encoding.Unicode.GetBytes(description+"\0"));
   var window=GetActiveWindow();if(window==IntPtr.Zero)window=System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
   if(window==IntPtr.Zero||!OpenClipboard(window)){error="클립보드를 사용할 수 없어요. 잠시 후 다시 복사해 주세요.";return false;}opened=true;
   if(!EmptyClipboard())throw new InvalidOperationException("Clipboard empty failed");
   if(SetClipboardData(8,dib)==IntPtr.Zero)throw new InvalidOperationException("DIB copy failed");dib=IntPtr.Zero;
   if(SetClipboardData(RegisterClipboardFormat("PNG"),png)==IntPtr.Zero)throw new InvalidOperationException("PNG copy failed");png=IntPtr.Zero;
   if(SetClipboardData(13,text)!=IntPtr.Zero)text=IntPtr.Zero;
   return true;
  }catch(Exception e){Debug.LogWarning("Road screenshot clipboard: "+e.Message);error="이미지 복사에 실패했어요. 다시 시도해 주세요.";return false;}
  finally{if(opened)CloseClipboard();if(dib!=IntPtr.Zero)GlobalFree(dib);if(png!=IntPtr.Zero)GlobalFree(png);if(text!=IntPtr.Zero)GlobalFree(text);}
#else
  error="이미지 클립보드는 Windows 실행본에서 지원합니다.";return false;
#endif
 }
}

using System;
using UnityEngine;

// Display order is the shortcut contract, including disabled rows. Consume once
// across Update/OnGUI so a response cannot also answer its follow-up this frame.
public static class NumericChoiceInput {
 static int consumedFrame=int.MinValue;
 public static string Label(int index){return (index+1).ToString(System.Globalization.CultureInfo.InvariantCulture);}
 public static KeyCode Key(int index,bool keypad){return index>=0&&index<9?(KeyCode)((int)(keypad?KeyCode.Keypad1:KeyCode.Alpha1)+index):KeyCode.None;}
 public static bool TryConsume(int frame){if(consumedFrame==frame)return false;consumedFrame=frame;return true;}
 public static int Read(int count,Func<int,bool> enabled){return Read(count,enabled,Input.GetKeyDown,Time.frameCount);}
 public static int Read(int count,Func<int,bool> enabled,Func<KeyCode,bool> down,int frame){
  if(consumedFrame==frame)return -1;
  for(int i=0;i<Math.Min(9,count);i++)if((down(Key(i,false))||down(Key(i,true)))&&enabled(i)){TryConsume(frame);return i;}
  return -1;
 }
}

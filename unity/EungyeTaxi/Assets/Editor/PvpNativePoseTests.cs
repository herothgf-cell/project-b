using System;using UnityEngine;
public static class PvpNativePoseTests {
 static void Near(float actual,float expected){if(Mathf.Abs(actual-expected)>.001f)throw new Exception("Native pose expected "+expected+" got "+actual);}
 public static void Run(){var p=new PvpNativePoseTimeline();Vector3 v;Quaternion q;p.Push(0,Vector3.zero,Quaternion.identity);p.Push(.1,new Vector3(3,0,0),Quaternion.Euler(0,90,0));p.Push(.27,new Vector3(8.1f,0,0),Quaternion.Euler(0,90,0));p.Sample(.05,out v,out q);Near(v.x,1.5f);p.Sample(.2,out v,out q);Near(v.x,6);p.Sample(.32,out v,out q);Near(v.x,9.6f);p.Sample(1,out v,out q);Near(v.x,11.1f);p.Push(.3,new Vector3(100,0,0),Quaternion.identity);p.Sample(.1,out v,out q);Near(v.x,100);p.Push(.3,new Vector3(101,0,0),Quaternion.identity);p.Sample(.3,out v,out q);Near(v.x,101);Debug.Log("PVP_NATIVE_POSE_TIMELINE_OK checks=6");}
}

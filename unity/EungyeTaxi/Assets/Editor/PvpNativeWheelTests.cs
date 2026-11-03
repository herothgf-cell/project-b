using System;using UnityEngine;
public static class PvpNativeWheelTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Native tyre: "+why);}
 public static void Run(){checks=0;var root=new GameObject("wheel render test");var tyre=new GameObject("tyre").transform;tyre.SetParent(root.transform,false);tyre.localScale=new Vector3(.68f,.12f,.68f);var scale=tyre.localScale;
 try{var wheel=new PvpNativeWheelView();wheel.Push(0,new Vector3(.85f,-.2f,-1.3f),Quaternion.identity,true);wheel.Draw(tyre,0);wheel.Push(.1,new Vector3(.85f,0,-1.3f),Quaternion.Euler(40,0,0));
 Check(Mathf.Abs(tyre.localPosition.y+.2f)<.001f,"packet arrival does not jump rendered wheel");wheel.Draw(tyre,.05);Check(Mathf.Abs(tyre.localPosition.y+.1f)<.001f,"wheel suspension uses chassis render timestamp");Check(Quaternion.Angle(tyre.localRotation,Quaternion.Euler(20,0,0))<.1f,"wheel rotation interpolates");
 root.transform.SetPositionAndRotation(new Vector3(30,2,80),Quaternion.Euler(0,70,0));wheel.Draw(tyre,5);Check(tyre.localPosition.y==0,"delivery gap holds latest suspension instead of overshoot");Check(tyre.localScale==scale,"tyre dimensions fixed under playback and parent motion");
 var mount=new Vector3(-.85f,0,-1.3f);var p=PvpNativeWheelView.MountPosition(new Vector3(-.4f,-.12f,-1.8f),mount);Check(p.x==mount.x&&p.z==mount.z&&p.y==-.12f,"render/physics chassis offset cannot slide axle, native suspension retained");
 wheel.Push(.1,new Vector3(.85f,-.05f,-1.3f),Quaternion.identity);wheel.Draw(tyre,.1);Check(Mathf.Abs(tyre.localPosition.y+.05f)<.001f,"duplicate frame replaces sample");wheel.Push(0,new Vector3(.85f,-.15f,-1.3f),Quaternion.identity,true);wheel.Draw(tyre,0);Check(Mathf.Abs(tyre.localPosition.y+.15f)<.001f,"new session resets history");
 Debug.Log("PVP_NATIVE_WHEEL_OK checks="+checks);
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
 public static void Build(){Run();PvpNativePoseTests.Run();ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{"../../artifacts/pvp/wheel-fix-candidate/EungyeTaxi.exe","eungye"});}
}

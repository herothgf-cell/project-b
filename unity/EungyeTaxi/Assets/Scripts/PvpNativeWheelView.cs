using UnityEngine;
// Local suspension/rotation use the chassis playback clock. Never scale a tyre
// or extrapolate its suspension beyond the latest authoritative sample.
public sealed class PvpNativeWheelView {
 readonly PvpNativePoseTimeline poses=new PvpNativePoseTimeline();double newest;
 public void Push(double time,Vector3 position,Quaternion rotation,bool snap=false){newest=time;poses.Push(time,position,rotation,snap);}
 public void Draw(Transform tyre,double time){Vector3 p;Quaternion q;if(poses.Sample(System.Math.Min(time,newest),out p,out q)){tyre.localPosition=p;tyre.localRotation=q;}}
 public static Vector3 MountPosition(Vector3 observed,Vector3 mount){return new Vector3(mount.x,observed.y,mount.z);}
}

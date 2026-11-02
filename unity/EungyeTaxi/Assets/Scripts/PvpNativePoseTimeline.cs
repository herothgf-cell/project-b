using UnityEngine;
// A bounded render timeline. The authoritative Rigidbody stays on the host.
public sealed class PvpNativePoseTimeline {
 const int Capacity=16;readonly double[] times=new double[Capacity];readonly Vector3[] positions=new Vector3[Capacity];readonly Quaternion[] rotations=new Quaternion[Capacity];int count;
 public void Push(double time,Vector3 position,Quaternion rotation,bool snap=false){
  if(snap||count>0&&(time<times[count-1]||(position-positions[count-1]).sqrMagnitude>900))count=0;
  if(count>0&&time==times[count-1]){positions[count-1]=position;rotations[count-1]=rotation;return;}
  if(count==Capacity){System.Array.Copy(times,1,times,0,Capacity-1);System.Array.Copy(positions,1,positions,0,Capacity-1);System.Array.Copy(rotations,1,rotations,0,Capacity-1);count--;}
  times[count]=time;positions[count]=position;rotations[count]=rotation;count++;
 }
 public bool Sample(double time,out Vector3 position,out Quaternion rotation){
  position=Vector3.zero;rotation=Quaternion.identity;if(count==0)return false;
  if(count==1||time<=times[0]){position=positions[0];rotation=rotations[0];return true;}
  for(int i=1;i<count;i++)if(time<=times[i]){float t=(float)((time-times[i-1])/(times[i]-times[i-1]));position=Vector3.Lerp(positions[i-1],positions[i],t);rotation=Quaternion.Slerp(rotations[i-1],rotations[i],t);return true;}
  int last=count-1;double span=times[last]-times[last-1];float extra=span>0?(float)(System.Math.Min(.1,System.Math.Max(0,time-times[last]))/span):0;
  position=positions[last]+(positions[last]-positions[last-1])*extra;rotation=Quaternion.SlerpUnclamped(rotations[last-1],rotations[last],1+extra);return true;
 }
}

using System;
using UnityEngine;
using Pvp.Driving;
namespace Pvp.Network {
public static class PvpRemotePresentation {
 public static PvpVehicleState Step(PvpVehicleState current,PvpVehicleState target,float delta){
  if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
  float weight=1-Mathf.Exp(-12*delta);var visual=target;visual.Position=Vector3.Lerp(current.Position,target.Position,weight);visual.YawDegrees=Mathf.LerpAngle(current.YawDegrees,target.YawDegrees,weight);return visual;
 }
}
}
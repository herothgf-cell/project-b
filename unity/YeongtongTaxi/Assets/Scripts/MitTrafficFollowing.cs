// Adapted from mchrbn/unity-traffic-simulation VehicleAI.MoveVehicle,
// commit 4f03f4c276e2b82f033a5f037f43036ba9ab6be3 (same-direction speed matching).
// Copyright (c) 2019 Matthieu Cherubini. SPDX-License-Identifier: MIT
// Full terms: licenses/mchrbn-unity-traffic-simulation/LICENSE.txt
// Modified 2026-10-21: pure m/s policy, measured leader speed, local range,
// no reverse/steering override; existing stop/safety speed always takes precedence.
using System;
public static class MitTrafficFollowing {
 public static float Limit(bool enabled,float baseline,float gap,float leaderSpeed,float alignment,bool hasLeader){
  if(!enabled||!hasLeader||alignment<=.8f||gap>=8)return baseline;
  float matched=Math.Max(leaderSpeed-.5f,.1f);
  // Release the extra constraint continuously as the gap opens (project adaptation).
  float blend=Math.Max(0,Math.Min(1,(gap-4)/4));
  return Math.Min(baseline,matched+(Math.Max(baseline,matched)-matched)*blend);
 }
}

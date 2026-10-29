using UnityEngine;
namespace Pvp.Driving {
public enum PvpVehicleProfile { A, B }
/// <summary>Complete integration state; signed speed in m/s, yaw in degrees (zero = +Z).</summary>
[System.Serializable]
public struct PvpVehicleState {
    public Vector3 Position;
    public float YawDegrees, Speed, Steering;
    public PvpVehicleProfile Profile;
}
}

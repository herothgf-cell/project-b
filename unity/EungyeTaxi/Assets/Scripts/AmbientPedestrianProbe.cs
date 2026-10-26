using UnityEngine;

// Attached only by the isolated two-process test flags.
public sealed class AmbientPedestrianProbe : MonoBehaviour {
 public DriveSession session;
 void Start(){gameObject.AddComponent<AmbientPedestrianProbeRunner>().session=session;}
}

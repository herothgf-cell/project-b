using UnityEngine;
// Test-only witness for an actual Rigidbody contact with the diagnostic wall.
public sealed class PvpSimulationImpactWitness : MonoBehaviour {
    public Collider Wall;
    public bool Hit {get;private set;}
    void OnCollisionEnter(Collision collision){if(collision.collider==Wall)Hit=true;}
}

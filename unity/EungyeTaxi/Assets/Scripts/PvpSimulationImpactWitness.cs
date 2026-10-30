using UnityEngine;
// Test-only witness for an actual Rigidbody contact with the diagnostic wall or live NPC traffic.
public sealed class PvpSimulationImpactWitness : MonoBehaviour {
    public Collider Wall;
    public bool Hit {get;private set;}
    public string Contact {get;private set;}
    void OnCollisionEnter(Collision collision){if(collision.collider==Wall||collision.collider.GetComponentInParent<TrafficCar>()){Hit=true;Contact=collision.collider.name;}}
}

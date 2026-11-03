using UnityEngine;

// Narrow-phase sphere clearance for traffic corridor samples. Nonconvex meshes
// cannot use Collider.ClosestPoint; querying their AABB would block empty road gaps.
public static class TrafficColliderGeometry {
    static SphereCollider probe;
    public static bool WithinRadius(Collider collider,Vector3 point,float radius){
        if(!collider||radius<=0)return false;
        var mesh=collider as MeshCollider;
        if(collider is BoxCollider||collider is SphereCollider||collider is CapsuleCollider||mesh&&mesh.convex)
            return (collider.ClosestPoint(point)-point).sqrMagnitude<radius*radius;
        if(!probe){
            var root=new GameObject("Traffic clearance query sphere");root.hideFlags=HideFlags.HideAndDontSave;
            // Keep a native shape alive outside the driving world. Disabled fresh
            // colliders have no usable shape for ComputePenetration in this Editor.
            root.layer=2;root.transform.position=Vector3.down*1000000;
            probe=root.AddComponent<SphereCollider>();probe.isTrigger=true;
            if(Application.isPlaying)Object.DontDestroyOnLoad(root);
        }
        probe.radius=radius;
        Vector3 direction;float distance;
        // Sphere is the required primitive operand. This API tests explicit poses
        // without moving the query object or either scene collider.
        return Physics.ComputePenetration(probe,point,Quaternion.identity,collider,collider.transform.position,collider.transform.rotation,out direction,out distance)&&distance>0;
    }
}

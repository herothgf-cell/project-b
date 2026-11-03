using System;
using UnityEngine;

// Geometry regression for the sampled traffic-clearance sphere; no scene simulation.
public static class TrafficColliderGeometryTests {
    static void Require(bool value,string message){if(!value)throw new Exception("Traffic geometry: "+message);}
    public static void Run(){
        var root=new GameObject("Traffic clearance geometry fixture");var mesh=new Mesh();int checks=0;
        try{
            foreach(var kind in new[]{PrimitiveType.Cube,PrimitiveType.Sphere,PrimitiveType.Capsule}){
                var item=GameObject.CreatePrimitive(kind);item.transform.SetParent(root.transform);item.transform.position=new Vector3(20,2,3);item.transform.rotation=Quaternion.Euler(0,32,0);
                var collider=item.GetComponent<Collider>();
                foreach(var point in new[]{item.transform.position,item.transform.position+Vector3.right*.7f,item.transform.position+Vector3.right*3}){
                    bool expected=(collider.ClosestPoint(point)-point).sqrMagnitude<1.15f*1.15f;
                    Require(TrafficColliderGeometry.WithinRadius(collider,point,1.15f)==expected,kind+" keeps original distance semantics");checks++;
                }
            }
            var surface=new GameObject("Separated nonconvex road strips");surface.transform.SetParent(root.transform);var shape=surface.AddComponent<MeshCollider>();
            mesh.vertices=new[]{new Vector3(-5,0,-2),new Vector3(-5,0,2),new Vector3(-3,0,2),new Vector3(-3,0,-2),new Vector3(3,0,-2),new Vector3(3,0,2),new Vector3(5,0,2),new Vector3(5,0,-2)};
            mesh.triangles=new[]{0,1,2,0,2,3,4,5,6,4,6,7};mesh.RecalculateBounds();shape.sharedMesh=mesh;shape.convex=false;
            Require(TrafficColliderGeometry.WithinRadius(shape,new Vector3(-4,.5f,0),1.15f),"sphere reaches actual triangle");checks++;
            Require(!TrafficColliderGeometry.WithinRadius(shape,new Vector3(-4,1.2f,0),1.15f),"sphere outside radius is clear");checks++;
            Require(!TrafficColliderGeometry.WithinRadius(shape,new Vector3(0,.5f,0),1.15f),"gap inside mesh AABB is clear");checks++;
            surface.transform.SetPositionAndRotation(new Vector3(9,3,7),Quaternion.Euler(0,37,0));surface.transform.localScale=new Vector3(1.5f,1,1.25f);
            Require(TrafficColliderGeometry.WithinRadius(shape,surface.transform.TransformPoint(new Vector3(4,.5f,0)),1.15f),"transformed nonconvex triangle");checks++;
            Require(!TrafficColliderGeometry.WithinRadius(shape,surface.transform.TransformPoint(new Vector3(0,.5f,0)),1.15f),"transformed AABB gap remains clear");checks++;
            Require(!TrafficColliderGeometry.WithinRadius(shape,Vector3.zero,0),"zero radius preserves strict comparison");checks++;
            Debug.Log("TRAFFIC_COLLIDER_GEOMETRY_OK checks="+checks);
        }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);}
    }
}

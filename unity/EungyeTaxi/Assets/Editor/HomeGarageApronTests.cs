using System;
using System.Linq;
using UnityEngine;

public static class HomeGarageApronTests {
    public static void Run() {
        var map = ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
        var west = map.areas.Single(a => a.evidence == "sidewalk:generated:3073").points[1].V();
        var east = map.areas.Single(a => a.evidence == "sidewalk:generated:3085").points[2].V();
        var curb = (east - west).normalized;
        var root = new GameObject("Home garage pavement check");
        try {
            HomeGarage.Draw(map, root.transform, true);
            var aprons = root.GetComponentsInChildren<MeshFilter>().Where(m => m.name == "차고 진입 포장").ToArray();
            if (aprons.Length != 2) throw new InvalidOperationException("Missing entrance or exit pavement");
            foreach (var apron in aprons) {
                var corners = apron.sharedMesh.vertices.Select(apron.transform.TransformPoint).ToArray();
                // The garage is north of the west-to-east curb. Each apron
                // should meet it along an edge, with no vertex in the street.
                var distances = corners.Select(p => curb.x * (p.z - west.z) - curb.z * (p.x - west.x)).ToArray();
                if (distances.Min() < -.02f || distances.Count(d => Mathf.Abs(d) < .02f) != 2)
                    throw new InvalidOperationException("Garage apron misses or crosses the curb");
                if (apron.GetComponent<Collider>() != null || apron.sharedMesh.normals.Any(n => n.y < .99f))
                    throw new InvalidOperationException("Garage apron must render face up without obstructing vehicles");
            }
            Debug.Log("HOME_GARAGE_APRONS_OK curbs=2 road_intrusions=0");
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}

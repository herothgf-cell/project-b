using System;
using UnityEngine;
[Serializable] public class MapPoint { public float x, z; public Vector3 V(float y = 0) { return new Vector3(x,y,z); } }
[Serializable] public class MapRoad { public string id; public float width; public int sourceLanes; public int DisplayLanes { get { return sourceLanes>=4?4:2; } } public MapPoint[] points; }
[Serializable] public class MapBuilding { public string id; public float height; public MapPoint[] points; public int[] triangles; }
[Serializable] public class MapTarget : MapPoint { public string name; public LeftTurnMode leftTurnMode; }
[Serializable] public class MapStop : MapPoint { public string name; public float dx,dz,width; public Vector3 Direction {get{return new Vector3(dx,0,dz);}} public Vector3 Passenger {get{return V(.16f)+TrafficSystem.Right(Direction)*(width*.5f+.85f);}} public bool CanStop(Vector3 p,Vector3 heading,float speed){var delta=p-Kerb;return speed<.56f && Vector3.Dot(heading,Direction)>.85f && Mathf.Abs(Vector3.Dot(delta,Direction))<4.5f && Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(Direction)))<1.15f;} public Vector3 Kerb {get{return V()+TrafficSystem.Right(Direction)*(width*.5f-1.5f);}} }
[Serializable] public class MapData { public string source; public float width,depth; public MapRoad[] roads; public MapBuilding[] buildings; public MapTarget[] targets; public MapStop[] stops; }

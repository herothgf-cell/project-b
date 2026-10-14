using System.Collections.Generic;
using UnityEngine;

public class TrafficJunction {
    public Vector3 center;public TrafficCar owner;
    public readonly Dictionary<TrafficCar,float> requests=new Dictionary<TrafficCar,float>();
    public readonly List<Renderer[]> leftLamps=new List<Renderer[]>();
    public string name;
    public float offset;
    public readonly List<Renderer[]> lamps = new List<Renderer[]>();
}
public class TrafficRoute {
    public Vector3 start, end, direction;
    public float length;
    public int axis,turn;public Vector3[] points;float[] cumulative;
    public void SetPoints(Vector3[] values){points=values;cumulative=new float[points.Length];for(int i=1;i<points.Length;i++)cumulative[i]=cumulative[i-1]+Vector3.Distance(points[i-1],points[i]);start=points[0];end=points[points.Length-1];direction=(points[1]-points[0]).normalized;length=cumulative[cumulative.Length-1];}
    public Vector3 Position(float d){if(points==null)return start+direction*d;for(int i=1;i<points.Length;i++)if(d<=cumulative[i])return Vector3.Lerp(points[i-1],points[i],Mathf.InverseLerp(cumulative[i-1],cumulative[i],d));return end;}
    public Vector3 Tangent(float d){return (Position(Mathf.Min(length,d+.4f))-Position(Mathf.Max(0,d-.4f))).normalized;}

    public readonly List<TrafficStop> stops = new List<TrafficStop>();
}
public class TrafficStop { public TrafficJunction junction; public float distance,exitDistance;public int axis; }

public class TrafficSystem : MonoBehaviour {
    public readonly List<TrafficJunction> junctions = new List<TrafficJunction>();
    public readonly List<TrafficCar> cars = new List<TrafficCar>();
    public bool manualClock;
    public float manualTime;
    public float Clock { get { return manualClock ? manualTime : Time.time; } }
    public Vector3 mainDirection;
    Material white, yellow, dark, pole;
    Material[] lit, unlit;
    readonly List<Vector3> whitePaint=new List<Vector3>(), yellowPaint=new List<Vector3>();

    public SignalColor Signal(TrafficJunction junction, int axis) { return TrafficRules.Signal(Clock + junction.offset, axis); }
    public SignalColor LeftSignal(TrafficJunction junction,int axis){return TrafficRules.LeftSignal(Clock+junction.offset,axis);}
    public SignalColor MovementSignal(TrafficJunction junction,int axis,int turn){return turn<0?LeftSignal(junction,axis):Signal(junction,axis);}
    public void Request(TrafficCar car,TrafficStop stop){if(!stop.junction.requests.ContainsKey(car))stop.junction.requests[car]=Time.time;}
    public void Build(MapData map, bool spawnCars) {
        mainDirection = (map.targets[1].V() - map.targets[0].V()).normalized;
        white = WorldBuilder.Mat("White road paint", new Color(.93f,.92f,.84f));
        yellow = WorldBuilder.Mat("Yellow centre paint", new Color(1,.71f,.13f));
        dark = WorldBuilder.Mat("Signal housing", new Color(.055f,.07f,.085f));
        pole = WorldBuilder.Mat("Signal pole", new Color(.28f,.32f,.35f));
        Color[] colors = { new Color(1,.035f,.02f), new Color(1,.55f,.015f), new Color(.025f,1,.22f) };
        lit = new Material[3]; unlit = new Material[3];
        for(int i=0;i<3;i++) {
            lit[i] = WorldBuilder.Mat("Signal lit " + i, colors[i]); lit[i].EnableKeyword("_EMISSION"); lit[i].SetColor("_EmissionColor", colors[i]*2);
            unlit[i] = WorldBuilder.Mat("Signal off " + i, colors[i]*.12f);
        }
        for(int i=0;i<2;i++){var target=map.targets[i];junctions.Add(new TrafficJunction { center=target.V(), name=target.name, offset=target.name=="T1" ? 0 : 7 });}
        PaintRoads(map);
        foreach(var junction in junctions) {
            for(int axis=0;axis<2;axis++) for(int sign=-1;sign<=1;sign+=2) {
                Vector3 direction = (axis==0 ? mainDirection : Right(mainDirection))*sign;
                AddApproach(junction, direction, axis==0 ? 5 : 10, axis==0 ? 10 : 5);
            }
        }
        PaintMesh(whitePaint,white);PaintMesh(yellowPaint,yellow);
        if(spawnCars) {
            var forward = MakeRoute(junctions[0].center-mainDirection*26, junctions[1].center+mainDirection*34, 0, map);
            var backward = MakeRoute(junctions[1].center+mainDirection*34, junctions[0].center-mainDirection*26, 0, map);
            Spawn(forward,35,0); Spawn(forward,95,1); Spawn(backward,0,2); Spawn(backward,100,3);
            foreach(var j in junctions) {
                var across=Right(mainDirection);
                // Only the verified straight approaches belong to these simple AI routes.
                Vector3 a=j.center-across*Mathf.Min(60,Vector3.Distance(j.center,Edge(j.center,-across,map)));
                Vector3 b=j.center+across*Mathf.Min(60,Vector3.Distance(j.center,Edge(j.center,across,map)));
                Spawn(MakeRoute(a,b,1,map),0,cars.Count); Spawn(MakeRoute(b,a,1,map),0,cars.Count);
            }
            foreach(var j in junctions){Spawn(MakeTurn(j,1,-1),0,cars.Count);cars[cars.Count-1].Recycle();Spawn(MakeTurn(j,-1,1),0,cars.Count);cars[cars.Count-1].Recycle();}
        }
    }
    public bool MayEnter(TrafficCar car,TrafficStop stop){
        var j=stop.junction;if(j.owner==car)return true;if(j.owner)return false;
        Request(car,stop);float mine=j.requests[car];
        foreach(var pair in j.requests){var other=pair.Key;if(other==car||!other||!other.enabled||other.entryBlocked)continue;foreach(var approach in other.route.stops)if(approach.junction==j&&approach.distance-other.distance>=-.05f&&approach.distance-other.distance<=1.5f&&MovementSignal(j,approach.axis,other.route.turn)==SignalColor.Green&&ExitClear(other,approach)){if(pair.Value<mine||(pair.Value==mine&&other.GetInstanceID()<car.GetInstanceID()))return false;}}

        // Reserve one movement at a time; never enter if the exit or conflict area is occupied.
        foreach(var hit in Physics.OverlapBox(j.center+Vector3.up*.85f,new Vector3(5.4f,.55f,10.4f),Quaternion.LookRotation(mainDirection),(1<<8)|(1<<9)))if(hit.GetComponentInParent<TrafficCar>()!=car)return false;
        if(!ExitClear(car,stop))return false;
        j.owner=car;return true;
    }
    public bool ExitClear(TrafficCar car,TrafficStop stop){var exit=car.route.Position(stop.exitDistance);var dir=car.route.Tangent(stop.exitDistance);
        foreach(var hit in Physics.OverlapBox(exit+Vector3.up*.85f,new Vector3(1.05f,.55f,4.5f),Quaternion.LookRotation(dir),(1<<8)|(1<<9)|1))if(hit.GetComponentInParent<TrafficCar>()!=car)return false;return true;
    }
    public void Release(TrafficCar car){foreach(var j in junctions){if(j.owner==car)j.owner=null;j.requests.Remove(car);}}
    TrafficRoute MakeTurn(TrafficJunction j,int sign,int turn){
        var incoming=mainDirection*sign;var outgoing=Right(incoming)*turn;var route=new TrafficRoute{axis=0,turn=turn};
        var begin=j.center-incoming*24+Right(incoming)*2.5f;var entry=j.center-incoming*13+Right(incoming)*2.5f;
        var exit=j.center+outgoing*12+Right(outgoing)*(turn>0?7.5f:2.5f);var finish=exit+outgoing*32;
        var p=new List<Vector3>{begin,entry};var c1=entry+incoming*12;var c2=exit-outgoing*12;
        for(int i=1;i<=24;i++){float t=i/24f,u=1-t;p.Add(u*u*u*entry+3*u*u*t*c1+3*u*t*t*c2+t*t*t*exit);}p.Add(finish);route.SetPoints(p.ToArray());
        route.stops.Add(new TrafficStop{junction=j,axis=0,distance=24-14-2.1f,exitDistance=route.length-32+5});return route;
    }
    public static Vector3 Right(Vector3 dir) { return new Vector3(dir.z,0,-dir.x); }
    static Vector3 Edge(Vector3 center, Vector3 dir, MapData map) {
        float distance=1000;
        if(dir.x>.001f) distance=Mathf.Min(distance,(map.width-7-center.x)/dir.x);
        if(dir.x<-.001f) distance=Mathf.Min(distance,(7-center.x)/dir.x);
        if(dir.z>.001f) distance=Mathf.Min(distance,(map.depth-7-center.z)/dir.z);
        if(dir.z<-.001f) distance=Mathf.Min(distance,(7-center.z)/dir.z);
        return center+dir*distance;
    }
    TrafficRoute MakeRoute(Vector3 a, Vector3 b, int axis, MapData map) {
        var direction=(b-a).normalized; var lane=Right(direction)*2.5f;
        var route=new TrafficRoute{start=a+lane,end=b+lane,direction=direction,length=Vector3.Distance(a,b),axis=axis};
        foreach(var j in junctions) {
            float along=Vector3.Dot(j.center-a,direction);
            float sideways=Vector3.Cross(j.center-a,direction).magnitude;
            if(along>0 && along<route.length && sideways<2) route.stops.Add(new TrafficStop{junction=j,axis=axis,distance=along-(axis==0?14:9)-2.1f,exitDistance=along+(axis==0?10:5)+5});
        }
        return route;
    }
    void Spawn(TrafficRoute route,float distance,int index) {
        var car=new GameObject("Traffic car " + index).AddComponent<TrafficCar>(); car.transform.SetParent(transform);
        car.Initialize(this,route,distance,index); cars.Add(car);
    }
    void PaintRoads(MapData map) {
        foreach(var road in map.roads) for(int i=1;i<road.points.Length;i++) {
            var a=road.points[i-1].V(); var b=road.points[i].V(); var dir=(b-a).normalized; float length=Vector3.Distance(a,b);
            for(float s=.3f;s<length;s+=1f) {
                var p=a+dir*s; bool junctionArea=false;
                foreach(var j in junctions) if(Vector3.Distance(p,j.center)<18) junctionArea=true;
                if(junctionArea) continue;
                float piece=Mathf.Min(.95f,length-s);
                Mark("Centre line",p,dir,.14f,piece,yellow);
                foreach(int side in new[]{-1,1}) {
                    Mark("Road edge",p+Right(dir)*side*(road.width*.5f-.3f),dir,.12f,piece,white);
                    if(road.DisplayLanes>=4 && ((int)s%6)<3) Mark("Lane divider",p+Right(dir)*side*road.width*.25f,dir,.13f,piece,white);
                }
            }
        }
    }
    void Mark(string name,Vector3 p,Vector3 dir,float width,float length,Material material) {
        p.y=.071f;var right=Right(dir)*width*.5f;var along=dir*length*.5f;var list=material==yellow?yellowPaint:whitePaint;list.AddRange(new[]{p-right-along,p-right+along,p+right+along,p+right-along});
    }
    void PaintMesh(List<Vector3> vertices,Material material){var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;var triangles=new List<int>();for(int i=0;i<vertices.Count;i+=4)triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var obj=new GameObject(material.name);obj.transform.SetParent(transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;}
    void AddApproach(TrafficJunction j,Vector3 direction,float halfWidth,float conflictHalfWidth) {
        var right=Right(direction);
        float stop=conflictHalfWidth+4;
        for(float x=-halfWidth+.65f;x<halfWidth-.3f;x+=.95f) Mark("Crosswalk "+j.name,j.center-direction*(conflictHalfWidth+1.8f)+right*x,direction,.5f,2.4f,white);
        Mark("Stop line "+j.name,j.center-direction*stop+right*halfWidth*.5f,direction,halfWidth-.3f,.4f,white);
        // Far-side placement keeps the signal visible from a stopped driver's eye position.
        var root=new GameObject(j.name+" signal approach"); root.transform.SetParent(transform); root.transform.position=j.center+direction*stop+right*(halfWidth+.6f); root.transform.rotation=Quaternion.LookRotation(direction);
        WorldBuilder.Box("Pole",new Vector3(0,2.5f,0),new Vector3(.15f,5,.15f),pole,root.transform,false);
        float reach=halfWidth-2.5f+.6f;
        WorldBuilder.Box("Signal arm",new Vector3(-reach*.5f,4.9f,0),new Vector3(reach,.12f,.12f),pole,root.transform,false);
        WorldBuilder.Box("Signal housing",new Vector3(-reach,4.55f,0),new Vector3(2.5f,.65f,.35f),dark,root.transform,false);
        var lamps=new Renderer[3];
        for(int i=0;i<3;i++) {
            var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name="Signal lamp "+(SignalColor)i; ball.transform.SetParent(root.transform,false);ball.transform.localPosition=new Vector3(-reach+(i==0?-.9f:i==1?-.3f:.9f),4.55f,-.22f);ball.transform.localScale=new Vector3(.42f,.42f,.16f); Destroy(ball.GetComponent<Collider>());lamps[i]=ball.GetComponent<Renderer>();
        }
        j.lamps.Add(lamps);
        var arrow=new List<Renderer>();var origin=new Vector3(-reach+.3f,4.55f,-.24f);
        arrow.Add(WorldBuilder.Box("좌회전 화살표",origin,new Vector3(.36f,.075f,.04f),unlit[2],root.transform,false).GetComponent<Renderer>());
        foreach(int side in new[]{-1,1}){var tip=WorldBuilder.Box("좌회전 화살촉",origin+new Vector3(-.1f,side*.065f,0),new Vector3(.2f,.075f,.04f),unlit[2],root.transform,false);tip.transform.localRotation=Quaternion.Euler(0,0,side*45);arrow.Add(tip.GetComponent<Renderer>());}j.leftLamps.Add(arrow.ToArray());
    }
    void Update() {
        foreach(var j in junctions) for(int approach=0;approach<j.lamps.Count;approach++) {
            var left=LeftSignal(j,approach/2);int color=(int)(left==SignalColor.Yellow?SignalColor.Yellow:Signal(j,approach/2));
            foreach(var arrow in j.leftLamps[approach])arrow.sharedMaterial=left==SignalColor.Green?lit[2]:unlit[2];
            for(int i=0;i<3;i++) j.lamps[approach][i].sharedMaterial=i==color?lit[i]:unlit[i];
        }
    }
}

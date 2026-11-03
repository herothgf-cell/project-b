using System;
using System.Collections.Generic;
using UnityEngine;

// Authored PvP windows use native gap/brake controls. Nothing bypasses collision
// sensing, changes a vehicle pose, or expires a physically occupied ownership.
public sealed class NativeTrafficMergePolicy {
    sealed class Zone {
        public NativeTrafficMergeZone data;public readonly List<Grant> grants=new List<Grant>();public int evaluated=-1;
        public readonly Dictionary<Vehicle,float> requests=new Dictionary<Vehicle,float>();
        public readonly Dictionary<Vehicle,string> reasons=new Dictionary<Vehicle,string>();
    }
    sealed class Grant {public Vehicle owner;public Span span;public bool committed;}
    sealed class Span {
        public Zone zone;public float begin,end;public bool merging;
        public Vector3 exit,direction,entry,entryDirection;public bool clear;
        public Vector3[] corridor;public Bounds bounds;
        public uint id;public string movement;
    }
    sealed class Vehicle {
        public TrafficCar car;public DriveSession driver;public Rigidbody body;public PvpSimulationBot pilot;
        public readonly List<Grant> owned=new List<Grant>();
        public object route;public List<Span> spans;public float at,speed;public Vector3 position;public bool valid;
        public string Name {get{return car?car.name:driver?driver.taxi.name:"destroyed";}}
        public GameObject Root {get{return car?car.gameObject:driver&&driver.taxi?driver.taxi.gameObject:null;}}
        public Rigidbody Body {get{return body;}}
        public bool Alive {get{
            if(car)return car.enabled&&car.Visible&&car.gameObject.activeInHierarchy;
            if(!driver||!driver.taxi||!driver.taxi.gameObject.activeInHierarchy)return false;
            return pilot&&pilot.enabled;
        }}
    }
    readonly TrafficSystem traffic;readonly HdRoadGraph graph;
    readonly List<Zone> zones=new List<Zone>();
    readonly HashSet<string> controlledJunctions=new HashSet<string>();
    readonly Dictionary<int,List<KeyValuePair<Zone,NativeTrafficMergeInterval>>> byEdge=new Dictionary<int,List<KeyValuePair<Zone,NativeTrafficMergeInterval>>>();
    readonly List<Vehicle> fleet=new List<Vehicle>();
    readonly List<Vehicle> eligible=new List<Vehicle>(),stale=new List<Vehicle>();
    readonly TrafficOverlapQuery overlaps=new TrafficOverlapQuery();
    float fleetTime=float.NaN;int fleetCount=-1,revision;uint nextSpan;
    readonly Dictionary<ulong,bool> conflicts=new Dictionary<ulong,bool>();
    public int FleetObservations {get;private set;}
    public int PhysicsQueries {get;private set;}
    public int GapCalls {get;private set;}
    public int PeakConcurrentGrants {get;private set;}
    readonly Dictionary<TrafficCar,Vehicle> cars=new Dictionary<TrafficCar,Vehicle>();
    readonly Dictionary<Rigidbody,Vehicle> bodies=new Dictionary<Rigidbody,Vehicle>();
    readonly Dictionary<DriveSession,Vehicle> drivers=new Dictionary<DriveSession,Vehicle>();
    public int Count {get{return zones.Count;}}
    public bool ControlsJunction(TrafficJunction junction){return !junction.signalized&&!junction.scenarioSignal&&controlledJunctions.Contains(junction.name);}
    public static bool Supports(MapData map){return map!=null&&map.mapId=="pvp-city-a"&&map.pvpTrafficMerges!=null&&map.pvpTrafficMerges.Length>0;}
    public NativeTrafficMergePolicy(TrafficSystem traffic,MapData map,HdRoadGraph graph){
        this.traffic=traffic;this.graph=graph;
        foreach(var data in map.pvpTrafficMerges){
            var zone=new Zone{data=data};zones.Add(zone);
            if(data.id.StartsWith("junction:",StringComparison.Ordinal))controlledJunctions.Add(data.id.Substring(9));
            foreach(var interval in data.intervals){
                List<KeyValuePair<Zone,NativeTrafficMergeInterval>> list;
                if(!byEdge.TryGetValue(interval.edge,out list)){list=new List<KeyValuePair<Zone,NativeTrafficMergeInterval>>();byEdge[interval.edge]=list;}
                list.Add(new KeyValuePair<Zone,NativeTrafficMergeInterval>(zone,interval));
            }
        }
    }
    Vehicle Get(TrafficCar car){Vehicle value;if(!cars.TryGetValue(car,out value)){value=new Vehicle{car=car,body=car.GetComponent<Rigidbody>()};cars[car]=value;if(value.body)bodies[value.body]=value;fleetTime=float.NaN;}return value;}
    Vehicle Get(DriveSession driver){Vehicle value;if(!drivers.TryGetValue(driver,out value)){value=new Vehicle{driver=driver,body=driver.taxi.body,pilot=driver.GetComponent<PvpSimulationBot>()};drivers[driver]=value;if(value.body)bodies[value.body]=value;fleetTime=float.NaN;}return value;}
    public void Register(DriveSession driver){Get(driver);}
    List<Span> Route(object key,HdRoadGraph.RoutePath path,Vector3 start){
        List<Span> result;
        var pieces=new Dictionary<Zone,List<Span>>();float error;float first=graph.paths[path.edges[0]].Project(start,out error);
        for(int k=0;k<path.edges.Count;k++){
            int edge=path.edges[k];List<KeyValuePair<Zone,NativeTrafficMergeInterval>> entries;if(!byEdge.TryGetValue(edge,out entries))continue;
            float origin=k==0?-first:path.ends[k-1],low=k==0?first:0,high=path.ends[k]-origin;
            foreach(var entry in entries){
                var interval=entry.Value;if(interval.end<low-.01f||interval.begin>high+.01f)continue;
                List<Span> spans;if(!pieces.TryGetValue(entry.Key,out spans)){spans=new List<Span>();pieces[entry.Key]=spans;}
                spans.Add(new Span{movement=edge.ToString(),zone=entry.Key,begin=origin+interval.begin,end=origin+interval.end,merging=interval.merging,clear=interval.clear,exit=graph.paths[edge].Position(interval.end),direction=graph.paths[edge].Tangent(interval.end),entry=graph.paths[edge].Position(interval.begin),entryDirection=graph.paths[edge].Tangent(interval.begin)});
            }
        }
        result=new List<Span>();
        foreach(var row in pieces){
            row.Value.Sort((a,b)=>a.begin.CompareTo(b.begin));Span current=null;
            foreach(var piece in row.Value){
                if(current==null||piece.begin>current.end+.25f){
                    current=new Span{movement=piece.movement,zone=row.Key,begin=piece.begin,end=piece.end,merging=piece.merging,clear=piece.clear,exit=piece.clear?piece.exit:row.Key.data.exit,direction=piece.clear?piece.direction:row.Key.data.direction,entry=piece.entry,entryDirection=piece.entryDirection};result.Add(current);
                }else{
                    current.movement+="/"+piece.movement;current.begin=Mathf.Min(current.begin,piece.begin);current.end=Mathf.Max(current.end,piece.end);current.merging|=piece.merging;
                    if(piece.clear){current.clear=true;current.exit=piece.exit;current.direction=piece.direction;}
                }
            }
        }
        result.Sort((a,b)=>a.begin.CompareTo(b.begin));
        foreach(var span in result){
            span.id=++nextSpan;
            float low=Mathf.Max(0,span.begin-3),high=Mathf.Min(path.path.length,span.end+3);
            int count=Mathf.Max(2,Mathf.CeilToInt((high-low)/2)+1);span.corridor=new Vector3[count];
            for(int i=0;i<count;i++){var point=path.path.Position(Mathf.Lerp(low,high,(float)i/(count-1)));span.corridor[i]=point;if(i==0)span.bounds=new Bounds(point,Vector3.zero);else span.bounds.Encapsulate(point);}
            span.bounds.Expand(new Vector3(2.6f,8,2.6f));
        }
        return result;
    }
    void Observe(Vehicle vehicle){
        vehicle.valid=false;if(!vehicle.Alive)return;
        var body=vehicle.Body;vehicle.position=body?body.position:vehicle.Root.transform.position;
        if(vehicle.car){
            var route=vehicle.car.route;if(route==null||route.graphPath==null)return;
            if(vehicle.route!=route){vehicle.spans=Route(route,route.graphPath,route.Position(0));vehicle.route=route;revision++;}vehicle.at=vehicle.car.distance;vehicle.speed=vehicle.car.speed;
        }else{
            var jobs=vehicle.driver.jobs;if(!jobs||!jobs.nav||!jobs.nav.active||jobs.nav.hdRoute==null)return;
            float error;var path=jobs.nav.hdRoute;if(vehicle.route!=path){vehicle.spans=Route(path,path,path.path.Position(0));vehicle.route=path;revision++;}vehicle.at=jobs.nav.ProjectVehicle(out error);vehicle.speed=Mathf.Abs(vehicle.driver.taxi.Speed);
        }
        vehicle.valid=true;
    }
    static float Horizon(Vehicle vehicle){return Mathf.Max(24,vehicle.speed*vehicle.speed/6+10);}
    static Span Upcoming(Vehicle vehicle,Zone zone){
        if(!vehicle.valid)return null;
        foreach(var span in vehicle.spans)if(span.zone==zone&&vehicle.at<=span.end+.05f&&span.begin-vehicle.at<=Horizon(vehicle))return span;
        return null;
    }
    List<Vehicle> Fleet(){
        if(fleetTime==traffic.Clock&&fleetCount==traffic.cars.Count)return fleet;
        foreach(var car in traffic.cars)if(car)Get(car);
        fleet.Clear();fleet.AddRange(cars.Values);fleet.AddRange(drivers.Values);
        foreach(var vehicle in fleet){Observe(vehicle);FleetObservations++;}
        fleetTime=traffic.Clock;fleetCount=traffic.cars.Count;revision++;
        return fleet;
    }
    static bool Owns(Vehicle vehicle,Zone zone){foreach(var grant in zone.grants)if(grant.owner==vehicle)return true;return false;}
    // Occupancy survives route replacement, but permission applies only to the
    // exact route span admitted. A new corridor competes while the old one stays
    // protected until its physical exit; the actor never conflicts with itself.
    static bool Authorized(Vehicle vehicle,Span span){foreach(var grant in span.zone.grants)if(grant.owner==vehicle&&grant.span==span)return true;return false;}
    // A swept centreline includes the body length at its endpoints. Width is
    // conservative for both native NPCs and taxis; adjacent 3.2m lanes stay compatible.
    public static bool CorridorsConflict(Vector3[] a,Vector3[] b,float separation=2.6f){
        for(int i=1;i<a.Length;i++)for(int j=1;j<b.Length;j++)if(SegmentDistanceSquared(a[i-1],a[i],b[j-1],b[j])<separation*separation)return true;
        return false;
    }
    static float Cross(Vector3 a,Vector3 b){return a.x*b.z-a.z*b.x;}
    static float PointSegment(Vector3 p,Vector3 a,Vector3 b){var d=b-a;d.y=0;p.y=a.y;float t=d.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);return (p-a-d*t).sqrMagnitude;}
    static float SegmentDistanceSquared(Vector3 a,Vector3 b,Vector3 c,Vector3 d){
        var u=b-a;var v=d-c;float denominator=Cross(u,v);
        if(Mathf.Abs(denominator)>.00001f){float t=Cross(c-a,v)/denominator,q=Cross(c-a,u)/denominator;if(t>=0&&t<=1&&q>=0&&q<=1)return 0;}
        return Mathf.Min(Mathf.Min(PointSegment(a,c,d),PointSegment(b,c,d)),Mathf.Min(PointSegment(c,a,b),PointSegment(d,a,b)));
    }
    static bool SameStream(Span a,Span b){return !a.merging&&!b.merging&&a.movement==b.movement&&Vector3.SqrMagnitude(a.entry-b.entry)<.04f&&Vector3.SqrMagnitude(a.exit-b.exit)<.04f;}
    static bool FollowingGrant(Vehicle vehicle,Span span,Grant lead){
        return lead.committed&&lead.owner.valid&&lead.owner.speed>.15f&&SameStream(span,lead.span)&&Vector3.Dot(lead.owner.position-vehicle.position,span.entryDirection)>=6;
    }
    static bool MovingStreamLeader(Vehicle vehicle,Span span,Vehicle other){
        foreach(var grant in span.zone.grants)if(grant.owner==other&&FollowingGrant(vehicle,span,grant))return true;
        return false;
    }
    bool Conflicts(Span a,Span b){
        if(a==b)return true;ulong key=((ulong)Math.Min(a.id,b.id)<<32)|Math.Max(a.id,b.id);bool value;if(conflicts.TryGetValue(key,out value))return value;
        value=a.bounds.Intersects(b.bounds)&&CorridorsConflict(a.corridor,b.corridor);if(conflicts.Count>=4096)conflicts.Clear();conflicts[key]=value;return value;
    }
    static Span PhysicalSpan(Vehicle vehicle,Zone zone){
        if(!vehicle.valid)return null;
        foreach(var span in vehicle.spans)if(span.zone==zone&&vehicle.at<=span.end+.05f)return span;
        return null;
    }
    static AssistedRoadNetwork.Path Path(Vehicle vehicle){
        return vehicle.car?vehicle.car.route.graphPath.path:((HdRoadGraph.RoutePath)vehicle.route).path;
    }
    bool Approach(Vehicle vehicle,Span target,List<Vehicle> all,out string reason){
        reason="";
        // Acquiring a later overlapping window requires an actual grant for
        // every earlier window still occupied or ahead on this same journey.
        foreach(var earlier in vehicle.spans){
            if(earlier==target||earlier.begin>=target.begin-.1f||earlier.end<vehicle.at-.05f)continue;
            if(!Authorized(vehicle,earlier)){reason="earlier window:"+earlier.zone.data.id;return false;}
        }
        var path=Path(vehicle);float low=Mathf.Max(0,vehicle.at),high=Mathf.Min(path.length,target.end+1);
        if(high>low+.75f)foreach(var other in all){
            if(other==vehicle||!other.Alive||PhysicalSpan(other,target.zone)==null||MovingStreamLeader(vehicle,target,other))continue;
            // The front vehicle remains a real queue leader even outside its
            // slower, shorter request horizon or with a blocked destination lane.
            if(Vector3.Dot(other.position-vehicle.position,path.Tangent(low))<=.75f)continue;
            float error;float at=path.ProjectRange(other.position,low,high,out error);
            if(at>low+.75f&&error<1.45f){reason="front queue:"+other.Name;return false;}
        }
        float gate=Mathf.Min(path.length,target.begin);
        if(gate>low+.1f){
            // Registered actors are not the only physical obstacles. A manual
            // player's body or static obstruction also prevents a rear grant.
            var bounds=new Bounds(path.Position(low)+Vector3.up*.85f,Vector3.zero);
            for(float at=low;at<=gate+1;at+=1)bounds.Encapsulate(path.Position(Mathf.Min(at,gate))+Vector3.up*.85f);
            bounds.Expand(new Vector3(2.4f,1.4f,2.4f));
            PhysicsQueries++;int count=overlaps.Box(bounds.center,bounds.extents,Quaternion.identity,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var hit=overlaps.hits[i];
                if(hit.transform.IsChildOf(vehicle.Root.transform)||hit.attachedRigidbody==vehicle.Body)continue;
                Vehicle leader;if(hit.attachedRigidbody&&bodies.TryGetValue(hit.attachedRigidbody,out leader)&&MovingStreamLeader(vehicle,target,leader))continue;
                for(float at=low+.5f;at<=gate+.5f;at+=1){
                    var point=path.Position(Mathf.Min(at,gate))+Vector3.up*.85f;
                    if(TrafficColliderGeometry.WithinRadius(hit,point,1.15f)){reason="physical approach:"+hit.name;return false;}
                }
            }
        }
        return true;
    }

    bool Cleared(Grant grant,List<Vehicle> all){
        var owner=grant.owner;if(!owner.Alive)return true;
        var body=owner.Body;var position=body?body.position:owner.Root.transform.position;var span=grant.span;
        if(Vector3.Dot(position-span.exit,span.direction)>.1f||!span.zone.data.bounds.Contains(position))return true;
        if(!grant.committed&&Vector3.Dot(position-span.entry,span.entryDirection)>.05f)grant.committed=true;
        if(!grant.committed){
            Observe(owner);var current=Upcoming(owner,span.zone);if(current==null)return true;
            string reason;if(!Approach(owner,current,all,out reason))return true;
            // A changed unentered intent must compete with its new corridor.
            if(current!=span)return true;
        }
        return false;
    }
    bool ExitClear(Vehicle vehicle,Span span){
        PhysicsQueries++;int count=overlaps.Box(span.exit+Vector3.up*.85f,new Vector3(1.15f,.65f,4.5f),Quaternion.LookRotation(span.direction),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++){var hit=overlaps.hits[i];
            if(hit.transform.IsChildOf(vehicle.Root.transform)||hit.attachedRigidbody==vehicle.Body)continue;
            Vehicle leader;if(hit.attachedRigidbody&&bodies.TryGetValue(hit.attachedRigidbody,out leader)&&MovingStreamLeader(vehicle,span,leader))continue;
            return false;
        }
        return true;
    }
    bool Better(Vehicle candidate,Span span,Vehicle best,Span bestSpan,Zone zone){
        if(best==null)return true;
        bool inside=candidate.at>span.begin+.1f,otherInside=best.at>bestSpan.begin+.1f;
        if(inside!=otherInside)return inside;
        if(inside){
            float progress=Vector3.Dot(candidate.position-best.position,zone.data.direction);
            if(Mathf.Abs(progress)>.5f)return progress>0;
        }
        float first=zone.requests[candidate],second=zone.requests[best];
        if(first!=second)return first<second;
        if(span.merging!=bestSpan.merging)return !span.merging;
        if(Mathf.Abs((span.begin-candidate.at)-(bestSpan.begin-best.at))>.1f)return span.begin-candidate.at<bestSpan.begin-best.at;
        return candidate.Root.GetInstanceID()<best.Root.GetInstanceID();
    }
    void Choose(Zone zone,List<Vehicle> all){
        eligible.Clear();stale.Clear();
        foreach(var vehicle in all){
            var span=Upcoming(vehicle,zone);if(span==null)continue;
            if(!zone.requests.ContainsKey(vehicle))zone.requests[vehicle]=traffic.Clock;
            if(Authorized(vehicle,span))continue;
            string reason;
            if(!Approach(vehicle,span,all,out reason)){zone.reasons[vehicle]=reason;continue;}
            if(!ExitClear(vehicle,span)){zone.reasons[vehicle]="exit occupied";continue;}
            int index=0;while(index<eligible.Count&&!Better(vehicle,span,eligible[index],Upcoming(eligible[index],zone),zone))index++;
            eligible.Insert(index,vehicle);
        }
        foreach(var vehicle in zone.requests.Keys)if(Upcoming(vehicle,zone)==null&&!Owns(vehicle,zone))stale.Add(vehicle);
        foreach(var vehicle in stale){zone.requests.Remove(vehicle);zone.reasons.Remove(vehicle);}
        foreach(var vehicle in eligible){
            var span=Upcoming(vehicle,zone);Grant blocker=null;
            foreach(var grant in zone.grants)if(grant.owner!=vehicle&&Conflicts(span,grant.span)&&!FollowingGrant(vehicle,span,grant)){blocker=grant;break;}
            if(blocker!=null){zone.reasons[vehicle]="conflict:"+blocker.owner.Name;continue;}
            // A stream may follow its moving leader, but new followers cannot
            // indefinitely extend a convoy past an older conflicting request.
            bool queued=false;foreach(var prior in eligible){if(prior==vehicle)break;var priorSpan=Upcoming(prior,zone);if(!Authorized(prior,priorSpan)){if(!SameStream(span,priorSpan)&&Conflicts(span,priorSpan)){queued=true;break;}}}
            if(queued){zone.reasons[vehicle]="older conflicting movement";continue;}
            var admitted=new Grant{owner=vehicle,span=span,committed=Vector3.Dot(vehicle.position-span.entry,span.entryDirection)>.05f};
            zone.grants.Add(admitted);PeakConcurrentGrants=Mathf.Max(PeakConcurrentGrants,zone.grants.Count);vehicle.owned.Add(admitted);zone.reasons[vehicle]="eligible";
        }
    }
    void Release(Grant grant){var zone=grant.span.zone;zone.grants.Remove(grant);grant.owner.owned.Remove(grant);if(!Owns(grant.owner,zone)){zone.requests.Remove(grant.owner);zone.reasons.Remove(grant.owner);}zone.evaluated=-1;}
    string Describe(Vehicle vehicle){
        if(vehicle==null)return "unregistered";var text=new System.Text.StringBuilder();
        foreach(var zone in zones){
            if(Owns(vehicle,zone)){foreach(var grant in vehicle.owned)if(grant.span.zone==zone)text.Append("owns:").Append(zone.data.id).Append(" committed=").Append(grant.committed).Append(" exitAhead=").Append(Vector3.Dot(grant.span.exit-vehicle.position,grant.span.direction).ToString("F1")).Append(";");}
            else {float requested;if(zone.requests.TryGetValue(vehicle,out requested))text.Append("requests:").Append(zone.data.id).Append(" age=").Append((traffic.Clock-requested).ToString("F1")).Append(" owners=").Append(zone.grants.Count).Append(" denied=").Append(zone.reasons.ContainsKey(vehicle)?zone.reasons[vehicle]:"pending").Append(";");}
        }
        return text.Length==0?"none":text.ToString();
    }
    public string Diagnostics(TrafficCar car){Vehicle vehicle;return Describe(cars.TryGetValue(car,out vehicle)?vehicle:null);}
    public string Diagnostics(DriveSession driver){Vehicle vehicle;return Describe(drivers.TryGetValue(driver,out vehicle)?vehicle:null);}
    float Gap(Vehicle vehicle,out string reason){
        GapCalls++;reason="";var all=Fleet();bool valid=vehicle.valid;var oldRoute=vehicle.route;Observe(vehicle);
        if(valid!=vehicle.valid||oldRoute!=vehicle.route)revision++;
        for(int i=vehicle.owned.Count-1;i>=0;i--)if(Cleared(vehicle.owned[i],all))Release(vehicle.owned[i]);
        if(!vehicle.valid)return float.PositiveInfinity;
        float gap=float.PositiveInfinity;
        foreach(var span in vehicle.spans){
            if(vehicle.at>span.end+.05f||span.begin-vehicle.at>Horizon(vehicle))continue;
            var zone=span.zone;
            if(zone.evaluated!=revision){
                for(int i=zone.grants.Count-1;i>=0;i--)if(Cleared(zone.grants[i],all))Release(zone.grants[i]);
                Choose(zone,all);zone.evaluated=revision;
            }
            if(Authorized(vehicle,span)){
                if(vehicle.at<=span.begin+.1f&&!ExitClear(vehicle,span)){float waiting=Mathf.Max(0,span.begin-vehicle.at);if(waiting<gap){gap=waiting;reason="exit blocked:"+zone.data.id;}}
                continue;
            }
            float limit=Mathf.Max(0,span.begin-vehicle.at);
            if(limit<gap){gap=limit;string denial;zone.reasons.TryGetValue(vehicle,out denial);reason="yield:"+zone.data.id+" "+denial;}
        }
        return gap;
    }
    public float Gap(TrafficCar car,out string reason){using(TrafficPerformanceProbe.Measure("Traffic.Merge"))return Gap(Get(car),out reason);}
    public float Gap(DriveSession driver,out string reason){using(TrafficPerformanceProbe.Measure("Traffic.Merge"))return Gap(Get(driver),out reason);}
}

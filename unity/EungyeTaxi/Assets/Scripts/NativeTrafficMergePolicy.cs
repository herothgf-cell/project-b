using System;
using System.Collections.Generic;
using UnityEngine;

// Authored PvP windows use native gap/brake controls. Nothing bypasses collision
// sensing, changes a vehicle pose, or expires a physically occupied ownership.
public sealed class NativeTrafficMergePolicy {
    sealed class Zone {
        public NativeTrafficMergeZone data;public Vehicle owner;public Vector3 entry,entryDirection;public bool committed;
        public readonly Dictionary<Vehicle,float> requests=new Dictionary<Vehicle,float>();
        public readonly Dictionary<Vehicle,string> reasons=new Dictionary<Vehicle,string>();
    }
    sealed class Span {
        public Zone zone;public float begin,end;public bool merging;
        public Vector3 exit,direction,entry,entryDirection;public bool clear;
    }
    sealed class Vehicle {
        public TrafficCar car;public DriveSession driver;
        public readonly List<Zone> owned=new List<Zone>();
        public object route;public List<Span> spans;public float at,speed;public Vector3 position;public bool valid;
        public string Name {get{return car?car.name:driver?driver.taxi.name:"destroyed";}}
        public GameObject Root {get{return car?car.gameObject:driver&&driver.taxi?driver.taxi.gameObject:null;}}
        public Rigidbody Body {get{return car?car.GetComponent<Rigidbody>():driver&&driver.taxi?driver.taxi.body:null;}}
        public bool Alive {get{
            if(car)return car.enabled&&car.Visible&&car.gameObject.activeInHierarchy;
            if(!driver||!driver.taxi||!driver.taxi.gameObject.activeInHierarchy)return false;
            var pilot=driver.GetComponent<PvpSimulationBot>();return pilot&&pilot.enabled;
        }}
    }
    readonly TrafficSystem traffic;readonly HdRoadGraph graph;
    readonly List<Zone> zones=new List<Zone>();
    readonly Dictionary<int,List<KeyValuePair<Zone,NativeTrafficMergeInterval>>> byEdge=new Dictionary<int,List<KeyValuePair<Zone,NativeTrafficMergeInterval>>>();
    readonly Dictionary<object,List<Span>> routes=new Dictionary<object,List<Span>>();
    readonly Dictionary<TrafficCar,Vehicle> cars=new Dictionary<TrafficCar,Vehicle>();
    readonly Dictionary<DriveSession,Vehicle> drivers=new Dictionary<DriveSession,Vehicle>();
    public int Count {get{return zones.Count;}}
    public static bool Supports(MapData map){return map!=null&&map.mapId=="pvp-city-a"&&map.pvpTrafficMerges!=null&&map.pvpTrafficMerges.Length>0;}
    public NativeTrafficMergePolicy(TrafficSystem traffic,MapData map,HdRoadGraph graph){
        this.traffic=traffic;this.graph=graph;
        foreach(var data in map.pvpTrafficMerges){
            var zone=new Zone{data=data};zones.Add(zone);
            foreach(var interval in data.intervals){
                List<KeyValuePair<Zone,NativeTrafficMergeInterval>> list;
                if(!byEdge.TryGetValue(interval.edge,out list)){list=new List<KeyValuePair<Zone,NativeTrafficMergeInterval>>();byEdge[interval.edge]=list;}
                list.Add(new KeyValuePair<Zone,NativeTrafficMergeInterval>(zone,interval));
            }
        }
    }
    Vehicle Get(TrafficCar car){Vehicle value;if(!cars.TryGetValue(car,out value)){value=new Vehicle{car=car};cars[car]=value;}return value;}
    Vehicle Get(DriveSession driver){Vehicle value;if(!drivers.TryGetValue(driver,out value)){value=new Vehicle{driver=driver};drivers[driver]=value;}return value;}
    public void Register(DriveSession driver){Get(driver);}
    List<Span> Route(object key,HdRoadGraph.RoutePath path,Vector3 start){
        List<Span> result;if(routes.TryGetValue(key,out result))return result;
        var pieces=new Dictionary<Zone,List<Span>>();float error;float first=graph.paths[path.edges[0]].Project(start,out error);
        for(int k=0;k<path.edges.Count;k++){
            int edge=path.edges[k];List<KeyValuePair<Zone,NativeTrafficMergeInterval>> entries;if(!byEdge.TryGetValue(edge,out entries))continue;
            float origin=k==0?-first:path.ends[k-1],low=k==0?first:0,high=path.ends[k]-origin;
            foreach(var entry in entries){
                var interval=entry.Value;if(interval.end<low-.01f||interval.begin>high+.01f)continue;
                List<Span> spans;if(!pieces.TryGetValue(entry.Key,out spans)){spans=new List<Span>();pieces[entry.Key]=spans;}
                spans.Add(new Span{zone=entry.Key,begin=origin+interval.begin,end=origin+interval.end,merging=interval.merging,clear=interval.clear,exit=graph.paths[edge].Position(interval.end),direction=graph.paths[edge].Tangent(interval.end),entry=graph.paths[edge].Position(interval.begin),entryDirection=graph.paths[edge].Tangent(interval.begin)});
            }
        }
        result=new List<Span>();
        foreach(var row in pieces){
            row.Value.Sort((a,b)=>a.begin.CompareTo(b.begin));Span current=null;
            foreach(var piece in row.Value){
                if(current==null||piece.begin>current.end+.25f){
                    current=new Span{zone=row.Key,begin=piece.begin,end=piece.end,merging=piece.merging,clear=piece.clear,exit=piece.clear?piece.exit:row.Key.data.exit,direction=piece.clear?piece.direction:row.Key.data.direction,entry=piece.entry,entryDirection=piece.entryDirection};result.Add(current);
                }else{
                    current.begin=Mathf.Min(current.begin,piece.begin);current.end=Mathf.Max(current.end,piece.end);current.merging|=piece.merging;
                    if(piece.clear){current.clear=true;current.exit=piece.exit;current.direction=piece.direction;}
                }
            }
        }
        result.Sort((a,b)=>a.begin.CompareTo(b.begin));routes[key]=result;return result;
    }
    void Observe(Vehicle vehicle){
        vehicle.valid=false;if(!vehicle.Alive)return;
        var body=vehicle.Body;vehicle.position=body?body.position:vehicle.Root.transform.position;
        if(vehicle.car){
            var route=vehicle.car.route;if(route==null||route.graphPath==null)return;
            vehicle.route=route;vehicle.spans=Route(route,route.graphPath,route.Position(0));vehicle.at=vehicle.car.distance;vehicle.speed=vehicle.car.speed;
        }else{
            var jobs=vehicle.driver.jobs;if(!jobs||!jobs.nav||!jobs.nav.active||jobs.nav.hdRoute==null)return;
            float error;var path=jobs.nav.hdRoute;vehicle.route=path;vehicle.spans=Route(path,path,path.path.Position(0));vehicle.at=jobs.nav.ProjectVehicle(out error);vehicle.speed=Mathf.Abs(vehicle.driver.taxi.Speed);
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
        foreach(var car in traffic.cars)if(car)Get(car);
        var all=new List<Vehicle>(cars.Values);all.AddRange(drivers.Values);
        foreach(var vehicle in all)Observe(vehicle);
        return all;
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
            if(earlier.zone.owner!=vehicle){reason="earlier window:"+earlier.zone.data.id;return false;}
        }
        var path=Path(vehicle);float low=Mathf.Max(0,vehicle.at),high=Mathf.Min(path.length,target.end+1);
        if(high>low+.75f)foreach(var other in all){
            if(other==vehicle||!other.Alive||PhysicalSpan(other,target.zone)==null)continue;
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
            foreach(var hit in Physics.OverlapBox(bounds.center,bounds.extents,Quaternion.identity,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore)){
                if(hit.transform.IsChildOf(vehicle.Root.transform)||hit.attachedRigidbody==vehicle.Body)continue;
                for(float at=low+.5f;at<=gate+.5f;at+=1){
                    var point=path.Position(Mathf.Min(at,gate))+Vector3.up*.85f;
                    if((hit.ClosestPoint(point)-point).sqrMagnitude<1.15f*1.15f){reason="physical approach:"+hit.name;return false;}
                }
            }
        }
        return true;
    }

    bool Cleared(Zone zone){
        var owner=zone.owner;if(owner==null)return true;if(!owner.Alive)return true;
        var body=owner.Body;var position=body?body.position:owner.Root.transform.position;
        if(Vector3.Dot(position-zone.data.exit,zone.data.direction)>.1f||!zone.data.bounds.Contains(position))return true;
        if(!zone.committed&&Vector3.Dot(position-zone.entry,zone.entryDirection)>.05f)zone.committed=true;
                if(!zone.committed){
            Observe(owner);var span=Upcoming(owner,zone);if(span==null)return true;
            string reason;if(!Approach(owner,span,Fleet(),out reason))return true;
        }
        return false;
    }
    bool ExitClear(Vehicle vehicle,Span span){
        foreach(var hit in Physics.OverlapBox(span.exit+Vector3.up*.85f,new Vector3(1.15f,.65f,4.5f),Quaternion.LookRotation(span.direction),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore)){
            if(hit.transform.IsChildOf(vehicle.Root.transform)||hit.attachedRigidbody==vehicle.Body)continue;
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
    Vehicle Choose(Zone zone){
        Vehicle best=null;Span bestSpan=null;var eligible=new HashSet<Vehicle>();var all=Fleet();
        foreach(var vehicle in all){
            var span=Upcoming(vehicle,zone);if(span==null)continue;
            eligible.Add(vehicle);if(!zone.requests.ContainsKey(vehicle))zone.requests[vehicle]=traffic.Clock;
            string reason;
            if(!Approach(vehicle,span,all,out reason)){zone.reasons[vehicle]=reason;continue;}
            if(!ExitClear(vehicle,span)){zone.reasons[vehicle]="exit occupied";continue;}
            zone.reasons[vehicle]="eligible";
            if(Better(vehicle,span,best,bestSpan,zone)){best=vehicle;bestSpan=span;}
        }
        foreach(var stale in new List<Vehicle>(zone.requests.Keys))if(!eligible.Contains(stale)){zone.requests.Remove(stale);zone.reasons.Remove(stale);}
        return best;
    }

    void Release(Zone zone){if(zone.owner!=null){zone.owner.owned.Remove(zone);zone.requests.Remove(zone.owner);zone.reasons.Remove(zone.owner);}zone.owner=null;zone.committed=false;}
    string Describe(Vehicle vehicle){
        if(vehicle==null)return "unregistered";var text=new System.Text.StringBuilder();
        foreach(var zone in zones){
            if(zone.owner==vehicle){var body=vehicle.Body;var position=body?body.position:vehicle.position;text.Append("owns:").Append(zone.data.id).Append(" committed=").Append(zone.committed).Append(" exitAhead=").Append(Vector3.Dot(zone.data.exit-position,zone.data.direction).ToString("F1")).Append(";");}
            else {float requested;if(zone.requests.TryGetValue(vehicle,out requested))text.Append("requests:").Append(zone.data.id).Append(" age=").Append((traffic.Clock-requested).ToString("F1")).Append(" owner=").Append(zone.owner==null?"none":zone.owner.Name).Append(" denied=").Append(zone.reasons.ContainsKey(vehicle)?zone.reasons[vehicle]:"pending").Append(";");}
        }
        return text.Length==0?"none":text.ToString();
    }
    public string Diagnostics(TrafficCar car){Vehicle vehicle;return Describe(cars.TryGetValue(car,out vehicle)?vehicle:null);}
    public string Diagnostics(DriveSession driver){Vehicle vehicle;return Describe(drivers.TryGetValue(driver,out vehicle)?vehicle:null);}
    float Gap(Vehicle vehicle,out string reason){
        reason="";for(int i=vehicle.owned.Count-1;i>=0;i--)if(Cleared(vehicle.owned[i]))Release(vehicle.owned[i]);
        Observe(vehicle);if(!vehicle.valid)return float.PositiveInfinity;
        float gap=float.PositiveInfinity;
        foreach(var span in vehicle.spans){
            if(vehicle.at>span.end+.05f||span.begin-vehicle.at>Horizon(vehicle))continue;
            var zone=span.zone;
            if(zone.owner!=null&&Cleared(zone))Release(zone);
            if(zone.owner==null){
                zone.owner=Choose(zone);
                if(zone.owner!=null){
                    var admission=Upcoming(zone.owner,zone);zone.entry=admission.entry;zone.entryDirection=admission.entryDirection;
                    zone.committed=Vector3.Dot(zone.owner.position-zone.entry,zone.entryDirection)>.05f;zone.owner.owned.Add(zone);
                }
            }
            if(zone.owner==vehicle){
                // If an exit becomes blocked before entry, wait upstream.
                // A committed owner keeps protection and the normal body checks.
                if(vehicle.at<=span.begin+.1f&&!ExitClear(vehicle,span)){float waiting=Mathf.Max(0,span.begin-vehicle.at);if(waiting<gap){gap=waiting;reason="exit blocked:"+zone.data.id;}}
                continue;
            }
            float limit=Mathf.Max(0,span.begin-vehicle.at);
            if(limit<gap){gap=limit;reason="yield:"+zone.data.id+" owner="+(zone.owner==null?"exit blocked":zone.owner.Name);}
        }
        return gap;
    }
    public float Gap(TrafficCar car,out string reason){return Gap(Get(car),out reason);}
    public float Gap(DriveSession driver,out string reason){return Gap(Get(driver),out reason);}
}

using UnityEngine;
public class JobOffer {public int pickup,drop,fare,requiredScore;public float seconds,distance;}
public class PassengerJobs : MonoBehaviour {
 public DriveSession session;public readonly FareTrip trip=new FareTrip(); public JobOffer[] offers=new JobOffer[3];public JobOffer current;
 public int balance,completed,failed;public string message="1·2·3으로 호출 선택 / 길가 손님 옆에 정차 후 F";
 public RoadNavigation nav;public MapStop[] stops;public GameObject[] people;public float[] cooldown;GameObject marker;float resultUntil;int generation;
 public bool Active {get{return trip.State==TripState.ToPickup||trip.State==TripState.OnTrip;}}
 public void Initialize(DriveSession owner){session=owner;stops=session.world.map.stops;nav=gameObject.AddComponent<RoadNavigation>();nav.Build(session.world.map,session.taxi.transform);cooldown=new float[stops.Length];people=new GameObject[stops.Length];
  var coat=WorldBuilder.Mat("Passenger coat",new Color(.12f,.6f,.72f));var skin=WorldBuilder.Mat("Passenger head",new Color(.86f,.65f,.44f));
  for(int i=0;i<stops.Length;i++){var root=new GameObject(KoreanText.Stop(stops[i].name)+" passenger");root.transform.SetParent(transform);root.transform.position=stops[i].Passenger;people[i]=root;
   var arrow=new GameObject("승하차 진행 방향");arrow.transform.SetParent(transform);arrow.transform.position=stops[i].Kerb+Vector3.up*.14f;arrow.transform.rotation=Quaternion.LookRotation(stops[i].Direction);
   WorldBuilder.Box("정차 방향",Vector3.zero,new Vector3(.16f,.025f,3),coat,arrow.transform,false);
   foreach(int side in new[]{-1,1}){var tip=WorldBuilder.Box("화살표",new Vector3(side*.38f,0,1.1f),new Vector3(.15f,.025f,1.1f),coat,arrow.transform,false);tip.transform.localRotation=Quaternion.Euler(0,-side*45,0);}

   WorldBuilder.Box("Passenger body",new Vector3(0,.85f,0),new Vector3(.48f,1.15f,.3f),coat,root.transform,false);
   var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(root.transform,false);head.transform.localPosition=Vector3.up*1.58f;head.transform.localScale=Vector3.one*.35f;head.GetComponent<Renderer>().sharedMaterial=skin;Destroy(head.GetComponent<Collider>());
   var label=new GameObject("Pickup label");label.transform.SetParent(root.transform,false);label.transform.localPosition=Vector3.up*2.3f;var text=label.AddComponent<TextMesh>();text.text=KoreanText.Stop(stops[i].name)+"\nF · 택시 탑승";text.fontSize=40;text.characterSize=.08f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.cyan;KoreanText.Apply(text);label.AddComponent<FaceCamera>();
  }
  marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Pickup or destination zone";marker.transform.localScale=new Vector3(3,.03f,9);marker.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("Destination zone",new Color(.2f,.8f,.75f));Destroy(marker.GetComponent<Collider>());marker.SetActive(false);Generate();
 }
 JobOffer Make(int pickup,int drop){float distance=nav.Distance(stops[pickup],stops[drop]);return new JobOffer{pickup=pickup,drop=drop,requiredScore=new[]{60,70,85}[(pickup+drop)%3],distance=distance,fare=3000+Mathf.CeilToInt(distance*4/100)*100,seconds=Mathf.Max(75,Mathf.Ceil(distance/6+50))};}
 public void Generate(){if(stops.Length<2)return;for(int i=0;i<3;i++){int p=(generation+i*2)%stops.Length;int d=(p+3+i)%stops.Length;if(p==d)d=(d+1)%stops.Length;offers[i]=Make(p,d);}generation++;}
 public bool Accept(int index){if(index<0||index>=offers.Length||trip.State!=TripState.Idle||Time.timeScale==0||!session.business.CanAccept)return false;current=offers[index];if(float.IsInfinity(current.distance))return false;if(!trip.Accept(current.fare,current.seconds))return false;message="호출 수락 · 픽업 장소: "+KoreanText.Stop(stops[current.pickup].name);SetGoal(current.pickup);return true;}
 void SetGoal(int index){nav.SetDestination(stops[index]);marker.transform.rotation=Quaternion.LookRotation(stops[index].Direction);marker.SetActive(true);marker.transform.position=stops[index].Kerb+Vector3.up*.085f;}
 public bool At(int index){return stops[index].CanStop(session.taxi.transform.position,session.taxi.transform.forward,session.taxi.body.velocity.magnitude);}
 public bool Interact(){if(Time.timeScale==0)return false;
  if(trip.State==TripState.Idle){if(!session.business.CanAccept)return false;int closest=-1;float best=8;for(int i=0;i<stops.Length;i++)if(Time.time>=cooldown[i]&&At(i)){float d=Vector3.Distance(session.taxi.transform.position,stops[i].Kerb);if(d<best){best=d;closest=i;}}
   if(closest<0){message="손님이 있는 오른쪽 연석에 같은 방향으로 정차하세요 (시속 2km 미만)";return false;}
   current=Make(closest,(closest+3)%stops.Length);if(float.IsInfinity(current.distance))return false;trip.Accept(current.fare,current.seconds);return Board();
  }
  if(trip.State==TripState.ToPickup){if(!session.business.CanAccept)return false;if(!At(current.pickup)){message="승차 지점 오른쪽 연석에 화살표 방향으로 정차 후 F";return false;}return Board();}
  if(trip.State==TripState.OnTrip){if(!At(current.drop)){message="목적지 오른쪽 연석에 화살표 방향으로 정차 후 F";return false;}if(session.requests)session.requests.Finish();int pay=MannerLedger.Payment(trip.Complete(),session.manners.ledger.Score,current.requiredScore);if(pay<=0)return false;balance+=pay;completed++;session.business.RecordFare(pay);session.business.RecordPassenger(pay,"운송 완료");message=(session.manners.ledger.Score>=current.requiredScore?"요구 점수 달성! ":"요구 점수 미달 · 요금 70% 지급 ")+"+"+pay.ToString("N0")+"원";Finish();return true;}
  return false;
 }
 bool Board(){if(!trip.Board())return false;if(session.law)session.law.tripId=session.law.ids.Next("trip");session.manners.BeginRide();if(session.requests)session.requests.Begin();cooldown[current.pickup]=Time.time+60;people[current.pickup].SetActive(false);message="승객 탑승 · 목적지: "+KoreanText.Stop(stops[current.drop].name);SetGoal(current.drop);return true;}
 public void ClearForDay(){if(session.law)session.law.tripId="";trip.Clear();current=null;resultUntil=0;nav.Clear();marker.SetActive(false);for(int i=0;i<cooldown.Length;i++)cooldown[i]=0;Generate();message="1·2·3으로 호출 선택 / 길가 손님 옆에 정차 후 F";}
 void Finish(){nav.Clear();marker.SetActive(false);resultUntil=Time.time+4;}
 public void RecordExpiredTrip(){if(trip.State==TripState.Failed&&resultUntil==0){failed++;session.business.RecordPassenger(0,"시간 초과");session.business.RecordFailure();message="제한 시간 초과 · 요금이 지급되지 않았습니다";Finish();}}
 void Update(){trip.Tick(Time.deltaTime);RecordExpiredTrip();
  if(resultUntil>0&&Time.time>=resultUntil){trip.Clear();resultUntil=0;Generate();}
  if(Time.timeScale>0){if(Input.GetKeyDown(KeyCode.Alpha1))Accept(0);if(Input.GetKeyDown(KeyCode.Alpha2))Accept(1);if(Input.GetKeyDown(KeyCode.Alpha3))Accept(2);if(Input.GetKeyDown(KeyCode.F)||Input.GetKeyDown(KeyCode.JoystickButton8))Interact();}
  for(int i=0;i<people.Length;i++)people[i].SetActive((Time.time>=cooldown[i]||trip.State==TripState.ToPickup&&current.pickup==i)&&!(trip.State==TripState.OnTrip&&current.pickup==i));
 }
}

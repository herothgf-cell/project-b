using UnityEngine;

// The inactive vehicle occupies its own marked bay inside the garage.
public sealed class HomeGarageParking : MonoBehaviour {
 public GameObject ParkedTaxi {get;private set;}
 Material pavement,paint;
 string displayedModel="";
 int displayedColor=-1;
 bool displayedOrnament;

 public static void Sync(DriveSession session){
  if(!session||!session.world||!session.business)return;
  var b=session.business;
  var a=b.ledger?.Account;
  var display=session.GetComponent<HomeGarageParking>();
  bool available=a!=null&&a.FatherCarGarage&&!b.SimulationSession&&!RoadMapRuntime.Trial&&!PracticeSession.Enabled
   &&HomeGarage.Available(session.world.map)&&a.Vehicle.modelId!=VehicleCatalog.Starter;
  if(!available){if(display&&display.ParkedTaxi)display.ParkedTaxi.SetActive(false);return;}
  if(!display)display=session.gameObject.AddComponent<HomeGarageParking>();
  var inactive=a.ActiveVehicle.modelId==VehicleCatalog.Starter?a.Vehicle:a.OwnedVehicle(VehicleCatalog.Starter);
  display.Show(session.world.map,inactive);
 }

 void Show(MapData map,VehicleState state){
  if(!ParkedTaxi){
   var home=HomeGarage.StoredBay(map);
   var stall=new GameObject("숙소 보관 주차 칸");
   stall.transform.SetParent(transform,false);
   stall.transform.SetPositionAndRotation(home.Kerb,Quaternion.LookRotation(home.Direction));
   pavement=WorldBuilder.Mat("숙소 보관 주차 바닥",WorldBuilder.SurfaceColor("parking"));
   paint=WorldBuilder.Mat("숙소 보관 주차선",new Color(.95f,.8f,.18f));
   TaxiArt.Part(stall.transform,"보관 주차 바닥",new Vector3(0,.025f,0),new Vector3(2.9f,.04f,5.3f),.01f,pavement);
   for(int side=-1;side<=1;side+=2)
    TaxiArt.Part(stall.transform,"보관 주차선",new Vector3(side*1.34f,.054f,0),new Vector3(.11f,.017f,5.15f),.005f,paint);
   ParkedTaxi=new GameObject("숙소 보관 택시");
   ParkedTaxi.transform.SetParent(stall.transform,false);
   ParkedTaxi.transform.localPosition=Vector3.up*.39f;
   TaxiArt.Exterior(ParkedTaxi.transform);
   for(int i=0;i<4;i++){
    var wheel=TaxiArt.Wheel(ParkedTaxi.transform);
    wheel.localPosition=new Vector3(i%2==0?-.85f:.85f,0,i<2?1.3f:-1.3f);
    wheel.localRotation=Quaternion.Euler(0,0,90);
   }
   foreach(var collider in ParkedTaxi.GetComponentsInChildren<Collider>(true))collider.enabled=false;
  }
  if(!ParkedTaxi.activeSelf)ParkedTaxi.SetActive(true);
  bool ornament=state.ornamentOwned&&state.ornamentPlaced;
  if(state.modelId==displayedModel&&state.color==displayedColor&&ornament==displayedOrnament)return;
  displayedModel=state.modelId;displayedColor=state.color;displayedOrnament=ornament;
  var appearance=VehicleAppearance.For(ParkedTaxi.transform);
  appearance.Set(new PvpVehicleLook{model=state.modelId,color=state.color,ornament=ornament});
  // VehicleAppearance builds a layer-8 plant for the player's exterior. This copy belongs to the world.
  foreach(var part in ParkedTaxi.GetComponentsInChildren<Transform>(true))part.gameObject.layer=0;
 }

 void OnDestroy(){
  if(pavement)Destroy(pavement);
  if(paint)Destroy(paint);
 }
}

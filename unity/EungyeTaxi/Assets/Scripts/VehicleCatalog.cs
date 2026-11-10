using System;

// Vehicle models differ only in five operating traits (fuel, trunk, ride comfort, wear, turning).
// Fare, top speed and acceleration are deliberately identical. The starter values equal the
// pre-vehicle behaviour, and PVP always uses the starter (standard) traits.
public sealed class VehicleSpec {
 public string id,name,summary,priceKey;public int defaultPrice;
 public double fuelFactor,comfortFactor,wearFactor,steerFactor;public bool carriesLuggage;
 public string[] colorNames;public float[][] colors;public float scaleX,scaleY,scaleZ;
}
[Serializable] public class VehicleState {
 public string modelId="starter";public int color;public bool altColorOwned,ornamentOwned,ornamentPlaced;
 public int purchasedDay,paidVehicle,paidColor,paidOrnament,paidTradedColor;public string tradedColorModel="";
 public VehicleState Copy(){return (VehicleState)MemberwiseClone();}
}
public sealed class VehicleQuote {public string model;public int price,tradeIn,spend,cashAfter;public bool allowed;public string reason="";}
public static class VehicleCatalog {
 public const string Starter="starter",Hybrid="hybrid",WideLpg="lpg";
 public const string ColorPriceKey="vehicle.color.price",OrnamentPriceKey="vehicle.ornament.price",TradeInKey="vehicle.tradeIn.base";
 public static readonly VehicleSpec[] All={
  new VehicleSpec{id=Starter,name="중고 택시",summary="지금 타는 차 · 무난하지만 잘 상함",priceKey="",defaultPrice=0,fuelFactor=1,comfortFactor=1,wearFactor=1,steerFactor=1,carriesLuggage=true,colorNames=new[]{"해바라기 노랑","귤색"},colors=new[]{new[]{1f,.72f,.025f},new[]{.98f,.48f,.12f}},scaleX=1,scaleY=1,scaleZ=1},
  new VehicleSpec{id=Hybrid,name="하이브리드 세단",summary="연료비가 적게 듦 · 트렁크가 작아 큰 짐 손님을 못 태움",priceKey="vehicle.hybrid.price",defaultPrice=26000,fuelFactor=.65,comfortFactor=1,wearFactor=.75,steerFactor=1,carriesLuggage=false,colorNames=new[]{"하늘 흰색","민트"},colors=new[]{new[]{.92f,.95f,.97f},new[]{.45f,.82f,.72f}},scaleX=.97f,scaleY=.93f,scaleZ=1.03f},
  new VehicleSpec{id=WideLpg,name="넓은 LPG 택시",summary="트렁크가 크고 승차감이 좋음 · 회전 반경이 큼",priceKey="vehicle.lpg.price",defaultPrice=22000,fuelFactor=.95,comfortFactor=1.08,wearFactor=.8,steerFactor=.86,carriesLuggage=true,colorNames=new[]{"주황","남색"},colors=new[]{new[]{1f,.55f,.12f},new[]{.16f,.25f,.46f}},scaleX=1.06f,scaleY=1.07f,scaleZ=1.04f}
 };
 public static VehicleSpec Get(string id){foreach(var v in All)if(v.id==id)return v;return null;}
 public static VehicleSpec Standard {get{return All[0];}}
 public static VehicleSpec Of(VehicleState state){var v=state==null?null:Get(state.modelId);return v??Standard;}
 public static bool Purchasable(string id){return id==Hybrid||id==WideLpg;}
 public static int Price(string id){var v=Get(id);return v==null||!Purchasable(id)?0:ContentTuning.Int(v.priceKey,v.defaultPrice);}
 public static int ColorPrice {get{return ContentTuning.Int(ColorPriceKey,1500);}}
 public static int OrnamentPrice {get{return ContentTuning.Int(OrnamentPriceKey,2000);}}
 // Durability 100 keeps the full base; durability 0 still returns half (scrap/parts value).
 public static int TradeIn(int durability){int d=Math.Max(0,Math.Min(100,durability));int b=ContentTuning.Int(TradeInKey,10000);return (int)Math.Floor(b*(50+d/2.0)/100/10)*10;}
 public static VehicleQuote Quote(VehicleState owned,string id,int cash,int debt,int durability){
  var q=new VehicleQuote{model=id,price=Price(id),tradeIn=TradeIn(durability)};q.spend=Math.Max(0,q.price-q.tradeIn);q.cashAfter=cash-q.spend;
  if(!Purchasable(id))q.reason="구매할 수 없는 차량";
  else if(owned!=null&&owned.modelId!=Starter)q.reason="이번 회차의 차량 교체는 1회입니다";
  else if(debt!=0)q.reason="미납 비용을 먼저 정리해야 합니다";
  else if(cash<q.spend)q.reason="현금이 "+(q.spend-cash).ToString("N0")+"원 모자랍니다";
  q.allowed=q.reason.Length==0;return q;
 }
 // Trunk and ride traits never apply to PVP (standard vehicle).
 public static bool CarriesLuggage(VehicleState s){return Of(s).carriesLuggage;}
 // Ride comfort widens the harsh-motion band; 1 keeps DriveMath's original thresholds.
 public static bool HarshMotion(double comfort,float acceleration,float speedMs,bool necessaryBraking){double c=comfort>0?comfort:1;float a=(float)(acceleration/c);return Math.Abs(speedMs)>2&&(a>4.5f||a< -6f&&!necessaryBraking);}
 // Sturdier bodies register a gentler impact tier for the same closing speed.
 public static float WearSpeed(VehicleSpec spec,float closingSpeed){return closingSpeed*(float)(spec==null?1:spec.wearFactor);}
}

// Account-level PVP appearance unlocks, kept apart from the 3-day campaign save. Union only:
// a new campaign or a failed campaign save never removes an unlock.
[Serializable] public class VehicleUnlocks {
 public int version=1;public string[] models=new string[0],colors=new string[0];public bool ornament;
 public bool HasModel(string id){return id==VehicleCatalog.Starter||Array.IndexOf(models??new string[0],id)>=0;}
 public bool HasColor(string id,int color){return color==0?HasModel(id):color==1&&Array.IndexOf(colors??new string[0],id+":1")>=0;}
 static string[] Add(string[] list,string value){list=list??new string[0];if(Array.IndexOf(list,value)>=0)return list;var next=new string[list.Length+1];list.CopyTo(next,0);next[list.Length]=value;Array.Sort(next,StringComparer.Ordinal);return next;}
 // Returns true when anything new was unlocked.
 public bool Record(VehicleState s){
  if(s==null)return false;int before=Count;
  if(VehicleCatalog.Purchasable(s.modelId))models=Add(models,s.modelId);
  if(s.altColorOwned&&VehicleCatalog.Get(s.modelId)!=null)colors=Add(colors,s.modelId+":1");
  if(!string.IsNullOrEmpty(s.tradedColorModel)&&VehicleCatalog.Get(s.tradedColorModel)!=null)colors=Add(colors,s.tradedColorModel+":1");
  if(s.ornamentOwned)ornament=true;return Count!=before;
 }
 int Count {get{return (models==null?0:models.Length)+(colors==null?0:colors.Length)+(ornament?1:0);}}
 public void Validate(){
  if(version!=1||models==null||colors==null)throw new ArgumentException("Invalid vehicle unlocks");
  foreach(var m in models)if(!VehicleCatalog.Purchasable(m))throw new ArgumentException("Invalid unlocked model");
  foreach(var c in colors){int at=c==null?-1:c.LastIndexOf(':');if(at<=0||c.Substring(at)!=":1"||VehicleCatalog.Get(c.Substring(0,at))==null)throw new ArgumentException("Invalid unlocked colour");}
  if(new System.Collections.Generic.HashSet<string>(models).Count!=models.Length||new System.Collections.Generic.HashSet<string>(colors).Count!=colors.Length)throw new ArgumentException("Duplicate unlock");
 }
}
// Wire/appearance only: PVP physics and economy always use VehicleCatalog.Standard.
[Serializable] public class PvpVehicleLook {
 public string model=VehicleCatalog.Starter;public int color;public bool ornament;
 public static PvpVehicleLook Default {get{return new PvpVehicleLook();}}
 public bool Valid {get{return VehicleCatalog.Get(model)!=null&&(color==0||color==1);}}
 public PvpVehicleLook Copy(){return new PvpVehicleLook{model=model,color=color,ornament=ornament};}
 // Selections not owned locally fall back to the default taxi (never an error). The dashboard
 // ornament has no PVP toggle: it shows whenever it was bought in single player.
 public static PvpVehicleLook Owned(PvpVehicleLook wanted,VehicleUnlocks unlocks){
  if(wanted==null||!wanted.Valid||unlocks==null||!unlocks.HasModel(wanted.model))return Default;
  return new PvpVehicleLook{model=wanted.model,color=unlocks.HasColor(wanted.model,wanted.color)?wanted.color:0,ornament=unlocks.ornament};
 }
 // Host-side sanitising of a guest's look: unknown values become the default taxi.
 public static PvpVehicleLook Sanitize(PvpVehicleLook look){return look!=null&&look.Valid?look.Copy():Default;}
 // Bots: deterministic per room session and slot, from the fixed catalog.
 public static PvpVehicleLook Bot(string session,int slot){uint h=2166136261;foreach(char c in (session??"")+":bot-look:"+slot){h^=c;h*=16777619;}var spec=VehicleCatalog.All[(int)(h%(uint)VehicleCatalog.All.Length)];return new PvpVehicleLook{model=spec.id,color=(int)(h/7%2),ornament=h/13%2==0};}
}

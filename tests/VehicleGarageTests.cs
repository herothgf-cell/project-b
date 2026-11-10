using System;
// Plan 22: vehicle traits, one replacement per 3-day campaign, decoration, migration, PVP unlocks/looks.
class VehicleGarageTests {
 static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static void Reject(Action a,string message){try{a();}catch(ArgumentException){count++;return;}throw new Exception(message);}
 static EconomyLedger SettledDay(int fare){var e=new EconomyLedger(PrototypeSettings.ForThreeDays());e.RecordFare("trip-"+fare,fare);e.RequestClose("test");e.Settle();return e;}
 static int Main(){try{Run();return 0;}catch(Exception x){Console.WriteLine("FAIL: "+x.GetType().Name+": "+x.Message);return 1;}}
 static void Run(){
  // Starter regression: every trait factor is neutral and fuel matches the pre-vehicle formula.
  var s=VehicleCatalog.Standard;Check(s.id=="starter"&&s.fuelFactor==1&&s.comfortFactor==1&&s.wearFactor==1&&s.steerFactor==1&&s.carriesLuggage,"starter traits neutral");
  var settings=PrototypeSettings.ForThreeDays();var a=new CampaignAccount(settings);a.Consume(60,2000);
  float expected=settings.initialFuel-(float)(2000/1000.0*settings.fuelPerKm+60/60.0*settings.idlePerMinute);Check(Math.Abs(a.Fuel-expected)<1e-5,"starter fuel unchanged "+a.Fuel+" vs "+expected);
  foreach(var spec in VehicleCatalog.All){Check(spec.colorNames.Length==2&&spec.colors.Length==2,"two colours "+spec.id);}
  Check(!VehicleCatalog.Get("hybrid").carriesLuggage&&VehicleCatalog.Get("lpg").carriesLuggage&&VehicleCatalog.Get("hybrid").fuelFactor<1&&VehicleCatalog.Get("lpg").comfortFactor>1&&VehicleCatalog.Get("lpg").steerFactor<1,"trade-off traits");
  // Comfort: neutral factor reproduces DriveMath exactly; wide LPG tolerates a slightly harder stop.
  foreach(float acc in new[]{-7f,-6.2f,-5.5f,4f,4.6f,5f})Check(VehicleCatalog.HarshMotion(1,acc,8,false)==DriveMath.HarshPassengerMotion(acc,8,false),"neutral comfort "+acc);
  Check(VehicleCatalog.HarshMotion(1,-6.3f,8,false)&&!VehicleCatalog.HarshMotion(1.08,-6.3f,8,false)&&VehicleCatalog.HarshMotion(1.08,-7f,8,false),"comfort widens band");
  Check(VehicleCatalog.WearSpeed(s,9)==9&&VehicleCatalog.WearSpeed(VehicleCatalog.Get("lpg"),10)<10,"wear factor");
  var care=new VehicleDamage();int starterLoss=care.Impact(VehicleCatalog.WearSpeed(s,11f));var sturdy=new VehicleDamage();int lpgLoss=sturdy.Impact(VehicleCatalog.WearSpeed(VehicleCatalog.Get("lpg"),11f));Check(starterLoss==30&&lpgLoss<starterLoss,"sturdier car loses less "+starterLoss+"/"+lpgLoss);
  // Purchase rules on a settled ledger.
  var e=SettledDay(30000);var q=e.QuoteVehicle("hybrid");Check(q.allowed&&q.price==26000&&q.tradeIn==10000&&q.spend==16000&&q.cashAfter==e.Account.Cash-16000,"quote "+q.spend+" "+q.reason);
  int net=e.Account.Net;int cash=e.Account.Cash;e.Care.Damage.Impact(9.5);int durability=e.Care.Damage.Durability;Check(durability==85&&e.QuoteVehicle("lpg").tradeIn==9250,"trade-in follows durability "+e.QuoteVehicle("lpg").tradeIn);
  Check(e.PurchaseVehicle("lpg"),"buy lpg after settlement");int spent=22000-9250;Check(e.Account.Cash==cash-spent&&e.Account.Net==net&&e.Account.PersonalSpent==spent,"personal expense separate from operating net");
  Check(e.Care.Damage.Durability==100&&e.Care.Cleanliness==100,"new car repaired and clean");
  Check(!e.PurchaseVehicle("hybrid")&&!e.PurchaseVehicle("lpg")&&e.Account.Cash==cash-spent,"duplicate click / second replacement rejected");
  Check(e.Account.VehicleSpec.id=="lpg","owned lpg");
  // Operating day cannot buy; insufficient cash cannot buy and does not mutate.
  var open=new EconomyLedger(PrototypeSettings.ForThreeDays());open.RecordFare("x",40000);Check(!open.PurchaseVehicle("lpg")&&open.Account.Vehicle.modelId=="starter","no purchase while operating");
  var poor=SettledDay(1000);Check(!poor.QuoteVehicle("hybrid").allowed&&!poor.PurchaseVehicle("hybrid")&&poor.Account.Vehicle.modelId=="starter"&&poor.Account.PersonalSpent==0,"insufficient cash no mutation");
  var debt=SettledDay(30000);debt.Account.Repair("r",1,100);Check(debt.Account.Debt==0||!debt.PurchaseVehicle("lpg"),"debt blocks");
  // Restart restore: snapshot round trip keeps ownership, payments and no double charge.
  var restored=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());Check(restored.Account.Vehicle.modelId=="lpg"&&restored.Account.Cash==e.Account.Cash&&restored.Care.Damage.Durability==100,"restore keeps vehicle");
  Check(!restored.PurchaseVehicle("hybrid")&&!restored.PurchaseVehicle("lpg"),"restored cannot re-buy");
  Check(restored.NextDay()&&restored.Account.VehicleSpec.id=="lpg","next day drives new car");
  // Decoration: colour + ornament on the same personal ledger; ornament follows a replacement.
  var d=SettledDay(30000);int c0=d.Account.Cash;Check(d.BuyVehicleColor()&&d.Account.Vehicle.color==1&&d.Account.Cash==c0-1500,"starter colour");Check(!d.BuyVehicleColor(),"colour once");
  Check(d.BuyVehicleOrnament()&&d.Account.Vehicle.ornamentPlaced&&d.Account.Cash==c0-3500&&!d.BuyVehicleOrnament(),"ornament once");
  Check(d.Account.ToggleOrnament()&&!d.Account.Vehicle.ornamentPlaced&&d.Account.SelectVehicleColor(0)&&!d.Account.SelectVehicleColor(2),"toggle and select");
  Check(d.PurchaseVehicle("hybrid")&&d.Account.Vehicle.ornamentOwned&&!d.Account.Vehicle.altColorOwned&&d.Account.Vehicle.tradedColorModel=="starter","replacement keeps ornament, records traded colour");
  var d2=EconomyLedger.RestoreCampaign(d.Snapshot(),d.Account.Snapshot());Check(d2.Account.Vehicle.modelId=="hybrid"&&d2.Account.PersonalSpent==d.Account.PersonalSpent,"traded colour restores");
  Check(d2.BuyVehicleColor()&&EconomyLedger.RestoreCampaign(d2.Snapshot(),d2.Account.Snapshot()).Account.Vehicle.altColorOwned,"new car colour restores");
  // Fuel factor applies to an owned hybrid only.
  var h=SettledDay(30000);Check(h.PurchaseVehicle("hybrid")&&h.NextDay(),"hybrid next day");float f0=h.Account.Fuel;h.Advance(60,2000);float used=f0-h.Account.Fuel;Check(Math.Abs(used-(2000/1000.0*settings.fuelPerKm+60/60.0*settings.idlePerMinute)*.65)<1e-4,"hybrid fuel factor "+used);
  // Tampered saves reject; old saves migrate to the starter taxi.
  var bad=e.Account.Snapshot();bad.Vehicle.modelId="starter";Reject(()=>CampaignAccount.Restore(bad),"paid vehicle without ownership rejected");
  var bad2=new CampaignAccount(settings).Snapshot();bad2.Vehicle.modelId="hybrid";Reject(()=>CampaignAccount.Restore(bad2),"unpaid vehicle rejected");
  var bad3=e.Account.Snapshot();bad3.Vehicle.paidVehicle=1;Reject(()=>CampaignAccount.Restore(bad3),"altered price rejected");
  var bad4=new CampaignAccount(settings).Snapshot();bad4.Vehicle.altColorOwned=true;Reject(()=>CampaignAccount.Restore(bad4),"unpaid colour rejected");
  var legacy=new CampaignAccount(settings).Snapshot();legacy.Vehicle=null;Check(CampaignAccount.Restore(legacy).Vehicle.modelId=="starter","legacy save migrates to starter");
  var blank=new CampaignAccount(settings).Snapshot();blank.Vehicle=new VehicleState{modelId=null,tradedColorModel=null};Check(CampaignAccount.Restore(blank).Vehicle.modelId=="starter","materialized blank migrates");
  // New campaign: a fresh ledger always starts with the starter taxi.
  Check(new EconomyLedger(PrototypeSettings.ForThreeDays()).Account.Vehicle.modelId=="starter","new campaign resets vehicle");
  // Finalized room record blocks further trades.
  var fin=SettledDay(30000);fin.Account.Room.finalized=true;Check(!fin.PurchaseVehicle("lpg")&&!fin.BuyVehicleColor(),"finalized blocks");
  // PVP unlocks: union only, survive a campaign reset, validated.
  var u=new VehicleUnlocks();Check(u.HasModel("starter")&&!u.HasModel("lpg")&&!u.HasColor("starter",1),"empty unlocks");
  Check(u.Record(d.Account.Vehicle)&&u.HasModel("hybrid")&&u.HasColor("starter",1)&&u.ornament,"record purchases");Check(!u.Record(d.Account.Vehicle),"idempotent");
  Check(!u.Record(new EconomyLedger(PrototypeSettings.ForThreeDays()).Account.Vehicle)&&u.HasModel("hybrid"),"new campaign keeps unlocks");
  u.Validate();var tampered=new VehicleUnlocks{models=new[]{"tank"}};Reject(()=>tampered.Validate(),"unknown unlock rejected");
  var want=new PvpVehicleLook{model="lpg",color=1,ornament=true};var got=PvpVehicleLook.Owned(want,u);Check(got.model=="starter"&&got.color==0,"not-owned look falls back to default taxi");
  var mine=PvpVehicleLook.Owned(new PvpVehicleLook{model="hybrid",color=1,ornament=true},u);Check(mine.model=="hybrid"&&mine.color==0&&mine.ornament,"owned model, unowned colour falls back");
  Check(PvpVehicleLook.Sanitize(new PvpVehicleLook{model="x",color=9}).model=="starter"&&PvpVehicleLook.Sanitize(null).Valid,"host sanitizes guest look");
  var b1=PvpVehicleLook.Bot("room",1);Check(b1.Valid&&PvpVehicleLook.Bot("room",1).model==b1.model&&PvpVehicleLook.Bot("room",1).color==b1.color,"bot look deterministic");
  bool varied=false;for(int i=0;i<20;i++)if(PvpVehicleLook.Bot("room"+i,2).model!=b1.model)varied=true;Check(varied,"bot looks vary");
  Console.WriteLine("PASS vehicle garage "+count);
 }
}

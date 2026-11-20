using System;
// Plan 22: vehicle traits, one replacement per 3-day campaign, decoration, migration, PVP unlocks/looks.
class VehicleGarageTests {
 static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static void Reject(Action a,string message){try{a();}catch(ArgumentException){count++;return;}throw new Exception(message);}
 static EconomyLedger SettledDay(int fare){var e=new EconomyLedger(PrototypeSettings.ForThreeDays());e.RecordFare("trip-"+fare,fare);e.RequestClose("test");e.Settle();return e;}
 static int Main(){try{Run();return 0;}catch(Exception x){Console.WriteLine("FAIL: "+x.GetType().Name+": "+x.Message);return 1;}}
 static void FatherGarage(){
  var eungyeSettings=PrototypeSettings.ForThreeDays();eungyeSettings.homeBase=true;
  var eungye=new EconomyLedger(eungyeSettings);eungye.RecordFare("home-test",33000);
  Check(!eungye.SelectOwnedVehicle("starter")&&!eungye.PurchaseVehicle("lpg"),"cannot change owned car selection or purchase during a shift");
  eungye.RequestClose();eungye.Settle();
  Check(eungye.QuoteVehicle("lpg").tradeIn==0&&eungye.QuoteVehicle("lpg").spend==22000,"Eungye pays full price and retains the father taxi");
  Check(eungye.BuyVehicleColor(),"father car colour bought");
  eungye.Care.Damage.Impact(9.5);eungye.Care.Soil("home-soil",22);
  int cash=eungye.Account.Cash,net=eungye.Account.Net;
  Check(eungye.PurchaseVehicle("lpg")&&eungye.Account.Cash==cash-22000&&eungye.Account.Net==net,"full price posts as a personal expense");
  Check(eungye.Account.OwnedVehicle("starter").color==1&&eungye.Account.ActiveVehicle.modelId=="starter"&&eungye.Account.PendingVehicleModel=="lpg","both cars owned after purchase, father still operating today");
  Check(eungye.Care.Damage.Durability==85&&eungye.Care.Cleanliness==78,"buying a second car does not repair the father taxi");
  Check(!eungye.SelectOwnedVehicle("hybrid")&&eungye.SelectOwnedVehicle("starter")&&eungye.Account.PendingVehicleModel.Length==0&&eungye.SelectOwnedVehicle("lpg"),"rescheduling only selects owned models");
  var restored=EconomyLedger.RestoreCampaign(eungye.Snapshot(),eungye.Account.Snapshot());
  Check(restored.Account.OwnedVehicle("starter").color==1&&restored.Account.ActiveVehicle.modelId=="starter"&&restored.Account.PendingVehicleModel=="lpg","restore both cars and next-day selection");
  Check(restored.NextDay()&&restored.Account.ActiveVehicle.modelId=="lpg"&&restored.Account.PendingVehicleModel.Length==0&&restored.Care.Damage.Durability==100&&restored.Care.Cleanliness==100,"new day activates the clean replacement");
  Check(!restored.SelectOwnedVehicle("starter")&&!restored.PurchaseVehicle("hybrid"),"operating day cannot change cars");
  restored.Care.Damage.Impact(8);restored.RequestClose();restored.Settle();
  int afterDay=restored.Account.Cash;
  Check(restored.SelectOwnedVehicle("starter")&&restored.Account.ActiveVehicle.modelId=="lpg"&&restored.Account.Vehicle.modelId=="lpg"&&restored.Account.Cash==afterDay,"return to father taxi is free and preserves the replacement");
  var returnToFather=EconomyLedger.RestoreCampaign(restored.Snapshot(),restored.Account.Snapshot());
  Check(returnToFather.NextDay()&&returnToFather.Account.ActiveVehicle.modelId=="starter"&&returnToFather.Care.Damage.Durability==85&&returnToFather.Care.Cleanliness==78,"father condition survives storage, save and reactivation");
  returnToFather.RequestClose();returnToFather.Settle();
  Check(returnToFather.SelectOwnedVehicle("lpg")&&returnToFather.ContinueCareer()&&returnToFather.Day==4&&returnToFather.Account.ActiveVehicle.modelId=="lpg"&&returnToFather.Care.Damage.Durability==85,"continued career activates pending replacement with its own condition");
  var invalid=returnToFather.Account.Snapshot();invalid.activeVehicleModel="hybrid";
  Reject(()=>CampaignAccount.Restore(invalid),"unowned active vehicle rejected");
  invalid=returnToFather.Account.Snapshot();invalid.ownedVehicles[0].paidColor++;
  Reject(()=>CampaignAccount.Restore(invalid),"tampered father paint payment rejected");
  invalid=returnToFather.Account.Snapshot();invalid.pendingVehicleModel="starter";
  var days=returnToFather.Snapshot();Reject(()=>EconomyLedger.RestoreCampaign(days,invalid),"active checkpoint cannot have an unapplied selection");
  var beforeP4=SettledDay(33000);
  Check(beforeP4.BuyVehicleColor()&&beforeP4.PurchaseVehicle("lpg"),"legacy trade-in fixture");
  var legacy=beforeP4.Account.Snapshot();legacy.version=4;legacy.settings.homeBase=true;
  var migrated=EconomyLedger.RestoreCampaign(beforeP4.Snapshot(),legacy);
  Check(migrated.Account.Cash==beforeP4.Account.Cash&&migrated.Account.Vehicle.paidVehicle==12000&&migrated.Account.ActiveVehicle.modelId=="lpg","migration keeps historical charges and current car");
  Check(migrated.Account.OwnedVehicle("starter").altColorOwned&&migrated.Account.OwnedVehicle("starter").paidColor==1500&&migrated.Account.Snapshot().ownedVehicles.Length==2,"legacy traded paint rights return to the retained starter");
  Check(EconomyLedger.RestoreCampaign(migrated.Snapshot(),migrated.Account.Snapshot()).Account.PersonalSpent==13500,"normalizing old garage saves never rebills ownership");
 }
 static void WorkshopParts(){
  var settings=PrototypeSettings.ForThreeDays();settings.homeBase=true;
  var ledger=new EconomyLedger(settings);var account=ledger.Account;
  int start=account.Cash;int net=account.Net;
  Check(!account.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Equip,1),"unowned parts cannot be fitted");
  account.Fare("parts-budget",50000);
  Check(account.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Buy,1)
   &&!account.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Buy,1)
   &&account.Cash==start+50000-6000&&account.Net==net+50000,"purchase once from personal funds");
  var filter=WorkshopPart.Get("eco-filter");
  Check(account.PartVehicle(filter)==null,"bought filter is initially in storage");
  float before=account.Fuel;account.Consume(60,1000);float stock=before-account.Fuel;
  Check(account.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Equip,1),"fit purchased filter");
  before=account.Fuel;account.Consume(60,1000);
  Check(Math.Abs(stock-(before-account.Fuel)-settings.fuelPerKm*.1)<.00002,"filter saves only distance fuel");
  Check(account.ChangeWorkshopPart("comfort-damper",WorkshopPartAction.Buy,1)
   &&account.ChangeWorkshopPart("comfort-damper",WorkshopPartAction.Equip,1)
   &&!VehicleCatalog.HarshMotion(account.RideComfort,4.8f,8,false),"damper changes the in-game comfort threshold");
  Check(!account.ChangeWorkshopPart("comfort-damper",WorkshopPartAction.Equip,1),"fitting twice is idempotent");
  ledger.RequestClose();ledger.Settle();Check(ledger.PurchaseVehicle("lpg")&&ledger.NextDay(),"switch vehicles with parts fitted");
  Check(account.PartVehicle(filter).modelId=="starter"&&Math.Abs(account.RideComfort-1.08)<.00001,"inactive car keeps its own fitted equipment");
  int cash=account.Cash;
  Check(account.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Equip,2)&&account.PartVehicle(filter).modelId=="lpg"&&account.Cash==cash,"transfer existing filter without another payment");
  var restored=EconomyLedger.RestoreCampaign(ledger.Snapshot(),account.Snapshot()).Account;
  Check(restored.OwnsPart(filter)&&restored.PartVehicle(filter).modelId=="lpg"
   &&restored.PartVehicle(WorkshopPart.Get("comfort-damper")).modelId=="starter","equipment and inventory survive restore");
  Check(restored.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Remove,2)&&restored.OwnsPart(filter)
   &&restored.PartVehicle(filter)==null&&!restored.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Remove,2),"storing releases the fitting without selling the part");
  var corrupt=account.Snapshot();corrupt.ownedVehicles[0].equippedParts|=filter.bit;
  Reject(()=>CampaignAccount.Restore(corrupt),"cannot duplicate a fitting across the garage");
  corrupt=account.Snapshot();corrupt.ownedVehicles[0].equippedParts|=WorkshopPart.Get("cream-stripe").bit;
  Reject(()=>CampaignAccount.Restore(corrupt),"cannot load unpaid fitted parts");
  var poor=PrototypeSettings.ForThreeDays();poor.homeBase=true;poor.startCash=0;
  var empty=new CampaignAccount(poor);
  Check(!empty.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Buy,1)&&empty.PersonalSpent==0,"insufficient cash leaves storage unchanged");
 }
 static void Run(){
  WorkshopParts();
  FatherGarage();
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

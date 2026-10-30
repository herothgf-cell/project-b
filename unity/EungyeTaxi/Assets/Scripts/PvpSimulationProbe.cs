using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Actual physics acceptance probe. Both competitors receive only pedal and
// steering-assistance inputs: no transport placements and no fabricated fares.
public sealed class PvpSimulationProbe : MonoBehaviour {
    readonly StringBuilder report=new StringBuilder();bool ok=true;string outputName;
    sealed class FixedArrival {
        readonly string offer,pickup,drop;
        public FixedArrival(DriveSession session){offer=session.jobs.OfferId;pickup=session.jobs.current.pickupStopId;drop=session.jobs.current.dropStopId;}
        public PassengerResult Read(DriveSession session){
            PassengerResult result=null;int count=0;
            foreach(var item in session.business.passengers)if(item.offerId==offer&&item.pickupStopId==pickup&&item.dropStopId==drop&&!string.IsNullOrEmpty(item.tripId)&&(item.outcome=="운송 완료"||item.outcome=="시간 초과 후 목적지 도착")){result=item;count++;}
            // Native records are keyed by boarded trip ID. Neither cancellation
            // nor a later offer to the same place can satisfy this fixed route.
            return count==1?result:null;
        }
    }
    void ReportNativePassengers(DriveSession session){
        var seen=new System.Collections.Generic.HashSet<string>();
        foreach(var item in session.business.passengers)if(seen.Add(item.tripId)){
            string detail="driver="+session.SimulationDriver+" trip="+item.tripId+" offer="+item.offerId+" pickup="+item.pickupStopId+" drop="+item.dropStopId+" outcome="+item.outcome+" paid="+item.paid+" quoted="+item.quoted+" elapsed="+item.elapsed;
            report.AppendLine("NATIVE_PASSENGER "+detail);Debug.Log("PVP_SIMULATION_NATIVE_PASSENGER "+detail);
        }
    }
    void Check(bool value,string detail){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+detail);Debug.Log("PVP_SIMULATION "+(value?"PASS ":"FAIL ")+detail);}
    static string StopGeometry(DriveSession s){if(s.jobs.current==null)return "none";int goal=s.jobs.trip.State==TripState.ToPickup?s.jobs.current.pickup:s.jobs.current.drop;var stop=s.jobs.stops[goal];return "completed="+s.jobs.completed+" goal="+goal+" direction="+stop.Direction+" routeEnd="+(s.jobs.nav.hdRoute==null?Vector3.zero:s.jobs.nav.hdRoute.path.Position(s.jobs.nav.hdRoute.path.length))+" finalHeading="+(s.jobs.nav.hdRoute==null?0:Vector3.Dot(s.jobs.nav.hdRoute.path.Tangent(s.jobs.nav.hdRoute.path.length),stop.Direction))+" curb="+stop.Kerb+" delta="+Vector3.Distance(s.taxi.transform.position,stop.Kerb).ToString("0.0")+" heading="+Vector3.Dot(s.taxi.transform.forward,stop.Direction).ToString("0.00");}
    bool NativeContract(DriveSession session,int pickup,int drop){
        session.jobs.ClearForDay();
        var factory=typeof(PassengerJobs).GetMethod("Make",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var offer=(JobOffer)factory.Invoke(session.jobs,new object[]{pickup,drop,null});
        if(offer==null)return false;
        session.jobs.offers=new[]{offer,null,null};
        Debug.Log("PVP_SIMULATION_FIXED_CONTRACT driver="+session.SimulationDriver+" pickup="+pickup+" drop="+drop+" seconds="+offer.seconds+" fare="+offer.fare+" distance="+offer.distance+" noPoseTimingOrRewardEdits=true");
        return session.jobs.Accept(0);
    }
    IEnumerator Start() {
        outputName=PvpSimulationMode.Expanded?"pvp-simulation-expanded":Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpSimulationEungyeRegression")>=0?"pvp-simulation-eungye-regression":Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpSimulationPaidFixture")>=0?"pvp-simulation-eungye-paid-fixture":"pvp-simulation-eungye";
        float deadline=Time.realtimeSinceStartup+180;PvpSimulationMatch match=null;
        while(!match&&Time.realtimeSinceStartup<deadline){match=FindObjectOfType<PvpSimulationMatch>();yield return null;}
        if(!match||!match.Bot){Check(false,"match initialization timeout");Complete();yield break;}
        var human=match.Human;var bot=match.Bot;
        var signalFixture=new TrafficJunction{signalized=true};var observationFixture=new RoadObservation{known=true,trafficClear=true,pedestrian=PedestrianState.None,forward=SignalColor.Red,right=SignalColor.Red};
        Check(!PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,0,true,SignalColor.Red,true),"native signal fixture straight red stops even after full stop");
        Check(!PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,1,false,SignalColor.Red,true)&&PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,1,true,SignalColor.Red,true),"native signal fixture legacy right on red requires full stop and clear crossing");
        observationFixture.pedestrian=PedestrianState.Crossing;Check(!PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,1,true,SignalColor.Red,true),"native signal fixture right turn never bypasses pedestrian");observationFixture.pedestrian=PedestrianState.None;
        observationFixture.dedicated=true;Check(!PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,1,true,SignalColor.Red,true),"native signal fixture dedicated right red stops after full stop");
        signalFixture.editorProgram=new RoadMapSignalProgram();Check(PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,1,true,SignalColor.Green,true)&&!PvpSimulationBot.NativeMovementPermitted(signalFixture,observationFixture,1,true,SignalColor.Red,true),"native signal fixture editor uses actual movement signal instead of straight indication");
        bool congestionRegression=!PvpSimulationMode.Expanded&&Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpSimulationEungyeRegression")>=0;
        bool paidFixture=!PvpSimulationMode.Expanded&&Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpSimulationPaidFixture")>=0;
        if(paidFixture&&congestionRegression){Check(false,"select only one fixed fixture");Complete();yield break;}
        bool fixedContracts=paidFixture||congestionRegression;
        FixedArrival fixedHuman=null,fixedBot=null;
        if(fixedContracts){
            var initialPilot=bot.GetComponent<PvpSimulationBot>();initialPilot.enabled=false;bot.taxi.autoThrottle=0;bot.taxi.autoBrake=true;
            // Both initial generated claims must be released before assigning
            // either fixed contract; otherwise a random first bot offer can
            // legitimately reserve the fixture human's pickup.
            human.jobs.ClearForDay();bot.jobs.ClearForDay();
            bool humanContract=NativeContract(human,9,paidFixture?15:7),botContract=NativeContract(bot,paidFixture?8:21,paidFixture?2:7);
            Check(humanContract&&botContract,(paidFixture?"fixed paid native fixture human9>15 bot8>2":"fixed congestion native fixture human9>7 bot21>7")+"; no position/timing/reward modification");
            if(!humanContract||!botContract){Complete();yield break;}
            fixedHuman=new FixedArrival(human);fixedBot=new FixedArrival(bot);
            if(congestionRegression){report.AppendLine("Congestion regression only: exact native arrival permits honest late/zero fare; ordinary Eungye/city paid-delivery acceptance remains separate.");
            Debug.Log("PVP_SIMULATION_CONGESTION_REGRESSION exact native arrivals, including late/zero fare; NOT the paid-delivery acceptance gate");}else report.AppendLine("Fixed paid native integration fixture: both exact selected contracts must pay positive fares; ordinary generated-call balance is separate.");
            initialPilot.enabled=true;
        }
        Check(PvpSimulationBot.ThrottleFor(0,.18f)>0&&PvpSimulationBot.ThrottleFor(0,7)>0&&PvpSimulationBot.ThrottleFor(0,0)==0&&PvpSimulationBot.ThrottleFor(2,1)==0,"low-speed pedal fixture starts positive crawl without accelerating into a stop or overspeed");
        var approach=PvpSimulationBot.CurbPathPoints(Vector3.zero,Vector3.forward,new Vector3(3.5f,0,28),Vector3.forward);
        bool forwardOnly=true;for(int i=1;i<approach.Length;i++)forwardOnly&=approach[i].z>=approach[i-1].z&&approach[i].x>=-.01f&&approach[i].x<=3.51f;
        Check(approach[0]==Vector3.zero&&approach[approach.Length-1]==new Vector3(3.5f,0,28)&&Vector3.Dot((approach[approach.Length-1]-approach[approach.Length-2]).normalized,Vector3.forward)>.999f&&forwardOnly,"curb geometry fixture preserves endpoints final heading and forward lane-to-kerb path");
        yield return null;
        var journal=human.GetComponent<DrivingScoreJournal>();Check(journal&&journal.Loaded&&!journal.enabled,"campaign score journal disabled before file access");
        Check(human.world==bot.world&&human.traffic==bot.traffic,"one shared world and traffic system");
        Check(human.jobs!=bot.jobs&&human.business.ledger!=bot.business.ledger&&human.law.ledger!=bot.law.ledger,"independent passenger/economy/law state");
        Check(human.taxi.wheels.Length==4&&bot.taxi.wheels.Length==4&&human.damage&&bot.damage,"original physical taxis and durability components");
        Check(human.GetComponent<DriveHUD>()&&human.drivingCamera&&FindObjectsOfType<AudioListener>().Length==1,"original HUD and one driving camera listener");
        Check(human.business.SavePath==""&&bot.business.SavePath==""&&human.business.SaveCheckpoint()&&bot.business.SaveCheckpoint(),"in-memory sessions do not resolve campaign save paths");
        // General passenger is a documented test selection, not a gameplay override.
        human.jobs.ForcedPassengerType=PassengerType.General;
        bot.jobs.ForcedPassengerType=PassengerType.General;
        human.taxi.automated=true;human.assisted.allowAutomated=true;
        var pilot=human.gameObject.AddComponent<PvpSimulationBot>();pilot.Initialize(human);
        var botPilot=bot.GetComponent<PvpSimulationBot>();
        float armDeadline=Time.realtimeSinceStartup+15;
        while(!bot.jobs.Active&&Time.realtimeSinceStartup<armDeadline)yield return null;
        Check(bot.jobs.Active&&bot.assisted.SetActive(true),"initial bot call route arms on actual directed spawn lane");
        Time.timeScale=3;deadline=Time.realtimeSinceStartup+420;float nextTelemetry=0,nextNpcTelemetry=20;
        while(!match.Finished&&Time.realtimeSinceStartup<deadline) {
            if(match.Elapsed>=nextNpcTelemetry){nextNpcTelemetry=match.Elapsed+20;foreach(var car in human.traffic.cars)if(car&&car.Visible&&car.speed<.1f)Debug.Log("PVP_SIMULATION_NPC_STOP time="+match.Elapsed+" "+PvpSimulationBot.TrafficCarDiagnostics(car));}
            var receipt=human.GetComponent<RideCompletionPanel>();if(receipt&&receipt.Open){receipt.Close();Time.timeScale=3;}
            if(fixedContracts?fixedHuman.Read(human)!=null&&fixedBot.Read(bot)!=null:human.jobs.completed>0&&bot.jobs.completed>0)break;
            if(match.Elapsed>=nextTelemetry){nextTelemetry=match.Elapsed+10;Debug.Log("PVP_SIMULATION_DRIVE time="+match.Elapsed+" human="+pilot.Status+" pos="+human.taxi.transform.position+" state="+human.jobs.trip.State+" remain="+human.jobs.nav.remaining+" metres="+pilot.MetresDriven+" bot="+botPilot.Status+" pos="+bot.taxi.transform.position+" state="+bot.jobs.trip.State+" remain="+bot.jobs.nav.remaining+" metres="+botPilot.MetresDriven+" humanOffers="+pilot.SelectionDiagnostics+" botOffers="+botPilot.SelectionDiagnostics+" humanStop="+StopGeometry(human)+" botStop="+StopGeometry(bot)+" humanMotion="+pilot.MotionDiagnostics+" botMotion="+botPilot.MotionDiagnostics);}
            yield return new WaitForSecondsRealtime(.25f);
        }
        Check(pilot.MetresDriven>30&&botPilot.MetresDriven>30,"physical distance human="+pilot.MetresDriven+" bot="+botPilot.MetresDriven);
        bool transportSatisfied=fixedContracts?fixedHuman.Read(human)!=null&&fixedBot.Read(bot)!=null&&(!paidFixture||(fixedHuman.Read(human).paid>0&&fixedBot.Read(bot).paid>0)):human.jobs.completed>0&&bot.jobs.completed>0;
        ReportNativePassengers(human);ReportNativePassengers(bot);
        if(congestionRegression)Check(transportSatisfied,"fixed congestion regression: exact human9>7 and bot21>7 native arrivals recorded once per boarded trip; late/zero permitted, paid counts human="+human.jobs.completed+" bot="+bot.jobs.completed);
        else Check(transportSatisfied,"actual boarding and paid delivery human="+human.jobs.completed+" bot="+bot.jobs.completed+" humanStatus="+pilot.Status+" botStatus="+botPilot.Status);
        match.ObserveServiceTime(human);match.ObserveServiceTime(bot);
        Check(match.HumanScore==human.jobs.balance-human.business.ledger.Account.Expense(1,null)&&match.BotScore==bot.jobs.balance-bot.business.ledger.Account.Expense(1,null),"completed original fares minus actual service expenses score");
        if(!congestionRegression)Check(human.jobs.balance>0&&bot.jobs.balance>0,"both earn actual delivered fare before service expenses");
        bot.signals.BeginPassengerStop();bot.signals.ObserveMovement(2);Check(!bot.signals.Hazards,"bot passenger-stop hazards clear through passive movement observation");bot.signals.Clear();
        bool botLampsVisible=true;foreach(var child in bot.signals.GetComponentsInChildren<Transform>())if(child.name=="Taxi indicator")botLampsVisible&=child.gameObject.layer==bot.taxi.gameObject.layer;
        Check(botLampsVisible,"bot indicator lamps inherit visible competitor layer");
        if(transportSatisfied)yield return PhysicalImpactFixture(match,pilot,botPilot);
        var fatigueFixture=new EconomyLedger(PrototypeSettings.ForOneDay());fatigueFixture.Driver.fatigue=100;fatigueFixture.Driver.drowsiness.visionSeconds=180;
        Check(fatigueFixture.UseEmergencySleepInMatch("simulation-emergency-fixture")&&fatigueFixture.State==BusinessState.Open&&!fatigueFixture.Driver.drowsiness.Blackout&&fatigueFixture.Driver.fatigue==80&&fatigueFixture.Elapsed==100,"fatigue model fixture recovers with native 100-second cost without closing match");
        if(!match.Finished){
            human.jobs.ClearForDay();
            foreach(var terminal in new[]{TripState.Complete,TripState.Failed}){human.jobs.trip.Restore(new FareTripSnapshot{state=terminal});int generation=human.jobs.OfferGeneration;bool refreshed=pilot.RefreshTerminalCalls();Check(refreshed&&human.jobs.trip.State==TripState.Idle&&human.jobs.OfferGeneration==generation+1&&!pilot.RefreshTerminalCalls()&&human.jobs.OfferGeneration==generation+1,"terminal fixture refreshes native calls exactly once: "+terminal);}
            bot.jobs.Generate();Check(bot.jobs.OfferPhase==(bot.business.ledger.Elapsed<300?0:bot.business.ledger.Elapsed<900?1:2),"one-day offer phase initialized without scenario zones");
            var testOffer=human.jobs.offers[0];if(testOffer!=null){float prior=human.jobs.cooldown[testOffer.pickup];human.jobs.cooldown[testOffer.pickup]=Time.time+60;Check(human.jobs.AcceptanceRejection(0)!="","cooldown fixture rejects a stale pickup offer");human.jobs.cooldown[testOffer.pickup]=prior;}
            int claimed=-1;for(int i=0;i<human.jobs.stops.Length;i++)if(match.Ledger.Available(i,0)&&match.Ledger.Available(i,1)){claimed=i;break;}
            if(claimed>=0){human.jobs.ClearForDay();human.jobs.current=new JobOffer{pickup=claimed};match.Ledger.TryClaim(claimed,0);human.jobs.ClearForDay();Check(match.Ledger.TryClaim(claimed,1),"reset fixture releases pickup ownership");match.Ledger.Release(claimed,1);}
            // Explicit accounting fixture, separate from the physical transport above.
            float raceTime=match.Elapsed,botWait=match.ServiceRemaining(bot);
            human.business.ledger.AdvanceService(10);human.business.SaveCheckpoint();
            Check(match.ServiceRemaining(human)>=9.9f&&match.ServiceRemaining(bot)==botWait&&match.Elapsed==raceTime,"service-time fixture locks only payer without jumping shared clock");
            Check(!human.business.CanAccept&&!human.jobs.Interact(),"service lock prevents new transport");
        }
        int beforeHuman=match.HumanScore,beforeBot=match.BotScore;match.Finish();
        Check(!match.Ledger.Settle("after-finish",0,9999)&&!human.jobs.Interact()&&!bot.jobs.Interact(),"finish blocks new settlement and transport");
        yield return null;
        Check(match.HumanScore==beforeHuman&&match.BotScore==beforeBot,"result remains frozen");
        report.AppendLine("map="+human.world.map.mapId+" elapsed="+match.Elapsed+" humanDamage="+human.business.ledger.Care.Damage.Durability+" botDamage="+bot.business.ledger.Care.Damage.Durability);
        bool expanded=PvpSimulationMode.Expanded;
        int duration=PvpSimulationMode.DurationSeconds;PvpSimulationMode.Reset();
        Check(human.business.SimulationSession&&bot.business.SimulationSession&&human.business.SaveCheckpoint()&&bot.business.SaveCheckpoint(),"old sessions retain no-save isolation after global mode reset");
        PvpSimulationMode.Configure(expanded,duration);
        DontDestroyOnLoad(gameObject);match.ReturnToMenu();
        yield return null;yield return null;
        var bootstrap=FindObjectOfType<Pvp.PvpBootstrap>();
        Check(!PvpSimulationMode.Active&&FindObjectsOfType<DriveSession>().Length==0&&bootstrap&&bootstrap.GetComponent<Pvp.PvpPrototypePanel>().enabled,"return menu clears mode and both simulation sessions");
        if(bootstrap){
            bootstrap.StartSimulation(!expanded,30);deadline=Time.realtimeSinceStartup+180;
            PvpSimulationMatch replay=null;
            while(!replay&&Time.realtimeSinceStartup<deadline){replay=FindObjectOfType<PvpSimulationMatch>();yield return null;}
            if(replay&&replay.Bot){Check(replay.Human.world.map.mapId==(expanded?"eungye":"pvp-city-a"),"menu replay switches map using native map pipeline");Check(replay.HumanScore==0&&replay.BotScore==0&&replay.Human.jobs.completed==0&&replay.Bot.jobs.completed==0&&replay.Human.business.ledger.Account.Fuel>59&&replay.Bot.business.ledger.Account.Fuel>59&&replay.Human.business.ledger.Care.Damage.Durability==100&&replay.Bot.business.ledger.Care.Damage.Durability==100,"new match resets scores fuel damage and transport");replay.Finish();}
            else Check(false,"replay initialization timeout");
        }
        Complete();
    }
    IEnumerator PhysicalImpactFixture(PvpSimulationMatch match,PvpSimulationBot humanPilot,PvpSimulationBot botPilot){
        // Separate collision fixture after real transport assertions. Only walls
        // are placed; taxis retain their actual poses and receive ordinary inputs.
        humanPilot.enabled=botPilot.enabled=false;
        var drivers=new[]{match.Human,match.Bot};var walls=new GameObject[2];
        var witnesses=new PvpSimulationImpactWitness[2];var initial=new int[2];var metres=new float[2];
        for(int i=0;i<drivers.Length;i++){
            var s=drivers[i];s.jobs.ClearForDay();s.assisted.SetActive(false);initial[i]=s.business.ledger.Care.Damage.Durability;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="PVP_IMPACT_FIXTURE_"+i;
            wall.transform.position=s.taxi.transform.position+s.taxi.transform.forward*12+Vector3.up*1.5f;
            wall.transform.rotation=Quaternion.LookRotation(s.taxi.transform.forward);wall.transform.localScale=new Vector3(12,4,2);walls[i]=wall;
            witnesses[i]=s.taxi.gameObject.AddComponent<PvpSimulationImpactWitness>();witnesses[i].Wall=wall.GetComponent<Collider>();
            s.taxi.automated=true;s.taxi.autoSteer=0;s.taxi.autoServiceBrake=0;s.taxi.autoBrake=false;s.taxi.autoThrottle=.7f;
        }
        Physics.SyncTransforms();Time.timeScale=3;float deadline=Time.realtimeSinceStartup+20;
        while(Time.realtimeSinceStartup<deadline&&(!witnesses[0].Hit||!witnesses[1].Hit)){
            for(int i=0;i<drivers.Length;i++){metres[i]+=drivers[i].taxi.body.velocity.magnitude*Time.deltaTime;if(witnesses[i].Hit){drivers[i].taxi.autoThrottle=0;drivers[i].taxi.autoBrake=true;}}
            yield return null;
        }
        for(int i=0;i<drivers.Length;i++){
            var s=drivers[i];s.taxi.autoThrottle=0;s.taxi.autoBrake=true;
            Check(witnesses[i].Hit&&metres[i]>5&&s.business.ledger.Care.Damage.Durability<initial[i],"physical impact fixture driver="+i+" obstacleContact="+witnesses[i].Hit+" collider="+witnesses[i].Contact+" metres="+metres[i]+" durability="+initial[i]+">"+s.business.ledger.Care.Damage.Durability+"; no taxi pose/velocity edits");
            Destroy(walls[i]);Destroy(witnesses[i]);
        }
        yield return null;
    }
    void Complete(){
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));
        var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="-pvpSimulationOutput")folder=Path.GetFullPath(args[i+1]);
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,outputName+".txt"),"success="+ok+"\n"+report);
        Debug.Log("PVP_SIMULATION_DONE success="+ok);Time.timeScale=1;Application.Quit(ok?0:1);
    }
}

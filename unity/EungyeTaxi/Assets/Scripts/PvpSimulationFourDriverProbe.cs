using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Acceptance drives the production four taxis with pedals and native steering.
// Every credited receipt originates in the common displayed call board.
public sealed class PvpSimulationFourDriverProbe : MonoBehaviour {
    readonly StringBuilder report=new StringBuilder();
    bool ok=true,completed;
    void Log(string value){report.AppendLine(value);Debug.Log("PVP_FOUR_DRIVER "+value);}
    void Check(bool value,string why){ok&=value;Log((value?"PASS ":"FAIL ")+why);}
    static bool Paid(DriveSession driver){
        foreach(var item in driver.business.passengers)if(item.paid>0&&item.outcome=="운송 완료"&&!string.IsNullOrEmpty(item.tripId)&&!string.IsNullOrEmpty(item.offerId))return true;
        return false;
    }
    static float TrafficDistance(DriveSession driver){float sum=0;foreach(var car in driver.traffic.cars)if(car)sum+=car.totalTravel;return sum;}
    void Calls(PvpSimulationMatch match,string when){
        var seen=new HashSet<int>();bool full=match.SharedCalls!=null&&match.SharedCalls.Offers.Length==8,shared=full;
        if(full)foreach(var offer in match.SharedCalls.Offers)full&=offer!=null&&seen.Add(offer.pickup);
        foreach(var driver in match.Drivers)shared&=ReferenceEquals(driver.jobs.offers,match.SharedCalls.Offers);
        Check(full&&shared,when+" eight unique waiting passengers in one shared board");
    }
    void NativeReceipts(PvpSimulationMatch match){
        for(int i=0;i<match.Drivers.Count;i++){
            var driver=match.Drivers[i];var ids=new HashSet<string>();bool unique=true;int paid=0;
            foreach(var receipt in driver.business.passengers){
                unique&=ids.Add(receipt.tripId);
                if(receipt.paid>0)paid+=receipt.paid;
                Log("NATIVE_RECEIPT driver="+i+" trip="+receipt.tripId+" offer="+receipt.offerId+" pickup="+receipt.pickupStopId+" drop="+receipt.dropStopId+" outcome="+receipt.outcome+" quoted="+receipt.quoted+" paid="+receipt.paid+" elapsed="+receipt.elapsed);
            }
            match.ObserveServiceTime(driver);int expenses=driver.business.ledger.Account.Expense(driver.business.ledger.Day,null);
            Check(unique&&paid==driver.jobs.balance&&match.Score(i)==paid-expenses,"driver="+i+" exactly-once native fares="+paid+" expenses="+expenses+" score="+match.Score(i));
        }
    }
    IEnumerator Start(){
        float deadline=Time.realtimeSinceStartup+180;PvpSimulationMatch match=null;
        while(!match&&Time.realtimeSinceStartup<deadline){match=FindObjectOfType<PvpSimulationMatch>();yield return null;}
        if(!match||match.Drivers.Count!=4||match.Bots.Count!=3){Check(false,"initialization must create human1 and physical bots3");Complete();yield break;}
        var human=match.Human;
        Check(match.Bot==match.Bots[0],"first bot compatibility");
        Calls(match,"initial");
        var pilots=new PvpSimulationBot[4];
        bool native=true,isolated=true,sharedWorld=true;
        for(int i=0;i<4;i++){
            var driver=match.Drivers[i];
            native&=driver.SimulationDriver==i&&driver.taxi.wheels.Length==4&&driver.damage&&driver.jobs&&driver.law&&driver.business;
            isolated&=driver.business.SimulationSession&&driver.business.SavePath==""&&driver.business.SaveCheckpoint();
            sharedWorld&=driver.world==human.world&&driver.traffic==human.traffic;
            for(int j=0;j<i;j++)isolated&=driver.jobs!=match.Drivers[j].jobs&&driver.business.ledger!=match.Drivers[j].business.ledger&&driver.law.ledger!=match.Drivers[j].law.ledger&&driver.damage!=match.Drivers[j].damage;
            pilots[i]=driver.GetComponent<PvpSimulationBot>();
            if(i==0){driver.taxi.automated=true;driver.assisted.allowAutomated=true;pilots[i]=driver.gameObject.AddComponent<PvpSimulationBot>();pilots[i].Initialize(driver);}
            Log("INITIAL driver="+i+" pos="+driver.taxi.transform.position+" wheels="+driver.taxi.wheels.Length+" score="+match.Score(i));
        }
        Check(native&&isolated&&sharedWorld,"four native physical taxis; independent jobs/law/damage/economy; shared traffic; no campaign saves");
        int npcs=human.traffic.cars.Count;float trafficAt=TrafficDistance(human);
        Check(npcs>0,"native traffic retained count="+npcs);
        Log("CONDITIONS human uses production bot controls; timeScale=3; native offers, poses, fares, deadlines and passenger types unchanged");
        Time.timeScale=3;
        var fullMap=human.GetComponent<FullMapOverlay>();
        if(fullMap){
            float mapClock=match.Elapsed,mapTraffic=TrafficDistance(human);double operation=human.business.ledger.Elapsed;
            Check(fullMap.Open(),"live map opens through native UI");
            yield return new WaitForSecondsRealtime(3);
            bool physicalBrake=true;foreach(var wheel in human.taxi.wheels)physicalBrake&=Mathf.Abs(wheel.motorTorque)<.01f&&wheel.brakeTorque>0;
            Check(fullMap.IsOpen&&!human.paused&&Mathf.Approximately(Time.timeScale,3)&&match.Elapsed>mapClock+2&&human.business.ledger.Elapsed>operation+2,"live map advances match clock and human operation without global pause");
            Check(human.taxi.PvpMapInputBlocked&&physicalBrake,"live map blocks actual automated motor torque and applies physical brakes");
            Check(TrafficDistance(human)>mapTraffic+1,"shared NPC traffic physically advances while map is open");
            fullMap.Close();yield return null;
            Check(!human.taxi.PvpMapInputBlocked&&!human.paused&&Mathf.Approximately(Time.timeScale,3),"map close restores previous simulation speed and vehicle input");
        }else Check(false,"native full map missing");
        deadline=Time.realtimeSinceStartup+600;float nextTelemetry=0;bool allPaid=false,claimsUnique=true;
        while(!match.Finished&&Time.realtimeSinceStartup<deadline){
            var receiptPanel=human.GetComponent<RideCompletionPanel>();if(receiptPanel&&receiptPanel.Open){receiptPanel.Close();Time.timeScale=3;}
            allPaid=true;var claims=new HashSet<int>();
            for(int i=0;i<4;i++){
                var driver=match.Drivers[i];allPaid&=Paid(driver)&&pilots[i].MetresDriven>100;
                if(driver.jobs.Active&&driver.jobs.current!=null)claimsUnique&=claims.Add(driver.jobs.current.pickup);
            }
            if(allPaid)break;
            if(match.Elapsed>=nextTelemetry){
                nextTelemetry=match.Elapsed+10;
                for(int i=0;i<4;i++){
                    var driver=match.Drivers[i];var jobs=driver.jobs;
                    Log("SAMPLE time="+match.Elapsed.ToString("F1")+" driver="+i+" metres="+pilots[i].MetresDriven.ToString("F1")+" pos="+driver.taxi.transform.position+" state="+jobs.trip.State+" routeRemaining="+jobs.nav.remaining.ToString("F1")+" completed="+jobs.completed+" failed="+jobs.failed+" balance="+jobs.balance+" pickup="+(jobs.current==null?-1:jobs.current.pickup)+" drop="+(jobs.current==null?-1:jobs.current.drop)+" status="+pilots[i].Status+" selection="+pilots[i].SelectionDiagnostics+" motion="+pilots[i].MotionDiagnostics);
                }
            }
            yield return new WaitForSecondsRealtime(.25f);
        }
        Check(claimsUnique,"no two native active transports own the same passenger pickup");
        for(int i=0;i<4;i++)Check(pilots[i].MetresDriven>100&&Paid(match.Drivers[i]),"driver="+i+" actual WheelCollider distance="+pilots[i].MetresDriven+"m and positive native delivered receipt");
        NativeReceipts(match);Calls(match,"after driving");
        Check(human.traffic.cars.Count==npcs&&TrafficDistance(human)>trafficAt+100,"shared native NPC pool retained and physically moving");
        int[] scores=new int[4];for(int i=0;i<4;i++)scores[i]=match.Score(i);
        match.Finish();yield return null;
        bool frozen=true;
        for(int i=0;i<4;i++)frozen&=match.Drivers[i].paused&&match.Drivers[i].taxi.autoBrake&&match.Drivers[i].taxi.autoThrottle==0&&match.Score(i)==scores[i]&&!match.Ledger.Settle("late-receipt",i,9999)&&!match.Drivers[i].jobs.Interact();
        Check(frozen&&Time.timeScale==0,"all four results frozen; physical pedals stopped; late native settlements rejected");
        if(!allPaid){Complete();yield break;}
        bool expanded=PvpSimulationMode.Expanded;int seconds=PvpSimulationMode.DurationSeconds;
        DontDestroyOnLoad(gameObject);match.ReturnToMenu();yield return null;yield return null;
        var bootstrap=FindObjectOfType<Pvp.PvpBootstrap>();
        Check(bootstrap&&!PvpSimulationMode.Active&&FindObjectsOfType<DriveSession>().Length==0,"menu return destroys all four sessions and match-local board");
        if(bootstrap){
            bootstrap.StartSimulation(expanded,seconds);deadline=Time.realtimeSinceStartup+180;PvpSimulationMatch replay=null;
            while(!replay&&Time.realtimeSinceStartup<deadline){replay=FindObjectOfType<PvpSimulationMatch>();yield return null;}
            if(replay&&replay.Drivers.Count==4&&replay.Bots.Count==3){
                Calls(replay,"restart");bool reset=true;
                for(int i=0;i<4;i++){
                    var driver=replay.Drivers[i];reset&=driver.SimulationDriver==i&&replay.Score(i)==0&&driver.jobs.completed==0&&driver.jobs.balance==0&&driver.business.passengers.Count==0&&driver.business.ledger.Care.Damage.Durability==100&&driver.business.ledger.Account.Fuel>59;
                }
                Check(reset,"restart resets all four receipts/score/condition; native sessions recreated");replay.Finish();
            }else Check(false,"restart creates four native drivers");
        }
        Complete();
    }
    void Complete(){
        if(completed)return;completed=true;
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-pvpSimulationOutput");
        string folder=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"pvp-four-driver-probe");
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"pvp-simulation-four-driver.txt"),"success="+ok+"\n"+report);
        Debug.Log(ok?"PVP_FOUR_DRIVER_OK":"PVP_FOUR_DRIVER_FAIL");Time.timeScale=1;Application.Quit(ok?0:1);
    }
}

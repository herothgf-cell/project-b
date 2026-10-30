using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Explicit low-fuel precondition only. All approach, purchases and exit after
// it are the production bot's physical controls with the shared traffic intact.
[DefaultExecutionOrder(-10000)]
public sealed class PvpSimulationServicesProbe : MonoBehaviour {
    PvpSimulationMatch match;
    DriveSession bot;
    PvpSimulationBot pilot;
    readonly StringBuilder report=new StringBuilder();
    bool prepared,ok=true,finished,purchased,locked,independent,exited;
    int initialCash,initialExpenses,initialFuelExpense,initialNpcCount;
    float startDistance,npcDistance,startReal,nextSample,firstLockElapsed=-1;
    double humanAtLock;

    void Check(bool value,string detail){ok&=value;Log((value?"PASS ":"FAIL ")+detail);}
    void Log(string detail){report.AppendLine(detail);Debug.Log("PVP_SIMULATION_SERVICES "+detail);}
    float TrafficDistance(){float sum=0;foreach(var car in match.Human.traffic.cars)if(car)sum+=car.totalTravel;return sum;}
    // Initialization finishes in DriveSession's coroutine, before LateUpdate.
    // Observe that frame so the bot has no opportunity to accept its first job.
    void LateUpdate(){
        if(prepared||finished)return;
        if(!match)match=FindObjectOfType<PvpSimulationMatch>();
        if(!match||!match.Bot||!match.Bot.StartupReady)return;
        bot=match.Bot;pilot=bot.GetComponent<PvpSimulationBot>();
        if(!pilot)return;
        pilot.enabled=false;
        Check(!bot.jobs.Active&&bot.jobs.completed==0,"precondition before first passenger acceptance");
        var e=bot.business.ledger;var account=e.Account;
        double elapsed=e.Elapsed,metres=e.Metres;int score=match.BotScore;
        float before=account.Fuel;
        if(bot.business.settings.fuelPerKm<=0){Check(false,"native fuel consumption configured");prepared=true;return;}
        // This is a condition fixture, never credited as driving or transport.
        account.Consume(0,Math.Max(0,before-24)/bot.business.settings.fuelPerKm*1000);
        Check(account.Fuel>23.9f&&account.Fuel<24.1f&&e.Elapsed==elapsed&&e.Metres==metres&&match.BotScore==score,
            "LOW_FUEL_FIXTURE via native Account.Consume only; fuel="+account.Fuel+" no operation metres/time/score");
        initialCash=account.Cash;initialExpenses=account.Expense(e.Day,null);initialFuelExpense=account.Expense(e.Day,"fuel");
        startDistance=pilot.MetresDriven;initialNpcCount=match.Human.traffic.cars.Count;npcDistance=TrafficDistance();
        Check(match.Human.traffic==bot.traffic&&initialNpcCount>0,"original shared NPC traffic retained count="+initialNpcCount);
        startReal=Time.realtimeSinceStartup;nextSample=startReal;prepared=true;
        pilot.enabled=true;Time.timeScale=3;
    }
    IEnumerator Start(){
        float initializationDeadline=Time.realtimeSinceStartup+180;
        while(!prepared&&Time.realtimeSinceStartup<initializationDeadline)yield return null;
        if(!prepared){Check(false,"initialization timeout");Complete();yield break;}
        if(!ok){Complete();yield break;}
        float previousFuel=bot.business.ledger.Account.Fuel;
        while(!finished&&Time.realtimeSinceStartup-startReal<300&&!match.Finished){
            var e=bot.business.ledger;var account=e.Account;
            var planner=bot.GetComponent<PvpSimulationBotServices>();
            int fuelExpense=account.Expense(e.Day,"fuel");
            if(!purchased&&fuelExpense>initialFuelExpense){
                purchased=true;
                Check(bot.services.AtFuelPump(),"native paid fuel purchase occurs at actual pump");
                Check(account.Cash<initialCash&&match.BotScore==bot.jobs.balance-account.Expense(e.Day,null),
                    "native fuel expense debits cash and competition score cash="+account.Cash+" score="+match.BotScore+" expenses="+account.Expense(e.Day,null));
                Check(account.Fuel>previousFuel+5,"purchased fuel replenishes actual account fuel="+account.Fuel+" before="+previousFuel);
            }
            if(purchased&&match.ServiceLocked(bot)){
                locked=true;
                CheckOnceHumanLock();
                if(firstLockElapsed<0){firstLockElapsed=match.Elapsed;humanAtLock=match.Human.business.ledger.Elapsed;}
                if(match.Elapsed>firstLockElapsed+1&&match.Human.business.ledger.Elapsed>humanAtLock+.5)independent=true;
            }
            if(purchased&&planner&&!planner.Active&&!bot.services.AtFuelPump()&&bot.jobs.Active){exited=true;break;}
            if(Time.realtimeSinceStartup>=nextSample){
                nextSample=Time.realtimeSinceStartup+5;
                Log("SAMPLE elapsed="+match.Elapsed.ToString("F1")+" metres="+(pilot.MetresDriven-startDistance).ToString("F1")+" pos="+bot.taxi.transform.position+" speed="+bot.taxi.Speed.ToString("F2")+" fuel="+account.Fuel.ToString("F2")+" fuelExpense="+fuelExpense+" score="+match.BotScore+" wait="+match.ServiceRemaining(bot).ToString("F1")+" trip="+bot.jobs.trip.State+" pilot="+pilot.Status+" planner="+(planner?planner.Status:"none")+" npcMetres="+(TrafficDistance()-npcDistance).ToString("F0"));
            }
            previousFuel=account.Fuel;
            yield return null;
        }
        Check(pilot.MetresDriven-startDistance>100,"actual bot physics distance >100m: "+(pilot.MetresDriven-startDistance));
        Check(purchased,"native physical approach and paid refuelling completed");
        Check(locked&&independent,"payer service wait observed while human economy and shared clock continue");
        Check(exited,"physical facility exit and resumed native passenger job");
        Check(bot.business.ledger.Account.Expense(bot.business.ledger.Day,null)>initialExpenses,"real service expense retained");
        Check(match.Human.traffic.cars.Count==initialNpcCount&&TrafficDistance()>npcDistance+20,"NPC pool retained and physically moving");
        Check(bot.jobs.completed==0||match.BotScore==bot.jobs.balance-bot.business.ledger.Account.Expense(bot.business.ledger.Day,null),"no service-created fare; accounting remains completed fares minus expense");
        Complete();
    }
    bool humanLockChecked;
    void CheckOnceHumanLock(){if(humanLockChecked)return;humanLockChecked=true;Check(!match.ServiceLocked(match.Human)&&!match.Human.paused&&Time.timeScale>0,"bot service locks only payer, human continues");}
    void Complete(){
        if(finished)return;finished=true;
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-pvpSimulationOutput");
        string folder=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"pvp-services-probe");
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"pvp-simulation-services.txt"),"success="+ok+"\n"+report);
        Debug.Log(ok?"PVP_SIMULATION_SERVICES_OK":"PVP_SIMULATION_SERVICES_FAIL");Time.timeScale=1;Application.Quit(ok?0:1);
    }
}


using System;
using System.Linq;
using UnityEngine;

public static class PvpConditionBotTests {
    static void Check(bool value,string message){if(!value)throw new Exception("PVP condition bot: "+message);}
    static DriverCondition Driver(double satiety,double fatigue,double exposure=0){return new DriverCondition{satiety=satiety,fatigue=fatigue,drowsiness=new Drowsiness{visionSeconds=exposure}};}
    static int Choose(DriverCondition d,double trip,double remaining,double meal,double rest){return PvpSimulationBotServices.NeededConditionService(d,trip,remaining,meal,rest);}
    public static void Run(){
        Check(Choose(Driver(80,55),180,1000,20,20)==-1,"fatigue55 alone no longer forces rest");
        Check(Choose(Driver(80,55),600,1000,20,20)==3,"long upcoming trip predicts risk before pickup despite moderate fatigue");
        Check(Choose(Driver(20,45),180,600,10,50)==1,"hungry safe driver chooses shorter preventive meal");
        Check(Choose(Driver(80,80),240,800,10,30)==3,"high projected fatigue requires rest despite available food");
        Check(Choose(Driver(20,70),180,600,5,20)==1,"food can prevent approaching fatigue100 while still below limit");
        Check(Choose(Driver(20,70),180,600,100,10)==3,"long food detour erases its thirty second advantage");
        Check(Choose(Driver(20,100,60),100,500,0,0)==3,"meal cannot cure existing fatigue100 or drowsiness");
        Check(Choose(Driver(80,90),120,60,0,0)==-1,"safe final minute avoids an unneeded stop");
        Check(Choose(Driver(20,70),180,40,10,10)==-1,"service that cannot finish before race end is skipped");
        Check(Choose(Driver(20,100,60),100,500,double.PositiveInfinity,double.PositiveInfinity)==-1,"unreachable sites do not create a perpetual service journey");
        Check(Choose(Driver(20,70),180,600,double.PositiveInfinity,10)==3,"unaffordable or absent meal falls back to free rest when risk requires it");
        var human=Driver(20,70);var bot=human.Copy();
        Check(Choose(human,180,600,5,20)==Choose(bot,180,600,5,20),"actor identity does not affect policy");
        var humanMeal=PvpConditionRules.PreviewMeal(human);var botMeal=PvpConditionRules.PreviewMeal(bot);
        Check(humanMeal.satiety==botMeal.satiety&&humanMeal.fatigue==botMeal.fatigue&&human.satiety==20&&human.fatigue==70,"planning copies share actor effects without granting recovery");
        var ledger=new EconomyLedger(new PrototypeSettings{oneDay=true,startCash=10000});
        Check(ledger.EnablePvpProfile(),"completion regression uses real PVP ledger");
        int mealCount=ledger.PvpMeals,restCount=ledger.PvpRests;
        Check(!PvpSimulationBotServices.ConditionServiceCompleted(ledger,mealCount,restCount),"pending or canceled service does not consume bot visit");
        Check(ledger.CompletePvpMeal("bot-meal-completed")&&ledger.Driver.meals==0,"PVP meal completion leaves legacy campaign counter unchanged");
        Check(PvpSimulationBotServices.ConditionServiceCompleted(ledger,mealCount,restCount),"PVP meal completion latches visit despite unchanged campaign counter");
        mealCount=ledger.PvpMeals;restCount=ledger.PvpRests;
        Check(!PvpSimulationBotServices.ConditionServiceCompleted(ledger,mealCount,restCount),"completed visit snapshot does not report another purchase");
        Check(ledger.CompletePvpRest("bot-rest-completed")&&ledger.Driver.sleeps==0,"PVP rest completion leaves legacy campaign counter unchanged");
        Check(PvpSimulationBotServices.ConditionServiceCompleted(ledger,mealCount,restCount),"PVP rest completion latches visit despite unchanged campaign counter");
        var map=PvpCityMap.Create();
        Check(map.stops.Length==18&&ProjectMapScene.Services(map,"parking").Count()==4&&FuelStationLayout.For(map).Count==3,"condition sites preserve existing transport/fuel/parking infrastructure");
        var meals=ProjectMapScene.Services(map,"restaurant").ToArray();Check(meals.Length==2,"two distributed meal counters exist");
        foreach(var meal in meals){
            var parking=ProjectMapScene.Services(map,"parking").Single(o=>o.id==meal.id.Replace("city:meal:","city:start:"));
            var access=Array.Find(map.roads,r=>r.id==parking.id+":access");
            Check(access!=null&&access.points.Length>=4&&meal.Position==parking.Position,"meal reuses authored through driveway and safe bay");
            Check(ProjectMapScene.CanUse(meal,meal.Position,meal.Direction,0)&&ProjectMapScene.CanUse(parking,meal.Position,meal.Direction,0),"human and bot same stopped bay permits either meal or rest");
            Check(!ProjectMapScene.CanUse(meal,meal.Position,meal.Direction,1),"moving taxis cannot consume at meal counter");
        }
        Debug.Log("PVP_CONDITION_BOT_TESTS_OK");
    }
}

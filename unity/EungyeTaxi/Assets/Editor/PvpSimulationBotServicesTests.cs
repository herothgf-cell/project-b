using System;
public static class PvpSimulationBotServicesTests {
    static void Check(bool value,string message){if(!value)throw new Exception("Bot services: "+message);}
    public static void Run(){
        Check(PvpSimulationBotServices.NeededService(60,10,100,false,100)==-1,"healthy taxi continues competing");
        Check(PvpSimulationBotServices.NeededService(24,10,100,false,100)==0,"low fuel plans physical refuelling before depletion");
        Check(PvpSimulationBotServices.NeededService(25,10,100,false,100)==-1,"fuel boundary does not repeatedly divert");
        Check(PvpSimulationBotServices.NeededService(60,55,100,false,100)==3,"rest is planned before fatigue100 control loss");
        Check(PvpSimulationBotServices.NeededService(60,10,44,false,100)==4,"wash is planned before refusal threshold30");
        Check(PvpSimulationBotServices.NeededService(60,10,100,true,100)==4,"vomit requires washing even with high numeric cleanliness");
        Check(PvpSimulationBotServices.NeededService(60,10,100,false,44)==5,"repair is planned before vehicle failure");
        Check(PvpSimulationBotServices.NeededService(60,55,20,true,100)==4,"passenger refusal resolves before new calls");
        UnityEngine.Debug.Log("PVP_SIMULATION_BOT_SERVICES_TESTS_OK");
    }
}

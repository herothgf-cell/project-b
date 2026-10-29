using System;
using UnityEngine;
public static class VehicleDurabilityIntegrationTests {
    static int checks;
    static void Check(bool value,string reason){checks++;if(!value)throw new Exception("DURABILITY: "+reason);}
    public static void Run() {
        Check(typeof(DriveSession).Assembly.GetType("VehicleDamageController")!=null,"campaign collision and repair controller is missing");
        Check(VehicleCare.Restore(JsonUtility.FromJson<VehicleCareSnapshot>("{}")).Damage.Durability==100,"legacy JsonUtility starts at100");
        Check(VehicleCare.Restore(JsonUtility.FromJson<VehicleCareSnapshot>("{\"damage\":{\"durability\":0}}")).Damage.Durability==100,"legacy phantom nested object does not disable car");
        var care=new VehicleCare();care.Damage.Impact(10);care.MarkVomit();
        var restored=VehicleCare.Restore(JsonUtility.FromJson<VehicleCareSnapshot>(JsonUtility.ToJson(care.Snapshot())));
        Check(restored.Damage.Durability==70&&restored.VomitUnwashed,"current JsonUtility preserves damage and independent dirt lock");
        Check(restored.Damage.Impact(10)==0&&restored.Damage.Impact(15)==20,"JSON incident keeps already charged tier");
        bool rejected=false;try{VehicleCare.Restore(JsonUtility.FromJson<VehicleCareSnapshot>("{\"damageVersion\":1}"));}catch(ArgumentException){rejected=true;}
        Check(rejected,"current JSON cannot omit damage payload");
        Check(VehicleDamageController.ClosingSpeed(Vector3.forward*10,Vector3.back)==10,"normal approach");
        Check(VehicleDamageController.ClosingSpeed(Vector3.right*10,Vector3.back)==0,"tangential motion");
        Check(VehicleDamageController.ClosingSpeed(Vector3.back*10,Vector3.back)==0,"separating motion");
        Check(VehicleDamageController.SupportingContact(null,Vector3.up,0),"supporting ground excluded");
        Debug.Log("VEHICLE_DURABILITY_EDITOR_OK checks="+checks);
    }
    public static void Build(){Run();PassengerBehaviorReleaseTests.Run();PrototypeBuild.BuildHybrid();}
}

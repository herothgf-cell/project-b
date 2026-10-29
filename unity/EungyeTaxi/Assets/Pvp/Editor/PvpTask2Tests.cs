using System;
using UnityEngine;
namespace Pvp.Tests.EditMode {
public static class PvpTask2Tests {
    public static void Run() {
        PvpEditorIsolationTests.Run();
        PvpMapTests.Run();
        PvpVehicleTests.Run();
        var type=typeof(PvpBootstrap).Assembly.GetType("Pvp.PvpPracticeSession");
        if(type==null) throw new Exception("PVP_TASK2: offline practice session missing");
        var owner=new GameObject("PVP integration fixture");
        try {
            var session=owner.AddComponent(type);
            var definition=Pvp.Map.PvpMapDefinition.CreateDefault();
            try {
                type.GetMethod("Begin").Invoke(session,new object[]{definition});
                var map=(Pvp.Map.PvpMapRuntime)type.GetProperty("Map").GetValue(session,null);
                if(map.Stops.Count!=4||map.MapHash.Length!=64)throw new Exception("PVP_TASK2: own validated map required");
                var before=(Pvp.Driving.PvpVehicleState)type.GetMethod("Capture").Invoke(session,null);
                var input=new Pvp.Driving.PvpVehicleInput(1,0,0);
                for(int i=0;i<50;i++)type.GetMethod("Step").Invoke(session,new object[]{input,0.02f});
                var after=(Pvp.Driving.PvpVehicleState)type.GetMethod("Capture").Invoke(session,null);
                if(Vector3.Distance(before.Position,after.Position)<1||after.Speed<=0)throw new Exception("PVP_TASK2: actual vehicle integration did not move");
                type.GetMethod("Recover").Invoke(session,null);
                var recovered=(Pvp.Driving.PvpVehicleState)type.GetMethod("Capture").Invoke(session,null);
                if(recovered.Speed!=0)throw new Exception("PVP_TASK2: recovery must stop vehicle");
                type.GetMethod("NextTarget").Invoke(session,null);
                var route=(Pvp.Map.PvpRoute)type.GetProperty("CurrentRoute").GetValue(session,null);
                if(route==null||!route.Found)throw new Exception("PVP_TASK2: target route missing");
            } finally { UnityEngine.Object.DestroyImmediate(definition); }
        } finally { UnityEngine.Object.DestroyImmediate(owner); }
        var retryOwner=new GameObject("PVP invalid-map retry fixture");
        var invalid=Pvp.Map.PvpMapDefinition.CreateDefault();
        invalid.Spawns=new int[0];
        var valid=Pvp.Map.PvpMapDefinition.CreateDefault();
        try {
            var bootstrap=retryOwner.AddComponent<PvpBootstrap>();bootstrap.PracticeMap=invalid;
            bootstrap.StartPractice();
            if(retryOwner.GetComponent<PvpPracticeSession>()!=null||String.IsNullOrEmpty(bootstrap.StartupError))throw new Exception("PVP_TASK2: failed start must clean session and report cause");
            bootstrap.PracticeMap=valid;bootstrap.StartPractice();
            if(retryOwner.GetComponent<PvpPracticeSession>()==null)throw new Exception("PVP_TASK2: valid retry must start");
        } finally {UnityEngine.Object.DestroyImmediate(retryOwner);UnityEngine.Object.DestroyImmediate(invalid);UnityEngine.Object.DestroyImmediate(valid);}
        Debug.Log("PVP_TASK2_INTEGRATION_OK map/movement/recovery/route/invalid-map-retry");
    }
}}

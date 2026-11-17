using System;
using System.Reflection;
using UnityEngine;

public static class DevRoadOverviewTests
{
    public static void Run(){
        var type=typeof(DrivingSettingsPanel).Assembly.GetType("DevRoadOverviewView");
        if(type==null)throw new Exception("Settings developer road overview is missing");
        var model=Activator.CreateInstance(type);var rect=new Rect(0,0,1200,600);
        type.GetMethod("Configure").Invoke(model,new object[]{new Rect(0,0,1000,2000)});
        Func<Vector2,Vector2> world=p=>(Vector2)type.GetMethod("WorldAt").Invoke(model,new object[]{p,rect});
        var middle=world(rect.center);if(Vector2.Distance(middle,new Vector2(500,1000))>.01f)throw new Exception("Full view centre");
        if(world(new Vector2(600,0)).y<=middle.y)throw new Exception("North must be up");
        var cursor=new Vector2(800,250);var anchor=world(cursor);
        type.GetMethod("ZoomAt").Invoke(model,new object[]{1,cursor,rect});
        if(Vector2.Distance(anchor,world(cursor))>.01f)throw new Exception("Zoom must preserve cursor world point");
        var before=world(rect.center);type.GetMethod("Pan").Invoke(model,new object[]{new Vector2(90,30),rect});
        var after=world(rect.center);if(after.x>=before.x||after.y<=before.y)throw new Exception("Drag direction");
        for(int i=0;i<50;i++)type.GetMethod("ZoomAt").Invoke(model,new object[]{1,rect.center,rect});
        float zoom=(float)type.GetProperty("Zoom").GetValue(model);if(zoom>64||zoom<1)throw new Exception("Zoom cap");
        type.GetMethod("Reset").Invoke(model,null);if(Vector2.Distance(world(rect.center),middle)>.01f)throw new Exception("Reset bounds");
        Debug.Log("DEV_ROAD_OVERVIEW_MODEL_OK checks=6");
    }
    public static void Build(){Run();PrototypeBuild.BuildHybrid();}
}

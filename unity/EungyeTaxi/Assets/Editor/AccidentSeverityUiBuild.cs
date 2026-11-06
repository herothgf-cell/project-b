using System;
using UnityEngine;
public static class AccidentSeverityUiBuild {
 public static void Focused(){int failed=0;foreach(var entry in new[]{"MapLabelReadabilityChecks.Run","RoadPresentationRevisionChecks.Signals","RoadPresentationRevisionChecks.AuthoredSignals","RoadPresentationRevisionChecks.Navigation","RoadPresentationRevisionChecks.CompiledUturn","RoadPresentationRevisionChecks.AuthorizedUturnVisibility","AccidentSeverityChecks.Run","AccidentUiRevisionChecks.Run"})try{var parts=entry.Split('.');typeof(AccidentSeverityUiBuild).Assembly.GetType(parts[0]).GetMethod(parts[1]).Invoke(null,null);}catch(Exception e){failed++;Debug.LogError("ACCIDENT_UI_SUITE_FAIL "+entry+" "+e.GetBaseException());}if(failed>0)throw new Exception("ACCIDENT_UI_REVISION failures="+failed);Debug.Log("ACCIDENT_UI_REVISION_FOCUSED_OK");}
 public static void Run(){Focused();ApproachSignalHeadTests.Run();ImportedMarkingRendererTests.Run();LaneDirectionPaintTests.Run();AccidentIntegrationChecks.Run();WorkshopLayoutTests.Run();HudRevisionBuild.Run();Debug.Log("ACCIDENT_UI_REVISION_INTEGRATION_OK");}
 public static void Build(){Run();ContentTuningBuild.Build();}
}

using System;
public static class RainSettingsRuntime {
 public static bool CanEdit(DriveSession session){return session&&!PvpNativeLan.IsReplica(session)&&!(session.simulationMatch&&session.simulationMatch.Finished);}
 public static bool Apply(DriveSession session,RainSettings settings,out string error){
  error="";if(!CanEdit(session)){error="날씨는 호스트만 변경할 수 있습니다.";return false;}
  string previous=ContentTuningRuntime.Snapshot();try{settings.Install();if(session.business&&!session.business.SaveCheckpoint())throw new InvalidOperationException("날씨 저장 실패 · 이전 설정을 유지합니다.");return true;}catch(Exception e){ContentTuning.UseJson(previous);error=e.Message;return false;}
 }
}

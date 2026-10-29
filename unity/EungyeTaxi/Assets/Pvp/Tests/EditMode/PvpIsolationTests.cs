using System;
using System.Globalization;
using System.IO;
using System.Text;
using Pvp;

// Pure behavior checks; also callable via Unity -executeMethod Pvp.Tests.EditMode.PvpIsolationTests.Run.
namespace Pvp.Tests.EditMode {
public static class PvpIsolationTests {
    const string MapHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    static PvpSessionConfig Config() { return new PvpSessionConfig { MatchDurationSeconds=240, FinishGraceSeconds=30, PlayerCount=2, RulesVersion="fixture-v1" }; }
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Reject(Action action) { try { action(); } catch(ArgumentException) { return; } throw new Exception("Expected invalid input rejection"); }
    public static void MissingDuration_IsRejected() { var config=Config();config.MatchDurationSeconds=null;Reject(()=>config.Freeze("match-1",MapHash)); }
    public static void InvalidSettings_AreRejected() {
        foreach(var duration in new[]{0,-1}) { var c=Config();c.MatchDurationSeconds=duration;Reject(()=>c.Freeze("m",MapHash)); }
        var grace=Config();grace.FinishGraceSeconds=-1;Reject(()=>grace.Freeze("m",MapHash));
        foreach(var count in new[]{-1,0,1}) { var c=Config();c.PlayerCount=count;Reject(()=>c.Freeze("m",MapHash)); }
        var rules=Config();rules.RulesVersion=" ";Reject(()=>rules.Freeze("m",MapHash));
        foreach(var id in new[]{null,""," ","m\nother","a/b"}) Reject(()=>Config().Freeze(id,MapHash));
        foreach(var hash in new[]{null,"","arena-pending",new string('z',64)}) Reject(()=>Config().Freeze("m",hash));
        Check(Config().Freeze("m",MapHash).FinishGraceSeconds==30,"valid settings accepted");
        var zero=Config();zero.FinishGraceSeconds=0;Check(zero.Freeze("m",MapHash).FinishGraceSeconds==0,"zero grace allowed");
    }
    public static void Freeze_IsImmutableAndCultureIndependent() {
        var c=Config();var frozen=c.Freeze("match-1",MapHash);c.MatchDurationSeconds=480;c.RulesVersion="changed";
        Check(frozen.MatchDurationSeconds==240&&frozen.RulesVersion=="fixture-v1"&&frozen.MatchId=="match-1"&&frozen.MapHash==MapHash,"snapshot detached from authoring state");
        Check(frozen.SettingsHash.Length==64,"SHA256 settings fingerprint");
        var before=CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture=new CultureInfo("ar-SA");Check(Config().Freeze("different-match",MapHash.ToUpperInvariant()).SettingsHash==frozen.SettingsHash,"culture and match ID do not change equivalent config/map hash"); }
        finally { CultureInfo.CurrentCulture=before; }
        Action<Action<PvpSessionConfig>> changes=change=>{var other=Config();change(other);Check(other.Freeze("match-1",MapHash).SettingsHash!=frozen.SettingsHash,"each authored gameplay field affects hash");};
        changes(x=>x.MatchDurationSeconds=360);changes(x=>x.FinishGraceSeconds=0);changes(x=>x.PlayerCount=4);changes(x=>x.RulesVersion="fixture-v2");
        Check(Config().Freeze("match-1",new string('f',64)).SettingsHash!=frozen.SettingsHash,"map identity affects hash");
    }
    public static void SavePath_DoesNotTouchCampaign() {
        WithTemp(root=>{
            var campaign=Path.Combine(root,"Taxi Prototype","은계 택시 시뮬레이터");Directory.CreateDirectory(campaign);
            var save=Path.Combine(campaign,"campaign.json");File.WriteAllText(save,"campaign-sentinel");File.WriteAllText(save+".bak","backup-sentinel");
            var store=new PvpLocalStore(root);var path=store.Resolve("preferences.json");
            Check(path==Path.Combine(root,"PvpPrototype","preferences.json"),"explicit PvpPrototype namespace independent of product");
            Check(!Directory.Exists(Path.Combine(root,"PvpPrototype")),"resolving a path has no filesystem side effects");
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,"pvp-only");
            Check(File.ReadAllText(save)=="campaign-sentinel"&&File.ReadAllText(save+".bak")=="backup-sentinel","campaign and backup unchanged");
            Check(Directory.GetFiles(campaign).Length==2,"no migration or extra campaign writes");
        });
    }
    public static void UnsafeFileNames_AreRejected() {
        WithTemp(root=>{
            var store=new PvpLocalStore(root);
            foreach(var name in new[]{null,""," ",".","..","../save","a/b","a\\b","/tmp/save","C:\\save","C:save","a:b","a?b","a*b","a|b","a\"b","a<b","a>b","a\nb","name.","name ","CON","nul.json","COM1.log","lpt9.txt","COM¹","COM²","COM³","LPT¹","LPT²","LPT³","COM¹.log","COM².log","COM³.log","LPT¹.txt","LPT².txt","LPT³.txt"}) Reject(()=>store.Resolve(name));
            Check(store.Resolve("client.preferences.json").EndsWith("client.preferences.json"),"ordinary filename accepted");
        });
    }
    public static void DiagnosticCreation_NeverTruncatesExistingFiles() {
        WithTemp(root=>{
            var store=new PvpLocalStore(root);string first,second;
            using(var stream=store.CreateDiagnostic("session-1")) { first=stream.Name;var bytes=Encoding.UTF8.GetBytes("first-sentinel");stream.Write(bytes,0,bytes.Length); }
            using(var stream=store.CreateDiagnostic("session-1")) { second=stream.Name;Check(stream.Length==0,"new empty file"); }
            Check(first!=second,"same process/session gets distinct diagnostic file");Check(File.ReadAllText(first)=="first-sentinel","previous diagnostic not truncated");
            Check(Path.GetDirectoryName(first)==Path.Combine(root,"PvpPrototype"),"diagnostics are isolated");
            Reject(()=>store.CreateDiagnostic("../session"));
        });
    }
    // Catches missing finally restoration on failed configure/build and success.
    public static void BuildFailure_RestoresSettings() {
        string state="campaign";var failure=new InvalidOperationException("simulated engine build failure");Exception actual=null;
        try { PvpBuildTransaction.Run(()=>state,()=>state="PvpPrototype",()=>{throw failure;},saved=>state=saved); } catch(Exception e) { actual=e; }
        Check(Object.ReferenceEquals(actual,failure),"original build failure propagated");Check(state=="campaign","temporary settings restored after failure");
    }
    public static void BuildSuccessAndConfigureFailure_RestoreSettings() {
        string state="campaign";bool built=false;
        PvpBuildTransaction.Run(()=>state,()=>state="PvpPrototype",()=>{Check(state=="PvpPrototype","build sees temporary settings");built=true;},saved=>state=saved);
        Check(built&&state=="campaign","success restores settings");
        try { PvpBuildTransaction.Run(()=>state,()=>{state="partial";throw new InvalidOperationException();},()=>{throw new Exception("must not build after configure failure");},saved=>state=saved); } catch(InvalidOperationException) { }
        Check(state=="campaign","partial configuration restored");
    }
    static void WithTemp(Action<string> action) { var root=Path.Combine(Path.GetTempPath(),"pvp-isolation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try { action(root); } finally { Directory.Delete(root,true); } }
    public static void Run() {
        int failed=0;
        foreach(var test in new Action[]{MissingDuration_IsRejected,InvalidSettings_AreRejected,Freeze_IsImmutableAndCultureIndependent,SavePath_DoesNotTouchCampaign,UnsafeFileNames_AreRejected,DiagnosticCreation_NeverTruncatesExistingFiles,BuildFailure_RestoresSettings,BuildSuccessAndConfigureFailure_RestoreSettings}) {
            try { test();Console.WriteLine("PASS "+test.Method.Name); } catch(Exception e) { failed++;Console.WriteLine("FAIL "+test.Method.Name+": "+e.Message); }
        }
        if(failed>0) throw new Exception("PVP isolation failed: "+failed);
        Console.WriteLine("PVP_ISOLATION_OK cases=8");
    }
}

}

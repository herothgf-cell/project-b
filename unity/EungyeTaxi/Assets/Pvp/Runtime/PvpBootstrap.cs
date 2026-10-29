using System;
using System.IO;
using UnityEngine;

namespace Pvp {
    // Scene-owned entry point only; opening campaign scenes never installs PvP.
    public sealed class PvpBootstrap : MonoBehaviour {
        StreamWriter diagnostic;
        public PvpMatchSettings CurrentSettings { get; private set; }
        public string DiagnosticPath { get; private set; }
        public string StartupError { get; private set; }
        void Awake() {
            var cameraObject=new GameObject("PVP startup camera");cameraObject.transform.SetParent(transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.08f);camera.cullingMask=0;
            gameObject.AddComponent<PvpPrototypePanel>();
            try {
                var stream=new PvpLocalStore().CreateDiagnostic(Guid.NewGuid().ToString("N"));DiagnosticPath=stream.Name;
                diagnostic=new StreamWriter(stream);diagnostic.AutoFlush=true;
                diagnostic.WriteLine("PVP_STARTUP utc="+DateTime.UtcNow.ToString("O")+" unity="+Application.unityVersion+" diagnostic="+DiagnosticPath);
            } catch(Exception e) { StartupError="Diagnostic storage unavailable: "+e.Message;Debug.LogError(StartupError); }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpIsolationCheck")>=0) gameObject.AddComponent<PvpRuntimeIsolationProbe>();
        }
        // Task2 supplies an actual verified map hash; freezing settings alone does
        // not load an arena, start a network connection, or claim a playable match.
        public void Initialize(PvpMatchSettings settings) {
            if(settings==null) throw new ArgumentNullException("settings");
            if(CurrentSettings!=null) throw new InvalidOperationException("PVP match settings are already frozen for this bootstrap.");
            CurrentSettings=settings;
            if(diagnostic!=null) diagnostic.WriteLine("PVP_SETTINGS match="+settings.MatchId+" map="+settings.MapHash+" settings="+settings.SettingsHash);
        }
        void OnDestroy() { if(diagnostic!=null) { diagnostic.Dispose();diagnostic=null; } }
    }
}

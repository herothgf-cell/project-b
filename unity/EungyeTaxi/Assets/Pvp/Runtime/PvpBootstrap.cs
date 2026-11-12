using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Pvp {
    // Scene-owned entry point only; opening campaign scenes never installs PvP.
    public sealed class PvpBootstrap : MonoBehaviour {
        public Pvp.Map.PvpMapDefinition PracticeMap;
        static bool simulationCommandConsumed; StreamWriter diagnostic;
        Pvp.Map.PvpMapDefinition ownedMap;
        public PvpMatchSettings CurrentSettings { get; private set; }
        public string DiagnosticPath { get; private set; }
        public string StartupError { get; private set; }
        public string RelayError { get; private set; }
        CancellationTokenSource relayCancellation;
        Task<PvpRelaySocket> relayOpening;
        public bool RelayConnecting => relayOpening != null;
        public void ClearRelayError() => RelayError = null;
        void Awake() {
            if(PracticeMap==null||PracticeMap.MapId=="pvp_circuit_01") {ownedMap=Pvp.Map.PvpMapDefinition.CreateExpanded();PracticeMap=ownedMap;}
            var cameraObject=new GameObject("PVP startup camera");cameraObject.transform.SetParent(transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.08f);camera.cullingMask=0;
            gameObject.AddComponent<PvpPrototypePanel>();
            try {
                var store=ContentTuningRuntime.TestMode?new PvpLocalStore(ContentTuningRuntime.TestDirectory):new PvpLocalStore();var stream=store.CreateDiagnostic(Guid.NewGuid().ToString("N"));DiagnosticPath=stream.Name;
                diagnostic=new StreamWriter(stream);diagnostic.AutoFlush=true;
                diagnostic.WriteLine("PVP_STARTUP utc="+DateTime.UtcNow.ToString("O")+" unity="+Application.unityVersion+" diagnostic="+DiagnosticPath);
            } catch(Exception e) { StartupError="Diagnostic storage unavailable: "+e.Message;Debug.LogError(StartupError); }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpExpandedCheck")>=0) gameObject.AddComponent<PvpExpandedProbe>();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpSoloBotCheck")>=0) gameObject.AddComponent<PvpSoloBotProbe>();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpIsolationCheck")>=0) gameObject.AddComponent<PvpRuntimeIsolationProbe>();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNetworkRole")>=0) gameObject.AddComponent<Pvp.Network.PvpNetworkProbe>();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpPracticeCheck")>=0) gameObject.AddComponent<PvpPracticeProbe>();
        }
        void Start() {
            var args=Environment.GetCommandLineArgs();
            bool check=Array.IndexOf(args,"-pvpSimulationCheck")>=0; bool four=Array.IndexOf(args,"-pvpSimulationFourDriverCheck")>=0;
            bool capture=Array.IndexOf(args,"-pvpSimulationCapture")>=0; bool service=Array.IndexOf(args,"-pvpSimulationServicesCheck")>=0;
            if(Array.IndexOf(args,"-pvpSimulationMenuCapture")>=0&&!simulationCommandConsumed){simulationCommandConsumed=true;gameObject.AddComponent<PvpSimulationCapture>();}
            else if((four||check||capture||service)&&!simulationCommandConsumed){simulationCommandConsumed=true;StartSimulation(four||service||Array.IndexOf(args,"-pvpSimulationExpanded")>=0,four?7200:1800);if(four)new GameObject("PVP four-driver native probe").AddComponent<PvpSimulationFourDriverProbe>();if(service)new GameObject("PVP native service probe").AddComponent<PvpSimulationServicesProbe>();if(check)new GameObject("PVP simulation acceptance probe").AddComponent<PvpSimulationProbe>();if(capture)gameObject.AddComponent<PvpSimulationCapture>();}
            else if(Array.IndexOf(args,"-pvpCityPerformanceTest")>=0&&!simulationCommandConsumed){simulationCommandConsumed=true;StartSimulation(true,7200);}
            else if(PvpSimulationMode.Active)StartSimulation(PvpSimulationMode.Expanded,PvpSimulationMode.DurationSeconds);
        }
        public void StartSimulation(bool expanded,int seconds) { if(FindObjectOfType<DriveSession>())return; if(!PvpNativeLan.Instance)ContentTuningRuntime.LoadActive();PvpSimulationMode.Configure(expanded,seconds);var panel=GetComponent<PvpPrototypePanel>();if(panel)panel.enabled=false;foreach(var camera in GetComponentsInChildren<Camera>())camera.gameObject.SetActive(false);var root=new GameObject("PVP simulation · existing DriveSession");root.SetActive(false);root.AddComponent<DriveSession>().startupRegion="eungye";root.SetActive(true); } public void StartNetwork(bool host,string address,ushort port) {
            if(GetComponent<Pvp.Network.PvpNetworkSession>()!=null)return;
            var session=gameObject.AddComponent<Pvp.Network.PvpNetworkSession>();
            try { session.Begin(host,address,port);StartupError=null; }
            catch(Exception e){StartupError="네트워크 시작 실패: "+e.Message;session.Stop();}
        }
        public void StartNativeLan(bool host,string address,ushort port,int seconds){if(PvpNativeLan.Instance)return;var lan=gameObject.AddComponent<PvpNativeLan>();try{lan.Begin(this,host,address,port,seconds);StartupError=null;}catch(Exception e){StartupError="LAN 접속 실패: "+e.Message;lan.StopTransport();Destroy(lan);}}
        public IEnumerator StartRelay(PvpRelayClient client, PvpRelayRoom selection, int seconds)
        {
            if (PvpNativeLan.Instance || RelayConnecting) yield break;
            bool host = selection == null;
            ClearRelayError();
            relayCancellation = new CancellationTokenSource();
            relayOpening = client.Open(selection?.id, DriveFlowPolicy.LocalName + "의 방", seconds,
                PvpNativeLan.MapFingerprint, relayCancellation.Token);
            while (!relayOpening.IsCompleted) yield return null;

            Task<PvpRelaySocket> completed = relayOpening;
            relayOpening = null;
            relayCancellation.Dispose();
            relayCancellation = null;
            PvpRelaySocket socket = null;
            PvpNativeLan lan = null;
            try
            {
                socket = completed.GetAwaiter().GetResult();
                var room = JsonUtility.FromJson<PvpRelayRoom>(socket.Greeting);
                if (room == null || !room.Valid || (!host && room.id != selection.id))
                    throw new InvalidDataException("방 정보가 올바르지 않습니다.");
                if (!room.Compatible) throw new InvalidOperationException("실행본 또는 PvP 지도가 다릅니다.");
                lan = gameObject.AddComponent<PvpNativeLan>();
                lan.BeginRelay(this, host, client, room, socket);
            }
            catch (Exception error)
            {
                socket?.Dispose();
                if (lan) { lan.StopTransport(); Destroy(lan); }
                Debug.LogWarning("PVP_RELAY_CONNECT " + error.GetType().Name + " " + error.Message);
                RelayError = host ? "방을 만들지 못했습니다. 중계 서버 연결을 확인해 주세요."
                                  : "방에 참가하지 못했습니다. 방 목록을 새로고침해 주세요.";
            }
        }
        public void StartSolo(int seconds,int finishGrace) {
            if(GetComponent<Pvp.Network.PvpNetworkSession>()!=null)return;
            var session=gameObject.AddComponent<Pvp.Network.PvpNetworkSession>();
            try {session.BeginOfflineBot(seconds,finishGrace);StartupError=null;}
            catch(Exception e){StartupError="1인 경기를 시작하지 못했습니다: "+e.Message;session.Stop();}
        }
        public void StartPractice() {
            if(GetComponent<PvpPracticeSession>()!=null) return;
            PvpPracticeSession session=null;
            try {
                if(PracticeMap==null) throw new InvalidOperationException("PVP practice map missing");
                session=gameObject.AddComponent<PvpPracticeSession>();session.Begin(PracticeMap);
                StartupError=null;
            } catch(Exception e) {
                StartupError="시험장을 시작하지 못했습니다: "+e.Message;
                if(session!=null) session.ExitPractice();
                Debug.LogWarning(StartupError);
            }
        }
        // Task2 supplies an actual verified map hash; freezing settings alone does
        // not load an arena, start a network connection, or claim a playable match.
        public void Initialize(PvpMatchSettings settings) {
            if(settings==null) throw new ArgumentNullException("settings");
            if(CurrentSettings!=null) throw new InvalidOperationException("PVP match settings are already frozen for this bootstrap.");
            CurrentSettings=settings;
            if(diagnostic!=null) diagnostic.WriteLine("PVP_SETTINGS match="+settings.MatchId+" map="+settings.MapHash+" settings="+settings.SettingsHash);
        }
        void OnDestroy() {
            if (relayOpening != null)
            {
                relayCancellation.Cancel();
                relayCancellation.Dispose();
                _ = relayOpening.ContinueWith(task => {
                    if (task.Status == TaskStatus.RanToCompletion) task.Result.Dispose();
                    else _ = task.Exception;
                }, TaskScheduler.Default);
            }
            if(diagnostic!=null) { diagnostic.Dispose();diagnostic=null; }if(ownedMap!=null){if(Application.isPlaying)Destroy(ownedMap);else DestroyImmediate(ownedMap);}
        }
    }
}

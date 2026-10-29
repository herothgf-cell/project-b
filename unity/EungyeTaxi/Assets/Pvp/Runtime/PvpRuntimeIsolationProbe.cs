using System;
using System.Collections;
using UnityEngine;

namespace Pvp {
    // Added only by the dedicated bootstrap when explicitly requested by CLI.
    // Run the built player twice with -pvpIsolationCheck and separate -logFile paths.
    public sealed class PvpRuntimeIsolationProbe : MonoBehaviour {
        IEnumerator Start() {
            yield return null;
            foreach(var behavior in FindObjectsOfType<MonoBehaviour>()) {
                if(behavior.GetType().Name=="DriveSession") Fail("campaign DriveSession exists");
            }
            var bootstrap=GetComponent<PvpBootstrap>();
            if(bootstrap==null||bootstrap.CurrentSettings!=null) Fail("startup must have no invented match");
            if(GetComponent<PvpPrototypePanel>()==null||GetComponentsInChildren<Camera>().Length!=1) Fail("own UI/camera missing");
            if(String.IsNullOrEmpty(bootstrap.DiagnosticPath)||!System.IO.File.Exists(bootstrap.DiagnosticPath)) Fail("dedicated diagnostic missing");
            Debug.Log("PVP_RUNTIME_ISOLATION_OK diagnostic="+bootstrap.DiagnosticPath);
            Application.Quit(0);
        }
        static void Fail(string message) { Debug.LogError("PVP_RUNTIME_ISOLATION_FAIL "+message);Application.Quit(1);throw new InvalidOperationException(message); }
    }
}

using System.Collections;
using System.IO;
using UnityEngine;
public class CompletionReloadSmokeTest:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-completionReloadTest")>=0)new GameObject("Reload check").AddComponent<CompletionReloadSmokeTest>();}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(1);var s=FindObjectOfType<DriveSession>();var b=s.business;bool legacy=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-legacyCheckpoint")>=0;var saved=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(b.SavePath));bool ok=b.ShowingSummary&&b.ledger.Day==2&&b.passengers.Count==(legacy?0:3)&&b.practices.Count==(legacy?0:1)&&s.law.ledger.Events.Length==saved.law.Length&&b.ledger.Account.Cash==CampaignAccount.Restore(saved.account).Cash&&Time.timeScale==0;File.WriteAllText("artifacts/completion-reload"+(legacy?"-legacy":"")+".txt","success="+ok+" day="+b.ledger.Day+" passengers="+b.passengers.Count+" practices="+b.practices.Count+" law="+s.law.ledger.Events.Length);Application.Quit(ok?0:1);}
}

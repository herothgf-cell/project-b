using UnityEngine;
public static class PvpDriverPresentation {
 public static string Name(int driver){return driver==0?"나":"봇 "+driver;}
 public static Color Color(int driver){return driver==1?new Color(1,.4f,.7f):driver==2?new Color(.3f,.85f,1):driver==3?new Color(1,.75f,.25f):UnityEngine.Color.white;}
 public static string Status(PvpSimulationMatch match,int driver){var s=match.Drivers[driver];string state=match.DriverStatus(driver);if(state.Length>11)state=state.Substring(0,11);return state+" · "+Mathf.Abs(s.taxi.Speed*3.6f).ToString("F0")+" km/h";}
}

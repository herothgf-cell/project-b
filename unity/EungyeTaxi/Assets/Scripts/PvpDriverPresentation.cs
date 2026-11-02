using UnityEngine;
public static class PvpDriverPresentation {
 public static string Name(int driver){return driver==0?"나":"봇 "+driver;}
 public static Color Color(int driver){return driver==1?new Color(.72f,.12f,.42f):driver==2?CasualUI.Blue:driver==3?CasualUI.Amber:CasualUI.Green;}
 public static string Status(PvpSimulationMatch match,int driver){var s=match.Drivers[driver];string state=match.DriverStatus(driver);if(state.Length>11)state=state.Substring(0,11);return state+" · "+Mathf.Abs(s.taxi.Speed*3.6f).ToString("F0")+" km/h";}
}

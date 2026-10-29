using System;
using System.Security.Cryptography;
namespace Pvp.Network {
// One guest slot in a host-owned trial. Tickets never leave memory except the direct-IP handshake.
public sealed class PvpReconnectRegistry {
 readonly double grace;string ticket;bool expired;
 public bool HasSlot {get;private set;}public bool Connected {get;private set;}public bool Reserved {get;private set;}public bool Withdrawn {get;private set;}
 public ulong Connection {get;private set;}public int Epoch {get;private set;}public double Expiry {get;private set;}
 public PvpReconnectRegistry(double seconds){if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0||seconds>120)throw new ArgumentOutOfRangeException(nameof(seconds));grace=seconds;}
 public bool TryReserve(ulong connection,string credential,double now,out bool resumed,out string reason){
  CheckTime(now);resumed=false;reason=null;Expire(now);
  if(connection==0){reason="invalid guest connection";return false;}
  if(!HasSlot){if(!string.IsNullOrEmpty(credential)){reason="invalid reconnect ticket";return false;}byte[] bytes=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(bytes);ticket=Convert.ToBase64String(bytes);HasSlot=true;Connection=connection;Epoch=1;Reserved=true;Expiry=now+grace;return true;}
  if(!Equal(ticket,credential)){reason="invalid reconnect ticket";return false;}
  if(Withdrawn){reason="left session";return false;}if(expired){reason="reconnect expired";return false;}
  if(Connected||Reserved){reason="already connected";return false;}
  Connection=connection;Epoch++;Reserved=true;resumed=true;return true;
 }
 public bool Confirm(ulong connection,double now){CheckTime(now);Expire(now);if(connection!=Connection||!Reserved||expired||Withdrawn)return false;Reserved=false;Connected=true;return true;}
 public string TicketFor(ulong connection){if(!Connected||connection!=Connection)throw new InvalidOperationException("No active ticket recipient");return ticket;}
 public bool Disconnect(ulong connection,double now){CheckTime(now);if(!HasSlot||connection!=Connection||Withdrawn)return false;if(Connected)Expiry=now+grace;Connected=false;Reserved=false;Expire(now);return true;}
 public bool Leave(ulong connection){if(!Connected||connection!=Connection)return false;Withdrawn=true;Connected=false;Reserved=false;return true;}
 public bool CanDrive(ulong connection,int epoch){return Connected&&!Withdrawn&&Connection==connection&&Epoch==epoch;}
 public void Expire(double now){CheckTime(now);if(HasSlot&&!Connected&&now>=Expiry){expired=true;Reserved=false;}}
 public bool Expired {get{return expired;}}
 static bool Equal(string a,string b){if(a==null||b==null||a.Length!=b.Length)return false;int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];return diff==0;}
 static void CheckTime(double value){if(double.IsNaN(value)||double.IsInfinity(value)||value<0)throw new ArgumentOutOfRangeException(nameof(value));}
}
}
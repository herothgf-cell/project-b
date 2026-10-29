using System;
namespace Pvp.Passengers {
public sealed class PvpPassengerCard {
 public readonly PassengerType Type;public readonly int Variant;public readonly string TypeLabel,VariantLabel,Dialogue;
 internal PvpPassengerCard(PassengerBehaviorState value){Type=value.Type;Variant=value.Variant;TypeLabel=value.TypeLabel;VariantLabel=value.VariantLabel;Dialogue=value.Dialogue;}
}
// Read-only content bridge. Does not board, advance, settle, save, or apply campaign dirt/fare effects.
public static class PvpPassengerCatalog {
 public const int Count=37; // General3 + completed special variants34.
 public static PvpPassengerCard At(int index){
  if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
  foreach(PassengerType type in Enum.GetValues(typeof(PassengerType))){int count=type==PassengerType.Sleeping?4:3;if(index>=count){index-=count;continue;}
   var value=PassengerBehaviorState.Create("pvp-content-preview",type);value.Variant=index;value.Quiet=type==PassengerType.Drunk&&index==2;value.Validate();return new PvpPassengerCard(value);
  }throw new InvalidOperationException("Passenger catalog changed; update the reviewed PVP mapping");
 }
}
}
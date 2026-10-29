using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Pvp {
    // Authoring values only. No product duration or player-count default is selected.
    public sealed class PvpSessionConfig {
        public int? MatchDurationSeconds;
        public int FinishGraceSeconds;
        public int PlayerCount;
        public string RulesVersion;

        public void Validate() {
            if(!MatchDurationSeconds.HasValue || MatchDurationSeconds.Value<=0) throw new ArgumentException("Match duration is required and must be positive (seconds).");
            if(FinishGraceSeconds<0) throw new ArgumentException("Finish grace cannot be negative.");
            if(PlayerCount<2) throw new ArgumentException("Player count must be at least two.");
            ValidateId(RulesVersion,"Rules version");
        }
        public PvpMatchSettings Freeze(string matchId,string mapHash) {
            Validate();ValidateId(matchId,"Match ID");
            if(mapHash==null || mapHash.Length!=64) throw new ArgumentException("Map identity must be a SHA256 content hash.");
            foreach(char c in mapHash) if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F'))) throw new ArgumentException("Map identity must be a SHA256 content hash.");
            mapHash=mapHash.ToLowerInvariant();
            // Versioned, invariant encoding; IDs cannot contain the newline delimiter.
            // Match ID is separate identity, so equivalent map/configs share a fingerprint.
            string canonical="pvp-settings-v1\n"+RulesVersion+"\n"+mapHash+"\n"+MatchDurationSeconds.Value.ToString(CultureInfo.InvariantCulture)+"\n"+FinishGraceSeconds.ToString(CultureInfo.InvariantCulture)+"\n"+PlayerCount.ToString(CultureInfo.InvariantCulture)+"\n";
            string hash;
            using(var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-","").ToLowerInvariant();
            return new PvpMatchSettings(matchId,mapHash,MatchDurationSeconds.Value,FinishGraceSeconds,PlayerCount,RulesVersion,hash);
        }
        internal static void ValidateId(string value,string label) {
            if(String.IsNullOrEmpty(value)||value.Length>128) throw new ArgumentException(label+" is required (maximum 128 characters).");
            foreach(char c in value) if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='-'||c=='_'||c=='.')) throw new ArgumentException(label+" must use letters, numbers, '.', '-' or '_'.");
        }
    }
    public sealed class PvpMatchSettings {
        public readonly string MatchId,MapHash,RulesVersion,SettingsHash;
        public readonly int MatchDurationSeconds,FinishGraceSeconds,PlayerCount;
        internal PvpMatchSettings(string id,string map,int duration,int grace,int count,string rules,string hash) {
            MatchId=id;MapHash=map;MatchDurationSeconds=duration;FinishGraceSeconds=grace;PlayerCount=count;RulesVersion=rules;SettingsHash=hash;
        }
    }
}

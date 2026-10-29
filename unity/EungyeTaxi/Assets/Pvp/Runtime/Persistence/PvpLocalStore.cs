using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Pvp {
    public sealed class PvpLocalStore {
        readonly string root;
        // LocalApplicationData is stable across product/executable names. Do not use
        // Application.persistentDataPath, which derives from Unity company/product.
        public PvpLocalStore() : this(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) { }
        public PvpLocalStore(string localDataRoot) {
            if(String.IsNullOrWhiteSpace(localDataRoot)) throw new ArgumentException("Local application data root is required.");
            root=Path.Combine(Path.GetFullPath(localDataRoot),"PvpPrototype");
        }
        public string Resolve(string fileName) {
            ValidateFileName(fileName);
            return Path.Combine(root,fileName);
        }
        public FileStream CreateDiagnostic(string sessionId) {
            PvpSessionConfig.ValidateId(sessionId,"Diagnostic session ID");
            Directory.CreateDirectory(root);
            // CreateNew is the authority: even an existing matching random filename
            // cannot be truncated. Share.Read allows inspection while the client runs.
            for(int attempt=0;attempt<8;attempt++) {
                string file="diagnostic-"+Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)+"-"+sessionId+"-"+Guid.NewGuid().ToString("N")+".log";
                string path=Resolve(file);
                try { return new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read); }
                catch(IOException) { if(!File.Exists(path)) throw; }
            }
            throw new IOException("Could not allocate a unique PVP diagnostic file.");
        }
        static void ValidateFileName(string name) {
            if(String.IsNullOrWhiteSpace(name)||name=="."||name==".."||name.Length>240||name.EndsWith(".",StringComparison.Ordinal)||name.EndsWith(" ",StringComparison.Ordinal)) throw new ArgumentException("A plain filename is required.");
            if(Path.IsPathRooted(name)) throw new ArgumentException("Rooted paths are not allowed.");
            foreach(char c in name) if(c<32 || "<>:\"/\\|?*".IndexOf(c)>=0) throw new ArgumentException("Filename contains a path separator or invalid character.");
            // Windows device names are invalid even when running these checks on Linux.
            string stem=name.Split('.')[0].ToUpperInvariant();
            if(stem=="CON"||stem=="PRN"||stem=="AUX"||stem=="NUL"||(stem.Length==4&&(stem.StartsWith("COM",StringComparison.Ordinal)||stem.StartsWith("LPT",StringComparison.Ordinal))&&"123456789¹²³".IndexOf(stem[3])>=0)) throw new ArgumentException("Reserved device filename.");
        }
    }
}

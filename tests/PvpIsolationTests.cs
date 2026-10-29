using System;
using System.IO;
using System.Text;
using Pvp;
using Pvp.Tests.EditMode;
public static class PvpIsolationTestRunner {
    public static int Main(string[] args) {
        try {
            if(args.Length==2&&args[0]=="--diagnostic") {
                using(var stream=new PvpLocalStore(args[1]).CreateDiagnostic("two-client-fixture")) {
                    var bytes=Encoding.UTF8.GetBytes("client-process="+System.Diagnostics.Process.GetCurrentProcess().Id);
                    stream.Write(bytes,0,bytes.Length);Console.WriteLine(stream.Name);
                }
                return 0;
            }
            PvpIsolationTests.Run();return 0;
        } catch(Exception e) { Console.Error.WriteLine(e);return 1; }
    }
}

using System;
namespace Pvp {
    // Capture/restore are real state operations; only the engine build boundary
    // needs replacement when checking failure behavior outside Unity.
    public static class PvpBuildTransaction {
        public static void Run<T>(Func<T> capture,Action configure,Action build,Action<T> restore) {
            T original=capture();
            try { configure();build(); }
            finally { restore(original); }
        }
    }
}

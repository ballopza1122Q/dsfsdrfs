using System;
using System.IO;
using UnityEngine;

namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static bool MzConfigCleanupAttempted, MzConfigRemoved, MzPatchRemoved, MzLegacyCleanupFinished;

        public static bool MzRemoveOwnedArtifact(string path)
        {
            try{File.Delete(path);return true;}
            catch(DirectoryNotFoundException){return true;}
            catch(Exception){return false;}
        }

        // Executed by an already loaded patch, including the normal CDN bundle
        // path. This does not claim to activate the Documents loader beforehand.
        public static void MzRemoveLocalConfig()
        {
            if(MzConfigCleanupAttempted)return;
            MzConfigCleanupAttempted=true;
            try
            {
                string folder=Application.persistentDataPath;
                if(String.IsNullOrEmpty(folder))return;
                // This code runs only after the VM has loaded the patch. Touch
                // explicit old menu artifacts only; no discovery or recursive delete.
                MzConfigRemoved=MzRemoveOwnedArtifact(Path.Combine(folder,"localConfig.json"));
                MzPatchRemoved=MzRemoveOwnedArtifact(Path.Combine(folder,"Assembly-CSharp-patch.bytes"));
                bool clean=MzRemoveOwnedArtifact(Path.Combine(folder,"Mizze-webview.log"));
                clean=MzRemoveOwnedArtifact(Path.Combine(folder,"Mizze-session.log"))&&clean;
                clean=MzRemoveOwnedArtifact(Path.Combine(folder,"Mizze-startup.log"))&&clean;
                clean=MzRemoveOwnedArtifact(Path.Combine(folder,"Mizze-error.log"))&&clean;
                string legacy=Path.Combine(folder,"MizzeWeb");
                // If there is an unrelated file inside, nonrecursive Delete fails
                // and preserves that file and the containing directory.
                clean=MzRemoveOwnedArtifact(Path.Combine(legacy,"menu.html"))&&clean;
                try{Directory.Delete(legacy,false);}catch(DirectoryNotFoundException){}catch(Exception){clean=false;}
                MzLegacyCleanupFinished=clean;
            }
            catch(Exception) { /* Configuration I/O must never prevent the menu. */ }
        }
    }
}

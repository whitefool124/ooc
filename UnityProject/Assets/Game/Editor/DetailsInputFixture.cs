using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem;

namespace OCC.Combat.EditorTools
{
    // Isolate automated Input System pointers from unrelated OS mouse events.
    // Only this Play session's Unity input device is suspended; the OS mouse is untouched.
    [InitializeOnLoad]
    internal static class DetailsInputFixture
    {
        private const string Prefix="OCC.DetailsInputFixture.";
        private const string SimulatedName="OCCDetailsVerificationMouse";
        private static Mouse native, simulated;
        private static bool enabled, configured, background;
        static DetailsInputFixture()
        {
            AssemblyReloadEvents.beforeAssemblyReload += End;
            EditorApplication.quitting += End;
            EditorApplication.playModeStateChanged += state =>
            {
                if(state==PlayModeStateChange.ExitingPlayMode || state==PlayModeStateChange.EnteredEditMode)End();
            };
            // Input devices can outlive managed statics during a domain reload.
            EditorApplication.delayCall += End;
        }
        public static void Begin(bool originalBackground)
        {
            End();background=originalBackground;configured=true;UnityEngine.Application.runInBackground=true;native=Mouse.current;enabled=native!=null && native.enabled;
            SessionState.SetBool(Prefix+"configured",true);
            SessionState.SetBool(Prefix+"background",background);
            SessionState.SetInt(Prefix+"native",native!=null?native.deviceId:-1);
            SessionState.SetBool(Prefix+"enabled",enabled);
            if(enabled)InputSystem.DisableDevice(native);
            simulated=InputSystem.AddDevice<Mouse>(SimulatedName);simulated.MakeCurrent();
        }
        public static void End()
        {
            foreach(var device in InputSystem.devices.ToArray())
                if(device is Mouse && device.name==SimulatedName)InputSystem.RemoveDevice(device);
            simulated=null;
            if(!configured && SessionState.GetBool(Prefix+"configured",false))
            {
                configured=true;background=SessionState.GetBool(Prefix+"background",false);
                native=InputSystem.GetDeviceById(SessionState.GetInt(Prefix+"native",-1)) as Mouse;
                enabled=SessionState.GetBool(Prefix+"enabled",false);
            }
            if(native!=null && native.added){if(enabled)InputSystem.EnableDevice(native);native.MakeCurrent();}
            native=null;
            if(configured)UnityEngine.Application.runInBackground=background;
            configured=false;enabled=false;
            SessionState.EraseBool(Prefix+"configured");
            SessionState.EraseBool(Prefix+"background");
            SessionState.EraseInt(Prefix+"native");
            SessionState.EraseBool(Prefix+"enabled");
        }
    }
}

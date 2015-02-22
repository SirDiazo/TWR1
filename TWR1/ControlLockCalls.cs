using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace VerticalVelocity
{
    public static class ControlLockCalls
    {
        public static bool ControlLockPresent()
        {
            foreach (AssemblyLoader.LoadedAssembly Asm in AssemblyLoader.loadedAssemblies) //auto detect KAS for Skycrane
            {
                if (Asm.dllName == "ControlLock")
                {
                    //Debug.Log("Control found");
                    //AGXRemoteTechQueue.Add(new AGXRemoteTechQueueItem(group, FlightGlobals.ActiveVessel.rootPart.flightID, Planetarium.GetUniversalTime() + 10, force, forceDir));
                    return true;
                }
            }
            return false;
        }
        
        public static void SetControlLock(string lockStr)
        {
            if (ControlLockPresent())
            {
                Type calledType = Type.GetType("ControlLock.ControlLock, ControlLock");
                calledType.InvokeMember("SetFullLock", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { lockStr });
            }
        }

        public static void ReleaseControlLock(string lockStr)
        {
            if (ControlLockPresent())
            {
                Type calledType = Type.GetType("ControlLock.ControlLock, ControlLock");
                calledType.InvokeMember("UnsetFullLock", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { lockStr });
            }
        }
    }
}

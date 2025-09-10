using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.LowLevel;
namespace VoxelBusters.CoreLibrary
{
    public class ManualPlayerLoopSystem
    {
        private readonly PlayerLoopSystem m_playerLoopSystem;
        
        public ManualPlayerLoopSystem(List<object> requiredSubSystems)
        {
            m_playerLoopSystem = GetPlayerLoopSystemWithRequiredSubSystems(requiredSubSystems);
        }

        public void Process()
        {
            Debug.Log("m_playerLoopSystem.subSystemList : " + m_playerLoopSystem.subSystemList.Length);
            foreach (var eachSubSystem in m_playerLoopSystem.subSystemList)
            {
                eachSubSystem.updateDelegate.Invoke();
            }
        }

        #region Private methods

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint PlayerLoopDelegate();
        private PlayerLoopSystem GetPlayerLoopSystemWithRequiredSubSystems(List<object> requiredSubSystems)
        {
            PlayerLoopSystem system = new PlayerLoopSystem();
            List<PlayerLoopSystem> subSystems = new List<PlayerLoopSystem>();
            
            DebugLogger.Log("Getting required subsystems");
            foreach (var eachRequiredSubSystem in requiredSubSystems)
            {

                var subSystemType = eachRequiredSubSystem.GetType();
                var subSystem = GetPlayerLoopSystemForType(subSystemType);
                
                var intPtr = Marshal.ReadIntPtr(subSystem.updateFunction); //Function pointer is at subSystem.updateFunction memory location. So getting it with ReadIntPtr.
                var updateDelegate = (PlayerLoopDelegate)Marshal.GetDelegateForFunctionPointer(intPtr, typeof(PlayerLoopDelegate));
                
                subSystems.Add(new PlayerLoopSystem()
                {
                    type = subSystemType,
                    updateDelegate = () => updateDelegate.Invoke()
                });
            }

            system.subSystemList = subSystems.ToArray();

            return system;
        }

        private PlayerLoopSystem GetPlayerLoopSystemForType(Type type)
        {
            PlayerLoopSystem defaultSystem = PlayerLoop.GetDefaultPlayerLoop();

            foreach (var subSystem in defaultSystem.subSystemList)
            {
                if(subSystem.subSystemList == null)
                    continue;
                
                foreach (var innerSubSystem in subSystem.subSystemList)
                {
                    if (innerSubSystem.type == type)
                    {
                        return innerSubSystem;
                    }    
                }
            }

            return default;
        }
        
        #endregion
    }
}

//Find the calling subsystem with Debug.Log("Completed : " + UnityEngine.StackTraceUtility.ExtractStackTrace());
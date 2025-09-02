using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.CoreLibrary
{
    public class ApplicationLifecycleObserver : SingletonBehaviour<ApplicationLifecycleObserver>
    {
        [ClearOnReload(ClearOnReloadOption.Default)]
        List<IApplicationLifecycleListener> m_listeners = new List<IApplicationLifecycleListener>();

        #region Public methods

        public static ApplicationLifecycleObserver Initialize()
        {
            return GetSingleton();
        }

        public void AddListener(IApplicationLifecycleListener listener) 
        {
            m_listeners.Add(listener);
        }

        public void RemoveListener(IApplicationLifecycleListener listener) 
        {
            m_listeners.Remove(listener);
        }
        #endregion

        private void OnApplicationFocus(bool hasFocus)
        {
            foreach(var listener in m_listeners)
            {
                listener.OnApplicationFocus(hasFocus);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            foreach(var listener in m_listeners)
            {
                listener.OnApplicationPause(pauseStatus);
            }
        }

        private void OnApplicationQuit()
        {
            foreach(var listener in m_listeners)
            {
                listener.OnApplicationQuit();
            }
        }
    }
}
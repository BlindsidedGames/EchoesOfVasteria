using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

/// <summary> 
/// Namespace for essential kit features. You need to import this namespace along with <see cref="VoxelBusters.CoreLibrary"/> to use essential kit features.
/// </summary>
namespace VoxelBusters.EssentialKit
{
    public class EssentialKitManager : PrivateSingletonBehaviour<EssentialKitManager>
    {
        #region Static methods

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnLoad()
        {
#pragma warning disable 
            var     singleton   = GetSingleton();
#pragma warning restore 
        }

        #endregion

        #region Unity methods

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();

            // Create required systems
            CallbackDispatcher.Initialize();
            ApplicationLifecycleObserver.Initialize();

            // Set environment variables
            var     settings    = EssentialKitSettings.Instance;
            DebugLogger.SetLogLevel(settings.ApplicationSettings.LogLevel,
                                    CoreLibraryDomain.Default,
                                    CoreLibraryDomain.NativePlugins,
                                    EssentialKitDomain.Default);

            settings.InitialiseFeatures();
        }

        #endregion
    }
}
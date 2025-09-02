using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using VoxelBusters.CoreLibrary.Editor.NativePlugins.Build;

namespace VoxelBusters.CoreLibrary.Editor
{
    public abstract class PluginFeatureObject : ScriptableObject
    {
        #region

        [SerializeField]
        protected bool m_isEnabled = true;

        #endregion

        #region Public methods

        public void EnableFeature(BuildTarget[] allowedImplementationTargets = null)
        {
            /* allowedImplementationTargets
            * We will look into the Implementations/iOS or Android or Windows and enable those libraries alone and disable rest.
            * 
            */
            m_isEnabled = true;
            UpdateAsmdef((AssemblyDefinitionProxy proxy) => proxy.IncludeAllPlatforms());
            GenerateLinkXml();
        }

        public void DisableFeature()
        {
            m_isEnabled = false;
            UpdateAsmdef((AssemblyDefinitionProxy proxy) => proxy.ExcludeAllPlatforms());
        }

        #endregion


        #region Protected methods

        protected virtual void UpdateLinkXmlWriter(LinkXmlWriter xml) { }

        #endregion

        #region Private methods

        private AssemblyDefinitionProxy GetAsmdefProxy()
        {
            string directory = GetFeatureDirectory();
            var proxy = new AssemblyDefinitionProxy(directory);
            return proxy;
        }

        private string GetFeatureDirectory()
        {
            var path = AssetDatabase.GetAssetPath(this);
            return IOServices.GetDirectoryName(path);
        }

        private void UpdateAsmdef(Action<AssemblyDefinitionProxy> action)
        {
            var proxy = GetAsmdefProxy();
            action.Invoke(proxy);
            proxy.Save();
        }

        private void GenerateLinkXml()
        {
            string directory = GetFeatureDirectory();
            var linkXml = new LinkXmlWriter(IOServices.CombinePath(directory, "link.xml"));
            UpdateLinkXmlWriter(linkXml);
            linkXml.WriteToFile();
        }

        #endregion
    }
}
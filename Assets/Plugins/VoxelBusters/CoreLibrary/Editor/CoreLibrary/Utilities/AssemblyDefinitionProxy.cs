using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace VoxelBusters.CoreLibrary.Editor
{

    public partial class AssemblyDefinitionProxy
    {

        private AssemblyDefinitionData m_data;
        private string m_directoryPath;

        public AssemblyDefinitionProxy(string assemblyDirectoryPath)
        {
            string asmdefFile = Directory.GetFiles(assemblyDirectoryPath, "*.asmdef").FirstOrDefault();

            if (string.IsNullOrEmpty(asmdefFile))
            {
                throw new VBException($"No .asmdef file found in {assemblyDirectoryPath} directory.");
            }

            m_directoryPath = assemblyDirectoryPath;
            string contents = IOServices.ReadFile(asmdefFile);
            m_data = AssemblyDefinitionData.Load(contents);
        }

        public void IncludeAllPlatforms()
        {
            m_data.ExcludePlatforms = new string[0];
            m_data.IncludePlatforms = new string[0];
        }

        public void ExcludeAllPlatforms()
        {
            AssemblyDefinitionPlatform[] platforms = CompilationPipeline.GetAssemblyDefinitionPlatforms();
            m_data.ExcludePlatforms = platforms.Select(platform => platform.Name).ToArray();
            m_data.IncludePlatforms = new string[0];
        }


        public void Save()
        {
            IOServices.CreateFile(IOServices.CombinePath(m_directoryPath, $"{m_data.Name}.asmdef"), m_data.ToJson());
        }

    }
}
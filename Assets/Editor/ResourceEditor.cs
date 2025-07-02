using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelessEchoes.Upgrades
{
    [CustomEditor(typeof(Resource))]
    public class ResourceEditor : UnityEditor.Editor
    {
        private Resource item => target as Resource;

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (item.icon != null)
            {
                var t = GetType("UnityEditor.SpriteUtility");
                if (t != null)
                {
                    var method = t.GetMethod("RenderStaticPreview",
                        new[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });
                    if (method != null)
                    {
                        var ret = method.Invoke("RenderStaticPreview",
                            new object[] { item.icon, Color.white, width, height });
                        if (ret is Texture2D tex)
                            return tex;
                    }
                }
            }

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }

            return null;
        }
    }
}
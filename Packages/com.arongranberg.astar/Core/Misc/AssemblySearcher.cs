using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pathfinding.Util {
	internal static class AssemblySearcher {
		/// <summary>
		/// Assemblies currently loaded into this process.
		///
		/// AppDomain.GetAssemblies can return assemblies Unity has already unloaded, which then throw
		/// when reflected over. Unity 6000.6 added a filtered list; older versions have no equivalent,
		/// which is why the GetTypes call below is wrapped in a try-catch.
		/// </summary>
		static IEnumerable<Assembly> LoadedAssemblies () {
#if UNITY_6000_6_OR_NEWER
			return UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
			return System.AppDomain.CurrentDomain.GetAssemblies();
#endif
		}

		public static List<System.Type> FindTypesInheritingFrom<T>() {
			var result = new List<System.Type>();
			foreach (var assembly in LoadedAssemblies()) {
				// Skip some assemblies which are known to not contain any graph types, for performance
				var name = assembly.GetName().Name;
				if (name.StartsWith("Unity.") || name.StartsWith("UnityEngine.") || name == "UnityEngine" || name.StartsWith("UnityEditor.") || name == "UnityEditor" || name.StartsWith("Mono.") || name.StartsWith("System.") || name == "System" || name.StartsWith("mscorlib") || name.StartsWith("I18N") || name == "netstandard" || name == "nunit.framework") continue;

				System.Type[] types = null;
				try {
					types = assembly.GetTypes();
				} catch {
					// Ignore type load exceptions and things like that.
					// We might not be able to read all assemblies for some reason, but hopefully the relevant types exist in the assemblies that we can read
					continue;
				}

				foreach (var type in types) {
#if NETFX_CORE && !UNITY_EDITOR
					System.Type baseType = type.GetTypeInfo().BaseType;
#else
					var baseType = type.BaseType;
#endif
					while (baseType != null) {
						if (System.Type.Equals(baseType, typeof(T))) {
							result.Add(type);
							break;
						}

#if NETFX_CORE && !UNITY_EDITOR
						baseType = baseType.GetTypeInfo().BaseType;
#else
						baseType = baseType.BaseType;
#endif
					}
				}
			}
			return result;
		}
	}
}

using UnityEngine;
using System.Diagnostics;

namespace TimelessEchoes
{
    public static class TELogger
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, Object context = null)
        {
            if (context != null)
                Debug.Log(message, context);
            else
                Debug.Log(message);
        }
    }
}

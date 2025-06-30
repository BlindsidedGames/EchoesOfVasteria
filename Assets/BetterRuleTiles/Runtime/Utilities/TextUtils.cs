using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VinTools.Utilities
{
    public static class TextUtils
    {
        //string modification
        public static string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.IndexOf(Find);
            if (Place < 0) return Source;

            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }

    }
}
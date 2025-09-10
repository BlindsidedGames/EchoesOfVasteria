using UnityEngine;

namespace VoxelBusters.CoreLibrary
{
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public string Message
        {
            get;
            private set;
        }


        /// <summary>
        /// Marks field to be read-only in the inspector.
        /// </summary>
        public ReadOnlyAttribute(string message = null)
        { 
            Message = message;
        }
    }
}
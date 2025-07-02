using System.Collections.Generic;
using Blindsided;
using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    /// <summary>
    /// Enables or disables objects based on whether specific NPCs have been met.
    /// </summary>
    public class NpcObjectStateController : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public string npcId;
            public List<GameObject> disableUntilMet = new();
            public List<GameObject> enableUntilMet = new();
        }

        [SerializeField]
        private List<Entry> entries = new();

        private void OnEnable()
        {
            EventHandler.OnLoadData += UpdateObjectStates;
        }

        private void OnDisable()
        {
            EventHandler.OnLoadData -= UpdateObjectStates;
        }

        private void Start()
        {
            UpdateObjectStates();
        }

        /// <summary>
        /// Iterates over all entries and updates the active state of their objects.
        /// </summary>
        public void UpdateObjectStates()
        {
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                bool met = !string.IsNullOrEmpty(entry.npcId) &&
                           StaticReferences.CompletedNpcTasks.Contains(entry.npcId);
                foreach (var obj in entry.disableUntilMet)
                    if (obj != null)
                        obj.SetActive(met);
                foreach (var obj in entry.enableUntilMet)
                    if (obj != null)
                        obj.SetActive(!met);
            }
        }
    }
}

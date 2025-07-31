using System.Collections;
using System.Collections.Generic;
using References.UI;
using UnityEngine;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays resources dropped during the current run.
    /// </summary>
    public class RunDropUI : MonoBehaviour
    {
        private ResourceManager resourceManager;
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject displayObject;
        [SerializeField] [Min(1)] private int maxVisibleDrops = 5;

        private readonly List<Resource> resources = new();
        private readonly List<ResourceUIReferences> slots = new();
        private readonly Dictionary<Resource, double> amounts = new();

        /// <summary>
        ///     Current amounts collected during this run.
        /// </summary>
        public IReadOnlyDictionary<Resource, double> Amounts => amounts;


        private void Awake()
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            if (slotParent == null)
                slotParent = transform;
            if (displayObject == null)
                displayObject = gameObject;
            ClearDrops();
        }

        private void OnEnable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceAdded += OnResourceAdded;
            ClearDrops();
        }

        private void OnDisable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceAdded -= OnResourceAdded;
        }



        /// <summary>
        ///     Clears all collected resource counts.
        /// </summary>
        public void ResetDrops()
        {
            ClearDrops();
        }

        private void ClearDrops()
        {
            foreach (var slot in slots)
                if (slot != null)
                    Destroy(slot.gameObject);
            resources.Clear();
            slots.Clear();
            amounts.Clear();
            if (displayObject != null)
                displayObject.SetActive(false);
        }


        private void OnResourceAdded(Resource resource, double amount)
        {
            if (resource == null || amount <= 0) return;

            ResourceUIReferences slot = null;
            var newSlot = false;
            var index = resources.IndexOf(resource);
            var moved = index > 0;

            if (index >= 0)
            {
                amounts[resource] += amount;
                slot = slots[index];
                if (index > 0)
                {
                    resources.RemoveAt(index);
                    slots.RemoveAt(index);
                    resources.Insert(0, resource);
                    slots.Insert(0, slot);
                    slot.transform.SetSiblingIndex(0);

                }
            }
            else
            {
                double current = 0;
                if (amounts.TryGetValue(resource, out var val))
                    current = val;
                amounts[resource] = current + amount;

                if (resources.Count >= maxVisibleDrops)
                {
                    var removeIndex = resources.Count - 1;
                    resources.RemoveAt(removeIndex);
                    var removedSlot = slots[removeIndex];
                    if (removedSlot != null)
                        Destroy(removedSlot.gameObject);
                    slots.RemoveAt(removeIndex);
                }

                slot = Instantiate(slotPrefab, slotParent);
                resources.Insert(0, resource);
                slots.Insert(0, slot);
                slot.transform.SetSiblingIndex(0);
                newSlot = true;

                if (slot != null && slot.countText != null)
                    slot.countText.gameObject.SetActive(true);
            }


            if (displayObject != null)
                displayObject.SetActive(true);

            UpdateSlot(resources.IndexOf(resource));

            if (slot != null)
            {
                if (newSlot || moved)
                    StartCoroutine(SpawnFloatingTextNextFrame(resource, slot, amount));
                else
                    FloatingText.Spawn(
                        $"{Blindsided.Utilities.TextStrings.ResourceIcon(resource.resourceID)} {Mathf.FloorToInt((float)amount)}",
                        slot.transform.position + Vector3.up,
                        Color.white, 8f, transform);
            }

        }

        private void UpdateSlot(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            var slot = slots[index];
            var resource = resources[index];
            if (slot == null) return;

            if (slot.iconImage)
            {
                slot.iconImage.sprite = resource ? resource.icon : null;
                slot.iconImage.enabled = true;
            }

            if (slot.countText)
            {
                var count = amounts.TryGetValue(resource, out var val) ? val : 0;
                slot.countText.text = FormatNumber(count, true);
                slot.countText.gameObject.SetActive(true);
            }
        }

        private IEnumerator SpawnFloatingTextNextFrame(Resource resource, ResourceUIReferences slot, double amount)
        {
            yield return null; // wait one frame for layout groups to update
            if (slot != null)
                FloatingText.Spawn(
                    $"{Blindsided.Utilities.TextStrings.ResourceIcon(resource.resourceID)} {Mathf.FloorToInt((float)amount)}",
                    slot.transform.position + Vector3.up,
                    Color.white, 8f, transform);
        }

    }
}
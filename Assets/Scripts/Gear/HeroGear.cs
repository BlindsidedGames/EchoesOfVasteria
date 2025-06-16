using System;
using Blindsided.SaveData;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;

public class HeroGear : MonoBehaviour
{
    public GearItem ring;
    public GearItem necklace;
    public GearItem brooch;
    public GearItem pocket;

    public event Action GearChanged;

    public int TotalDamage => (ring?.damage ?? 0) + (necklace?.damage ?? 0) + (brooch?.damage ?? 0) + (pocket?.damage ?? 0);
    public float TotalAttackSpeed => (ring?.attackSpeed ?? 0) + (necklace?.attackSpeed ?? 0) + (brooch?.attackSpeed ?? 0) + (pocket?.attackSpeed ?? 0);
    public int TotalHealth => (ring?.health ?? 0) + (necklace?.health ?? 0) + (brooch?.health ?? 0) + (pocket?.health ?? 0);
    public int TotalDefense => (ring?.defense ?? 0) + (necklace?.defense ?? 0) + (brooch?.defense ?? 0) + (pocket?.defense ?? 0);
    public float TotalMoveSpeed => (ring?.moveSpeed ?? 0) + (necklace?.moveSpeed ?? 0) + (brooch?.moveSpeed ?? 0) + (pocket?.moveSpeed ?? 0);

    private void Awake()
    {
        LoadState();
        EventHandler.OnSaveData += SaveState;
        EventHandler.OnLoadData += LoadState;
    }

    private void OnDestroy()
    {
        EventHandler.OnSaveData -= SaveState;
        EventHandler.OnLoadData -= LoadState;
    }

    public void Equip(GearItem item)
    {
        switch (item.slot)
        {
            case GearSlot.Ring: ring = item; break;
            case GearSlot.Necklace: necklace = item; break;
            case GearSlot.Brooch: brooch = item; break;
            case GearSlot.Pocket: pocket = item; break;
        }
        GearChanged?.Invoke();
    }

    private void SaveState()
    {
        if (oracle == null) return;
        var dict = oracle.saveData.HeroGear;
        if (dict == null)
            oracle.saveData.HeroGear = dict = new System.Collections.Generic.Dictionary<string, SaveData.HeroGearState>();
        if (!dict.TryGetValue(gameObject.name, out var state))
            dict[gameObject.name] = state = new SaveData.HeroGearState();
        state.Ring = ring;
        state.Necklace = necklace;
        state.Brooch = brooch;
        state.Pocket = pocket;
    }

    private void LoadState()
    {
        if (oracle == null) return;
        oracle.saveData.HeroGear ??= new System.Collections.Generic.Dictionary<string, SaveData.HeroGearState>();
        if (oracle.saveData.HeroGear.TryGetValue(gameObject.name, out var state))
        {
            ring = state.Ring;
            necklace = state.Necklace;
            brooch = state.Brooch;
            pocket = state.Pocket;
        }
        else
        {
            oracle.saveData.HeroGear[gameObject.name] = new SaveData.HeroGearState();
        }
        GearChanged?.Invoke();
    }
}

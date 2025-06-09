using System;
using UnityEngine;

public class HeroStance : MonoBehaviour
{
    public enum Stance
    {
        Move,
        Hold
    }

    [field: SerializeField] public Stance CurrentStance { get; private set; } = Stance.Move;

    public event Action<Stance> OnStanceChanged;

    /// <summary>Toggle Move ↔ Hold.</summary>
    public void Cycle()
    {
        CurrentStance = CurrentStance == Stance.Move ? Stance.Hold : Stance.Move;
        OnStanceChanged?.Invoke(CurrentStance);
    }
}
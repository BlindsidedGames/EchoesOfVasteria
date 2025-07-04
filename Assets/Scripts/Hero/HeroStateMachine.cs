using UnityEngine;

namespace TimelessEchoes.Hero
{
    public enum HeroState
    {
        Idle,
        Moving,
        PerformingTask,
        Combat
    }

    public class HeroStateMachine : MonoBehaviour
    {
        [SerializeField] private HeroState currentState = HeroState.Idle;
        public HeroState CurrentState => currentState;

        public void ChangeState(HeroState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
        }
    }
}

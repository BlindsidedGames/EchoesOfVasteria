using UnityEngine;

namespace Gear
{
    [RequireComponent(typeof(Health))]
    public class EnemyDropper : MonoBehaviour
    {
        private EnemyBalanceData balance;

        private void Awake()
        {
            var holder = GetComponent<EnemyBalanceHolder>();
            balance = holder ? holder.Balance : null;
            GetComponent<Health>().OnDeath += SpawnDrop;
        }

        private void SpawnDrop()
        {
            if (balance == null) return;
            if (Random.value > balance.gearDropRate) return;
            var gear = GearGenerator.Generate(balance.enemyLevel);
            var go = new GameObject("GearDrop");
            go.transform.position = transform.position;
            var drop = go.AddComponent<GearDrop>();
            drop.Init(gear);
        }
    }
}
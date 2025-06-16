using UnityEngine;

namespace Gear
{
    [RequireComponent(typeof(Health))]
    public class EnemyDropper : MonoBehaviour
    {
        [SerializeField] private GearDrop dropPrefab;
        private EnemyBalanceData balance;

        private void Awake()
        {
            var holder = GetComponent<BalanceHolder>();
            balance = holder ? holder.Balance as EnemyBalanceData : null;
            GetComponent<Health>().OnDeath += SpawnDrop;
        }

        private void SpawnDrop()
        {
            if (balance == null) return;
            if (Random.value > balance.gearDropRate) return;
            var gear = GearGenerator.Generate(balance.enemyLevel);
            if (dropPrefab)
            {
                var drop = Instantiate(dropPrefab, transform.position, Quaternion.identity);
                if (drop)
                {
                    drop.Init(gear);
                }
                else
                {
                    Debug.LogWarning("EnemyDropper: dropPrefab is missing GearDrop component", this);
                }
            }
            else
            {
                var go = new GameObject("GearDrop");
                go.transform.position = transform.position;
                var drop = go.AddComponent<GearDrop>();
                drop.Init(gear);
            }
        }
    }
}
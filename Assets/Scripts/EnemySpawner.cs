using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")] [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField] private int maxEnemies = 3;

    [Header("Spawn Area")] [SerializeField]
    private Vector2 spawnAreaSize = new(20, 20);

    [Header("Respawn")] [SerializeField] private float respawnTime = 15f;

    private readonly List<GameObject> spawnedEnemies = new();

    private void Start()
    {
        // Spawn the initial wave of enemies.
        for (var i = 0; i < maxEnemies; i++) SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (spawnedEnemies.Count >= maxEnemies || enemyPrefab == null) return;

        // Find a random position within the spawn area.
        var spawnPos = (Vector2)transform.position + new Vector2(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
        );

        var enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        spawnedEnemies.Add(enemy);

        // Tell the new enemy where its home is for leashing.
        if (enemy.TryGetComponent(out EnemyAI enemyAI)) enemyAI.SetSpawnAnchor(transform.position);

        // Subscribe to the enemy's death event so we can respawn it.
        if (enemy.TryGetComponent(out Health enemyHealth)) enemyHealth.OnDeath += () => HandleEnemyDeath(enemy);
    }

    private void HandleEnemyDeath(GameObject enemy)
    {
        // Remove the dead enemy from our list.
        spawnedEnemies.Remove(enemy);

        // Destroy the dead enemy's GameObject.
        if (enemy != null) Destroy(enemy);

        // Start a timer to respawn a new one.
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnTime);
        SpawnEnemy();
    }

#if UNITY_EDITOR
    // Draw a box in the editor to visualize the spawn area.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f); // Green, semi-transparent
        Gizmos.DrawCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1));
    }
#endif
}
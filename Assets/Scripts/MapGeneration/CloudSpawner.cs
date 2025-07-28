using System.Collections;
using UnityEngine;

// Cloud parallax manager
// Execution order is set so this runs before most scripts
[DefaultExecutionOrder(-1)]
public class CloudSpawner : MonoBehaviour
{
    public static CloudSpawner Instance { get; private set; }
    [Header("Setup")] [SerializeField] private Sprite[] frames; // 4 cloud images
    [SerializeField] private int runCloudCount = 3; // number of clouds during runs
    private int TownCloudCount => runCloudCount * 2;

    [Header("Parallax")] [SerializeField] private float baseSpeed = 0.2f; // world units / sec
    [SerializeField] private float speedVariance = 0.1f; // ± extra random

    [Header("Recycle Distances")] [SerializeField]
    private float aheadDistance = 18f; // recycle if too far in front of camera

    [SerializeField] [Min(0f)] private float recycleSpawnDistance = 6f; // how far ahead to place recycled clouds

    [SerializeField] private float behindDistance = 2f; // recycle if too far behind the camera

    private Camera cam;
    private float screenHalfWidth;
    private float screenHalfHeight;
    private Cloud[] clouds;
    private Coroutine resetRoutine;
    private bool allowClouds = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        var maxSpawn = aheadDistance - 1f;
        if (recycleSpawnDistance > maxSpawn)
            recycleSpawnDistance = maxSpawn;
    }
#endif

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        UpdateScreenDimensions();
        var maxCount = Mathf.Max(runCloudCount, TownCloudCount);
        clouds = new Cloud[maxCount];

        for (var i = 0; i < maxCount; i++)
            clouds[i] = Spawn(true);
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
        if (clouds != null)
            ResetClouds(true); // game starts in town
    }

    private void Update()
    {
        foreach (var c in clouds)
        {
            if (!c.Tr.gameObject.activeInHierarchy)
                continue;

            // move cloud
            c.Tr.position += Vector3.left * c.Speed * Time.deltaTime;

            var leftEdge = cam.transform.position.x - (screenHalfWidth + behindDistance);
            var rightEdge = cam.transform.position.x + screenHalfWidth + aheadDistance;

            // recycle if outside camera bounds
            if (c.Tr.position.x < leftEdge || c.Tr.position.x > rightEdge)
                Recycle(c);
        }
    }

    private Cloud Spawn(bool spawnInView = false)
    {
        var go = new GameObject("Cloud", typeof(SpriteRenderer));
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = frames[Random.Range(0, frames.Length)];
        sr.sortingLayerName = "Foreground";
        sr.material.enableInstancing = true;

        var cloud = new Cloud { Tr = go.transform };
        Recycle(cloud, spawnInView);
        return cloud;
    }

    private void Recycle(Cloud c, bool spawnInView = false)
    {
        UpdateScreenDimensions();

        float x;
        if (spawnInView)
        {
            x = cam.transform.position.x + Random.Range(-screenHalfWidth, screenHalfWidth);
        }
        else
        {
            var maxSpawn = aheadDistance - 1f;
            var offset = Mathf.Min(recycleSpawnDistance, maxSpawn);
            x = cam.transform.position.x + screenHalfWidth + offset;
        }

        var y = cam.transform.position.y + Random.Range(-screenHalfHeight, screenHalfHeight);
        c.Tr.position = new Vector3(x, y, 0f);
        c.Speed = baseSpeed + Random.Range(-speedVariance, speedVariance);
        // pick a new frame / scale for variety
        c.Tr.GetComponent<SpriteRenderer>().sprite = frames[Random.Range(0, frames.Length)];
        c.Tr.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
    }

    private void UpdateScreenDimensions()
    {
        // Refresh the camera reference in case a different camera became active.
        var currentMain = Camera.main;
        if (currentMain != null)
            cam = currentMain;

        if (cam == null)
            return;

        screenHalfHeight = cam.orthographicSize;
        screenHalfWidth = screenHalfHeight * cam.aspect;
    }

    public void SetAllowClouds(bool allow)
    {
        allowClouds = allow;
        if (!allowClouds && clouds != null)
        {
            foreach (var c in clouds)
                if (c?.Tr != null)
                    c.Tr.gameObject.SetActive(false);
        }
    }

    public void ResetClouds(bool inTown)
    {
        if (resetRoutine != null)
            StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(ResetCloudsRoutine(inTown));
    }

    private IEnumerator ResetCloudsRoutine(bool inTown)
    {
        // Wait until Cinemachine has updated the camera position.
        yield return new WaitForEndOfFrame();

        UpdateScreenDimensions();
        if (clouds == null)
            yield break;

        if (!allowClouds)
        {
            foreach (var c in clouds)
                c.Tr.gameObject.SetActive(false);
            resetRoutine = null;
            yield break;
        }

        var activeCount = inTown ? TownCloudCount : runCloudCount;

        for (var i = 0; i < clouds.Length; i++)
        {
            var active = i < activeCount;
            var go = clouds[i].Tr.gameObject;
            go.SetActive(active);
            if (active)
                Recycle(clouds[i], true);
        }

        resetRoutine = null;
    }

    private class Cloud
    {
        public Transform Tr;
        public float Speed;
    }
}
using System.Collections;
using UnityEngine;
using TimelessEchoes.Utilities;

// Cloud parallax manager
// Execution order is set so this runs before most scripts
[DefaultExecutionOrder(-1)]
public class CloudSpawner : Singleton<CloudSpawner>
{
    
    [Header("Setup")] [SerializeField] private Sprite[] frames; // 4 cloud images
    [SerializeField] private int runCloudCount = 3; // number of clouds during runs
    [Header("Town weather")]
    [SerializeField, Min(64f)] private float townAreaPerCloud = 320f;
    [SerializeField, Min(2f)] private float townEdgePadding = 8f;
    private Bounds townBounds;
    private bool townMode = true;
    private int townCloudCount;

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

    protected override void Awake()
    {
        base.Awake();
        cam = Camera.main;
        UpdateScreenDimensions();
        // Camera bounds describe the mainland, excluding river overshoot and props.
        var land = GameObject.Find("Hometown/Town Camera Land Bounds")?.GetComponent<BoxCollider2D>();
        townBounds = land != null ? land.bounds : new Bounds(cam != null ? cam.transform.position : transform.position,
            new Vector3(screenHalfWidth * 2f, screenHalfHeight * 2f, 0));
        townBounds.Expand(new Vector3(townEdgePadding * 2, townEdgePadding * 2, 0));
        townCloudCount = Mathf.Clamp(Mathf.CeilToInt(townBounds.size.x * townBounds.size.y / townAreaPerCloud), 1, 128);
        var maxCount = Mathf.Max(runCloudCount, townCloudCount);
        clouds = new Cloud[maxCount];

        for (var i = 0; i < maxCount; i++)
            clouds[i] = Spawn(true);
    }

    private void OnEnable()
    {
        if (clouds != null)
            ResetClouds(true); // game starts in town
    }

    private void Update()
    {
        if (clouds == null || cam == null) return;
        if (!townMode) UpdateScreenDimensions();
        foreach (var c in clouds)
        {
            if (!c.Tr.gameObject.activeInHierarchy)
                continue;

            // move cloud
            c.Tr.position += (townMode ? new Vector3(-1f, .08f, 0) : Vector3.left) * c.Speed * Time.deltaTime;

            if (townMode)
            {
                // Wrap only beyond the padded map, never at a panning camera edge.
                if (c.Renderer.bounds.max.x < townBounds.min.x)
                {
                    Recycle(c, true);
                    c.Tr.position = new Vector3(townBounds.max.x + c.Renderer.bounds.extents.x,
                        Random.Range(townBounds.min.y, townBounds.max.y), 0);
                }
                else if (c.Renderer.bounds.min.y > townBounds.max.y)
                    c.Tr.position = new Vector3(c.Tr.position.x, townBounds.min.y - c.Renderer.bounds.extents.y, 0);
                continue;
            }

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
        go.transform.SetParent(transform, true);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = frames[Random.Range(0, frames.Length)];
        sr.sortingLayerName = "Foreground";
        var cloud = new Cloud { Tr = go.transform, Renderer = sr };
        Recycle(cloud, spawnInView);
        return cloud;
    }

    private void Recycle(Cloud c, bool spawnInView = false)
    {
        UpdateScreenDimensions();

        if (townMode)
        {
            c.Renderer.sprite = frames[Random.Range(0, frames.Length)];
            c.Tr.localScale = Vector3.one * Random.Range(.85f, 1.35f);
            c.Renderer.color = new Color(1, 1, 1, Random.Range(.65f, .95f));
            c.Speed = Mathf.Max(.02f, baseSpeed * Random.Range(.8f, 1.2f));
            c.Tr.position = new Vector3(Random.Range(townBounds.min.x, townBounds.max.x),
                Random.Range(townBounds.min.y, townBounds.max.y), 0);
            return;
        }

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
        c.Renderer.sprite = frames[Random.Range(0, frames.Length)];
        c.Renderer.color = Color.white;
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
        townMode = inTown;
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

        var activeCount = inTown ? townCloudCount : runCloudCount;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(activeCount * townBounds.size.x / townBounds.size.y));
        int rows = Mathf.CeilToInt((float)activeCount / columns);

        for (var i = 0; i < clouds.Length; i++)
        {
            var active = i < activeCount;
            var go = clouds[i].Tr.gameObject;
            go.SetActive(active);
            if (active)
            {
                Recycle(clouds[i], true);
                if (inTown)
                {
                    // Jittered cells prevent both empty regions and clumps at startup.
                    clouds[i].Tr.position = new Vector3(
                        townBounds.min.x + (i % columns + Random.Range(.1f, .9f)) * townBounds.size.x / columns,
                        townBounds.min.y + (i / columns + Random.Range(.1f, .9f)) * townBounds.size.y / rows, 0);
                }
            }
        }

        resetRoutine = null;
    }

    private class Cloud
    {
        public Transform Tr;
        public SpriteRenderer Renderer;
        public float Speed;
    }
}

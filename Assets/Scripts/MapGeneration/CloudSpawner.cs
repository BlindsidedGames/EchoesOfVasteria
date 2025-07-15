using UnityEngine;

// Cloud parallax manager
// Execution order is set so this runs before most scripts
[DefaultExecutionOrder(-1)]
public class CloudSpawner : MonoBehaviour
{
    public static CloudSpawner Instance { get; private set; }
    [Header("Setup")] [SerializeField] private Sprite[] frames; // 4 cloud images
    [SerializeField] private int cloudCount = 3; // keep it small

    [Header("Parallax")] [SerializeField] private float baseSpeed = 0.2f; // world units / sec
    [SerializeField] private float speedVariance = 0.1f; // ± extra random

    [Header("Recycle Distances")] [SerializeField]
    private float aheadDistance = 18f; // recycle if too far in front of camera

    [SerializeField]
    private float behindDistance = 2f; // recycle if too far behind the camera

    private Camera cam;
    private float screenHalfWidth;
    private float screenHalfHeight;
    private Cloud[] clouds;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        UpdateScreenDimensions();
        clouds = new Cloud[cloudCount];

        for (var i = 0; i < cloudCount; i++)
            clouds[i] = Spawn(true);
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
        if (clouds != null)
            ResetClouds();
    }

    private void Update()
    {
        foreach (var c in clouds)
        {
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
        sr.sortingLayerName = "Background";
        sr.material.enableInstancing = true;

        var cloud = new Cloud { Tr = go.transform };
        Recycle(cloud, spawnInView);
        return cloud;
    }

    private void Recycle(Cloud c, bool spawnInView = false)
    {
        UpdateScreenDimensions();

        var x = cam.transform.position.x + Random.Range(-screenHalfWidth, screenHalfWidth);

        var y = cam.transform.position.y + Random.Range(-screenHalfHeight, screenHalfHeight);
        c.Tr.position = new Vector3(x, y, 0f);
        c.Speed = baseSpeed + Random.Range(-speedVariance, speedVariance);
        // pick a new frame / scale for variety
        c.Tr.GetComponent<SpriteRenderer>().sprite = frames[Random.Range(0, frames.Length)];
        c.Tr.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
    }

    private void UpdateScreenDimensions()
    {
        if (cam == null)
            return;

        screenHalfHeight = cam.orthographicSize;
        screenHalfWidth = screenHalfHeight * cam.aspect;
    }

    public void ResetClouds()
    {
        UpdateScreenDimensions();
        if (clouds == null)
            return;

        foreach (var c in clouds)
            Recycle(c, true);
    }

    private class Cloud
    {
        public Transform Tr;
        public float Speed;
    }
}
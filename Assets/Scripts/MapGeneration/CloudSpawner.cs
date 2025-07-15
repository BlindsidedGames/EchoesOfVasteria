using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Setup")] [SerializeField] private Sprite[] frames; // 4 cloud images
    [SerializeField] private int cloudCount = 3; // keep it small
    [SerializeField] private Vector2 heightRange = new(2f, 6f);

    [Header("Parallax")] [SerializeField] private float baseSpeed = 0.2f; // world units / sec
    [SerializeField] private float speedVariance = 0.1f; // ± extra random

    [Header("Spawn Ahead/Behind")] [SerializeField]
    private float aheadDistance = 18f; // how far in front of cam to (re)spawn

    [SerializeField]
    private float aheadVariance = 4f; // random range around aheadDistance

    [SerializeField] private float behindDistance = 12f; // despawn once this far behind

    private Camera cam;
    private float screenHalfWidth;
    private Cloud[] clouds;

    private void Awake()
    {
        cam = Camera.main;
        screenHalfWidth = cam.orthographicSize * cam.aspect;
        clouds = new Cloud[cloudCount];

        for (var i = 0; i < cloudCount; i++)
            clouds[i] = Spawn(true);
    }

    private void Update()
    {
        foreach (var c in clouds)
        {
            // move cloud
            c.Tr.position += Vector3.left * c.Speed * Time.deltaTime;

            // if it’s left of the camera by > behindDistance, recycle in front
            if (c.Tr.position.x < cam.transform.position.x - behindDistance)
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
        float x;
        if (spawnInView)
        {
            x = cam.transform.position.x + Random.Range(-screenHalfWidth, screenHalfWidth);
        }
        else
        {
            x = cam.transform.position.x + Random.Range(aheadDistance - aheadVariance, aheadDistance + aheadVariance);
        }

        var y = Random.Range(heightRange.x, heightRange.y);
        c.Tr.position = new Vector3(x, y, 0f);
        c.Speed = baseSpeed + Random.Range(-speedVariance, speedVariance);
        // pick a new frame / scale for variety
        c.Tr.GetComponent<SpriteRenderer>().sprite = frames[Random.Range(0, frames.Length)];
        c.Tr.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
    }

    private class Cloud
    {
        public Transform Tr;
        public float Speed;
    }
}
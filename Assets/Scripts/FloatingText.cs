using TMPro;
using UnityEngine;
using Blindsided.SaveData;
using Blindsided.Utilities.Pooling;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple floating text that rises and fades out.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public static readonly Color DefaultColor = new Color32(0xEA, 0xD4, 0xAA, 0xFF);
        private static GameObject prefab = null;
        [SerializeField] private float moveDistance = 1f;
        [SerializeField] private float lifetime = 1f;
        private float speed;
        private TMP_Text tmp;
        private float timer;

        /// <summary>
        /// Spawns a floating text object displaying the given string.
        /// </summary>
        public static void Spawn(string text, Vector3 position, Color color, float fontSize = 8f, Transform parent = null, float duration = -1f)
        {
            if (prefab == null)
            {
                prefab = new GameObject("FloatingText");
                prefab.SetActive(false);
                var ft = prefab.AddComponent<FloatingText>();
                ft.tmp = prefab.AddComponent<TextMeshPro>();
                ft.tmp.alignment = TextAlignmentOptions.Center;
                ft.tmp.fontSize = fontSize;
                ft.tmp.fontStyle |= FontStyles.SmallCaps;
                ft.tmp.spriteAsset = TimelessEchoes.Upgrades.ResourceIconLookup.SpriteAsset;

                var renderer = ft.tmp.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sortingLayerName = "Canvas";
                    renderer.sortingOrder = 10;
                }

                PoolManager.CreatePool(prefab, 1);
            }

            var obj = PoolManager.Get(prefab);
            var offset = Random.insideUnitCircle * 0.25f;
            obj.transform.position = position + new Vector3(offset.x, offset.y, 0f);
            obj.transform.SetParent(parent, true);

            var ftInstance = obj.GetComponent<FloatingText>();
            ftInstance.lifetime = duration >= 0f ? duration : StaticReferences.DropFloatingTextDuration;
            ftInstance.tmp.fontSize = fontSize;
            ftInstance.tmp.text = text;
            ftInstance.tmp.color = color;
            ftInstance.speed = ftInstance.moveDistance / Mathf.Max(ftInstance.lifetime, 0.0001f);
            ftInstance.timer = 0f;
        }

        private void Awake()
        {
            if (tmp == null)
                tmp = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            transform.position += Vector3.up * speed * Time.deltaTime;
            timer += Time.deltaTime;
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = Mathf.Lerp(1f, 0.5f, timer / lifetime);
                tmp.color = c;
            }
            if (timer >= lifetime)
                PoolManager.Release(gameObject);
        }

        private void OnDisable()
        {
            timer = 0f;
            speed = 0f;
            transform.SetParent(null);
        }
    }
}

using TMPro;
using UnityEngine;
using Blindsided.SaveData;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple floating text that rises and fades out.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public static readonly Color DefaultColor = new Color32(0xEA, 0xD4, 0xAA, 0xFF);
        [SerializeField] private float speed = 1f;
        [SerializeField] private float lifetime = 1f;
        private TMP_Text tmp;
        private float timer;

        /// <summary>
        /// Spawns a floating text object displaying the given string.
        /// </summary>
        public static void Spawn(string text, Vector3 position, Color color, float fontSize = 8f, Transform parent = null, float duration = -1f)
        {
            var obj = new GameObject("FloatingText");
            var offset = Random.insideUnitCircle * 0.25f;
            obj.transform.position = position + new Vector3(offset.x, offset.y, 0f);
            if (parent != null)
                obj.transform.SetParent(parent, true);
            var ft = obj.AddComponent<FloatingText>();
            ft.lifetime = duration >= 0f ? duration : StaticReferences.DropFloatingTextDuration;
            ft.tmp = obj.AddComponent<TextMeshPro>();
            ft.tmp.alignment = TextAlignmentOptions.Center;
            ft.tmp.fontSize = fontSize;
            ft.tmp.fontStyle |= FontStyles.SmallCaps;
            ft.tmp.spriteAsset = TimelessEchoes.Upgrades.ResourceIconLookup.SpriteAsset;
            ft.tmp.text = text;
            ft.tmp.color = color;

            // Ensure the floating text renders in front of other objects.
            var renderer = ft.tmp.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Canvas";
                renderer.sortingOrder = 10;
            }
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
                Destroy(gameObject);
        }
    }
}

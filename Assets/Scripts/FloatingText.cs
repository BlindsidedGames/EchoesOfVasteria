using TMPro;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple floating text that rises and fades out.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float speed = 1f;
        [SerializeField] private float lifetime = 1f;
        private TMP_Text tmp;
        private float timer;

        /// <summary>
        /// Spawns a floating text object displaying the given string.
        /// </summary>
        public static void Spawn(string text, Vector3 position, Color color)
        {
            var obj = new GameObject("FloatingText");
            obj.transform.position = position;
            var ft = obj.AddComponent<FloatingText>();
            ft.tmp = obj.AddComponent<TextMeshPro>();
            ft.tmp.alignment = TextAlignmentOptions.Center;
            ft.tmp.fontSize = 4f;
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
                c.a = Mathf.Lerp(1f, 0f, timer / lifetime);
                tmp.color = c;
            }
            if (timer >= lifetime)
                Destroy(gameObject);
        }
    }
}

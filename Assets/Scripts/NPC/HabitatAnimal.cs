using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes.NPC
{
    /// <summary>Small decorative routes authored in the parent's local space.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HabitatAnimal : MonoBehaviour
    {
        public enum Habitat { Land, Water, Air }
        [SerializeField] private Habitat habitat;
        [SerializeField] private Vector2[] waypoints;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] moveFrames;
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap cliffs;
        [SerializeField] private float speed = .4f;
        [SerializeField] private float framesPerSecond = 6;
        [SerializeField, Range(.1f, 1f)] private float idleAnimationSpeed = .6f;
        [SerializeField] private Vector2 pauseSeconds = new(2, 5);
        [SerializeField] private float clearance = .35f;
        [SerializeField] private bool artworkFacesLeft;
        private SpriteRenderer view;
        private int destination;
        private float pause, phase, pace;
        private float animationPace, boutPace;
        private bool wasAnimatingMove;

        private void Awake() => view = GetComponent<SpriteRenderer>();
        private void OnEnable()
        {
            destination = waypoints != null && waypoints.Length > 1 ? 1 : 0;
            pause = Random.Range(pauseSeconds.x, pauseSeconds.y);
            phase = Random.Range(0f, 10f);
            pace = Random.Range(.85f, 1.15f);
            animationPace = Random.Range(.85f, 1.15f);
            boutPace = Random.Range(.9f, 1.1f);
            wasAnimatingMove = habitat == Habitat.Air;
        }

        public bool IsSafe(Vector3 world)
        {
            if (habitat == Habitat.Air) return true;
            if (ground == null) return false;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    var p = world + new Vector3(x * clearance, y * clearance);
                    var sprite = ground.GetSprite(ground.WorldToCell(p));
                    if (sprite == null) return false;
                    bool water = sprite.name.StartsWith("Water_Middle");
                    if (habitat == Habitat.Water ? !water : water || !sprite.name.StartsWith("Grass_")) return false;
                    if (cliffs != null && cliffs.HasTile(cliffs.WorldToCell(p))) return false;
                }
            return true;
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            bool moving = false;
            if (pause > 0) pause -= Time.deltaTime;
            else
            {
                var target = transform.parent.TransformPoint(waypoints[destination]);
                var delta = target - transform.position;
                var next = Vector3.MoveTowards(transform.position, target, speed * pace * Time.deltaTime);
                // Sample the complete step too: a long frame must not jump a bank.
                bool safe = true;
                int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(transform.position, next) / .15f));
                for (int i = 1; i <= samples && safe; i++) safe = IsSafe(Vector3.Lerp(transform.position, next, (float)i / samples));
                if (safe)
                {
                    moving = delta.sqrMagnitude > .0001f;
                    transform.position = next;
                    if (Mathf.Abs(delta.x) > .02f) view.flipX = (delta.x < 0) != artworkFacesLeft;
                }
                if (!safe || Vector3.Distance(next, target) < .02f)
                {
                    destination = (destination + 1) % waypoints.Length;
                    pause = Random.Range(pauseSeconds.x, pauseSeconds.y);
                }
            }
            bool animatingMove = moving || habitat == Habitat.Air;
            if (animatingMove != wasAnimatingMove)
            {
                // Start each action at its first frame, with a fresh, subtle tempo variation.
                phase = 0;
                boutPace = Random.Range(.9f, 1.1f);
                wasAnimatingMove = animatingMove;
            }
            phase += Time.deltaTime * framesPerSecond * animationPace * boutPace
                * (animatingMove ? 1 : idleAnimationSpeed);
            var frames = animatingMove ? moveFrames : idleFrames;
            if (frames != null && frames.Length > 0) view.sprite = frames[(int)phase % frames.Length];
            // Flight is above the entire vegetation band, not just above the insect's feet.
            view.sortingOrder = habitat == Habitat.Air ? 2000 : Mathf.RoundToInt(-view.bounds.min.y * 16);
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || transform.parent == null) return;
            Gizmos.color = habitat == Habitat.Water ? Color.cyan : Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var p = transform.parent.TransformPoint(waypoints[i]);
                Gizmos.DrawWireSphere(p, clearance);
                Gizmos.DrawLine(p, transform.parent.TransformPoint(waypoints[(i + 1) % waypoints.Length]));
            }
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;

namespace TimelessEchoes.Characters
{
    // The original Animator continues to own timing and gameplay events. Its sprite is a lookup key
    // into editor-baked maps, shared by all actors; only the visual child is replaced.
    [DefaultExecutionOrder(1000)]
    public sealed class CharacterAppearance : MonoBehaviour
    {
        private SpriteRenderer source;
        private SpriteRenderer[] layers;
        private SortingGroup group;
        private CharacterCatalog catalog;
        private CharacterProfile profile;
        private int lastFrame = -1;
        private SpriteRenderer composite;
        private MaterialPropertyBlock compositeProperties;
        private static Sprite compositeQuad;
        private bool wasComposited;
        private static readonly int[] TextureIds = new int[8], RectIds = new int[8];
        private static readonly int ToolBehindId = Shader.PropertyToID("_ToolBehind");
        static CharacterAppearance() {
            for (var i = 0; i < 8; i++) { TextureIds[i] = Shader.PropertyToID("_Layer" + i); RectIds[i] = Shader.PropertyToID("_LayerRect" + i); }
        }
        public CharacterProfile Profile => profile;
        public static CharacterAppearance Attach(SpriteRenderer renderer)
        {
            if (!renderer) return null;
            var view = renderer.GetComponent<CharacterAppearance>() ?? renderer.gameObject.AddComponent<CharacterAppearance>();
            view.source = renderer;
            return view;
        }
        private void OnEnable() { lastFrame = -1; }
        private void LateUpdate()
        {
            if (!source) source = GetComponent<SpriteRenderer>();
            var current = CharacterEquipmentAppearance.Resolve(CharacterCreation.Current);
            var active = current is { Created: true, Modular: true } && source && source.enabled && (catalog || (catalog = CharacterCatalog.Load()));
            if (source) source.forceRenderingOff = active;
            if (group) group.gameObject.SetActive(active);
            if (!active) { ClearAppearanceReferences(); return; }
            if (!group) Build();
            var changed = profile != current;
            profile = current;
            catalog.TrySource(source.sprite, out var frame);
            var toolSprite = CharacterSkillTools.SpriteFor(catalog, profile, frame);
            changed |= layers[(int)CharacterSlot.Tool].sprite != toolSprite;
            group.sortingLayerID = source.sortingLayerID; group.sortingOrder = source.sortingOrder;
            var composited = source.color.a < .999f && BuildComposite();
            for (var i = 0; i < layers.Length; i++)
            {
                var renderer = layers[i];
                renderer.flipX = source.flipX; renderer.flipY = source.flipY;
                renderer.color = source.color;
                if (changed || lastFrame != frame) renderer.sprite = i == (int)CharacterSlot.Tool ? toolSprite : catalog.SpriteFor(profile, (CharacterSlot)i, frame);
                renderer.enabled = renderer.sprite && !composited;
            }
            layers[(int)CharacterSlot.Tool].sortingOrder = frame < catalog.toolBehind.Length && catalog.toolBehind[frame] ? -5 : 45;
            if (composite) {
                composite.enabled = composited;
                if (composited) {
                    composite.color = source.color; composite.flipX = source.flipX; composite.flipY = source.flipY;
                    if (changed || lastFrame != frame || !wasComposited) {
                        for (var i = 0; i < layers.Length; i++) {
                            var sprite = layers[i].sprite;
                            compositeProperties.SetTexture(TextureIds[i], sprite ? sprite.texture : Texture2D.blackTexture);
                            // These sheets use tight meshes inside a shared 64px canvas. textureRect
                            // describes the trimmed mesh, not the authored layer alignment.
                            var rect = sprite ? sprite.rect : Rect.zero;
                            compositeProperties.SetVector(RectIds[i], sprite ? new Vector4(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
                                rect.width / sprite.texture.width, rect.height / sprite.texture.height) : Vector4.zero);
                        }
                        compositeProperties.SetFloat(ToolBehindId, layers[(int)CharacterSlot.Tool].sortingOrder < 0 ? 1 : 0);
                        composite.SetPropertyBlock(compositeProperties);
                    }
                }
            }
            wasComposited = composited;
            lastFrame = frame;
            if (changed) catalog.TrimAppearanceCache(profile, CharacterCreation.Current);
        }
        private bool BuildComposite()
        {
            if (composite) return true;
            var material = Resources.Load<Material>("Characters/Composite"); if (!material) return false;
            var body = catalog.SpriteFor(profile, CharacterSlot.Body, 0);
            if (!body) return false;
            if (!compositeQuad) {
                var texture = Texture2D.whiteTexture;
                compositeQuad = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f),
                    texture.width * body.pixelsPerUnit / body.rect.width, 0, SpriteMeshType.FullRect);
                compositeQuad.name = "Character composite quad"; compositeQuad.hideFlags = HideFlags.HideAndDontSave;
            }
            var child = new GameObject("Composite"); child.transform.SetParent(group.transform, false);
            composite = child.AddComponent<SpriteRenderer>(); composite.sprite = compositeQuad; composite.sharedMaterial = material;
            compositeProperties = new MaterialPropertyBlock();
            return true;
        }
        private void Build()
        {
            var root = new GameObject("Character appearance"); root.transform.SetParent(transform, false);
            // The centred 64px modular canvas puts the feet 8px below the legacy
            // 32px sprite's ground pivot. Keep gameplay targets and world UI anchored
            // to the original position by lifting the complete outfit (including tools).
            root.transform.localPosition = new Vector3(0, 8f / 16f, 0);
            group = root.AddComponent<SortingGroup>(); layers = new SpriteRenderer[8];
            var orders = new[] { 0, 10, 20, 30, 40, 5, 60, 45 };
            for (var i = 0; i < layers.Length; i++)
            {
                var child = new GameObject(((CharacterSlot)i).ToString()); child.transform.SetParent(root.transform, false);
                layers[i] = child.AddComponent<SpriteRenderer>(); layers[i].sortingOrder = orders[i];
                layers[i].sharedMaterial = source.sharedMaterial;
            }
        }
        private void ClearAppearanceReferences()
        {
            if (layers != null && (profile != null || lastFrame >= 0))
            {
                foreach (var layer in layers) if (layer) layer.sprite = null;
                compositeProperties?.Clear();
                if (composite) composite.SetPropertyBlock(null);
            }
            profile = null;
            lastFrame = -1;
        }
        private void OnDisable() { if (source) source.forceRenderingOff = false; if (group) group.gameObject.SetActive(false); ClearAppearanceReferences(); }
        private void OnDestroy() { if (group) Destroy(group.gameObject); if (source) source.forceRenderingOff = false; }
    }
}

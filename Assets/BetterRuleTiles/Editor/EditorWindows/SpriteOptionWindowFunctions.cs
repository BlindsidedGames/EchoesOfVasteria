#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Internal;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{
    public static class SpriteOptionWindowFunctions
    {
        public static float DrawSpriteOptionsWithBackground(GUIBuilder guiBuilder, ref int spriteIndex, Rect rect, BetterRuleTileContainer file, ref bool showUniversalSpritesArray, ref float universalSpriteSettingDialogHeight, ref float _configWindowWidth, out Rect _dragDropArea)
        {
            // default value
            _dragDropArea = new Rect();
            // calculate height beforehand
            float height = universalSpriteSettingDialogHeight;

            // check for null
            if (file._overrideSprites[spriteIndex].BaseSprite == null && EditorUtility.DisplayDialog("Missing sprite!",
                    "The sprite you are trying to access has been deleted. The corresponding override setting will be removed.",
                    "OK"))
                return DeleteSelected(file, ref spriteIndex);
                
            // display background
            float backgroundHeight = guiBuilder.DrawBackground(rect, new GUIContent(file._overrideSprites[spriteIndex].BaseSprite.name), height).height;
            if (GUI.Button(new Rect(rect.width - 17, rect.y + 2, 20, 16), "✕") && EditorUtility.DisplayDialog("Delete override?", "Are you sure you want to delete this sprite override?", "Yes", "Cancel"))
                return DeleteSelected(file, ref spriteIndex);

            return DrawSpriteOptionsUI(rect, file, spriteIndex, ref showUniversalSpritesArray, ref universalSpriteSettingDialogHeight, ref _configWindowWidth, out _dragDropArea, 25, backgroundHeight);
        }

        static float DeleteSelected(BetterRuleTileContainer file, ref int spriteIndex)
        {
            var toDelete = file._overrideSprites[spriteIndex];
            file._overrideSprites.Remove(toDelete);
            spriteIndex = -1;
            return 0;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="file"></param>
        /// <param name="spriteIndex"></param>
        /// <param name="showUniversalSpritesArray">Reference to show or hide the sprites array</param>
        /// <param name="universalSpriteSettingDialogHeight"></param>
        /// <param name="_configWindowWidth"></param>
        /// <param name="currentOffset"></param>
        /// <param name="backgroundHeight"></param>
        /// <returns></returns>
        public static float DrawSpriteOptionsUI(Rect rect, BetterRuleTileContainer file, int spriteIndex, ref bool showUniversalSpritesArray, ref float universalSpriteSettingDialogHeight, ref float _configWindowWidth, out Rect _dragDropArea, float currentOffset = 25, float backgroundHeight = 0)
        {
            //display the UI
            file._overrideSprites[spriteIndex].GameObject = (GameObject)EditorGUI.ObjectField(GetCurrentRect(), "Gameobject", file._overrideSprites[spriteIndex].GameObject, typeof(GameObject), false);
            file._overrideSprites[spriteIndex].ColliderType = (Tile.ColliderType)EditorGUI.EnumPopup(GetCurrentRect(), "Collider type", file._overrideSprites[spriteIndex].ColliderType);
            file._overrideSprites[spriteIndex].OutputSprite = (ExtendedOutputSprite)EditorGUI.EnumPopup(GetCurrentRect(), new GUIContent("Output"), file._overrideSprites[spriteIndex].OutputSprite);
            if (file._overrideSprites[spriteIndex].OutputSprite != ExtendedOutputSprite.Single) currentOffset += 10;

            switch (file._overrideSprites[spriteIndex].OutputSprite)
            {
                case ExtendedOutputSprite.Random:
                    EditorGUI.LabelField(GetCurrentRect(), "Random tile settings", EditorStyles.boldLabel);
                    file._overrideSprites[spriteIndex].NoiseScale = EditorGUI.Slider(GetCurrentRect(), "Noise", file._overrideSprites[spriteIndex].NoiseScale, 0.001f, 0.999f);
                    file._overrideSprites[spriteIndex].RandomTransform = (RuleTile.TilingRuleOutput.Transform)EditorGUI.EnumPopup(GetCurrentRect(), "Shuffle", file._overrideSprites[spriteIndex].RandomTransform);
                    break;
                case ExtendedOutputSprite.Animation:
                    EditorGUI.LabelField(GetCurrentRect(), "Animation settings", EditorStyles.boldLabel);
                    file._overrideSprites[spriteIndex].MinAnimationSpeed = EditorGUI.FloatField(GetCurrentRect(), "Min speed", file._overrideSprites[spriteIndex].MinAnimationSpeed);
                    file._overrideSprites[spriteIndex].MaxAnimationSpeed = EditorGUI.FloatField(GetCurrentRect(), "Max speed", file._overrideSprites[spriteIndex].MaxAnimationSpeed);
                    break;
                case ExtendedOutputSprite.Pattern:
                    EditorGUI.LabelField(GetCurrentRect(), "Pattern settings", EditorStyles.boldLabel);
                    file._overrideSprites[spriteIndex].PatternSize = EditorGUI.Vector2IntField(GetCurrentRect(40), "Pattern size", file._overrideSprites[spriteIndex].PatternSize);
                    //set sizes based on the value above
                    if (file._overrideSprites[spriteIndex].PatternSize.x < 1) file._overrideSprites[spriteIndex].PatternSize.x = 1;
                    if (file._overrideSprites[spriteIndex].PatternSize.y < 1) file._overrideSprites[spriteIndex].PatternSize.y = 1;
                    int size = file._overrideSprites[spriteIndex].PatternSize.x * file._overrideSprites[spriteIndex].PatternSize.y;
                    if (size != file._overrideSprites[spriteIndex].Sprites.Length) Array.Resize(ref file._overrideSprites[spriteIndex].Sprites, size);
                    break;
                default:
                    break;
            }

            if (file._overrideSprites[spriteIndex].OutputSprite != ExtendedOutputSprite.Single)
            {
                //space
                currentOffset += 10;
                _dragDropArea = GetCurrentRect();
                EditorGUI.LabelField(_dragDropArea, "Output sprites", EditorStyles.boldLabel);

                //display a list using a custom-made editor field
                file._overrideSprites[spriteIndex].Sprites = EditorGUIField<Sprite>.ArrayField(GetCurrentRect(0), "Sprites", ref showUniversalSpritesArray, file._overrideSprites[spriteIndex].Sprites, typeof(Sprite), out float arrayHeight);
                currentOffset += arrayHeight;
            }
            else _dragDropArea = new Rect();

            //display pattern
            if (file._overrideSprites[spriteIndex].OutputSprite == ExtendedOutputSprite.Pattern)
            {
                currentOffset += 10;
                currentOffset += DisplayPatternGrid(GetWideCurrentRect(), file._overrideSprites[spriteIndex].PatternSize, file._overrideSprites[spriteIndex].Sprites, ref _configWindowWidth) + 13;
            }


            //return background height
            universalSpriteSettingDialogHeight = currentOffset - 15;
            return backgroundHeight;



            //local methods
            Rect GetCurrentRect(float h = 20)
            {
                Rect r = new Rect(rect.x + 5, rect.y + currentOffset, rect.width - 10, h - 2);
                currentOffset += h;
                return r;
            }
            Rect GetWideCurrentRect(float h = 20)
            {
                Rect r = new Rect(rect.x, rect.y + currentOffset, rect.width, h - 2);
                currentOffset += h;
                return r;
            }
        }

        public static float DisplayPatternGrid(Rect defaultRect, Vector2Int PatternSize, Sprite[] Sprites, ref float _configWindowWidth)
        {
            //get background rect
            if (defaultRect.width > 1) _configWindowWidth = defaultRect.width;

            Rect rect = new Rect(defaultRect.x, defaultRect.y + 6, _configWindowWidth, defaultRect.height);

            //set
            int columns = PatternSize.x;
            int rows = PatternSize.y;
            const float spacing = 2;
            float xOffset = 11;

            //
            float tileSize = (rect.width - 22 - (columns - 1) * spacing) / columns;
            if (tileSize > 40)
            {
                xOffset += columns * (tileSize - 40) / 2;
                tileSize = 40;
            }

            //set background height
            rect.height = (tileSize + spacing) * rows + 36;

            //draw background 
            EditorGUI.DrawRect(rect, new Color(200f / 255f, 200f / 255f, 200f / 255f));
            EditorGUI.DrawRect(rect, new Color(40f / 255f, 40f / 255f, 40f / 255f));
            EditorGUI.DrawRect(new Rect(rect.position, new Vector2(rect.width, 20)), new Color(180f / 255f, 180f / 255f, 180f / 255f));
            EditorGUI.DrawRect(new Rect(rect.position, new Vector2(rect.width, 20)), new Color(40f / 255f, 40f / 255f, 40f / 255f));
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), new GUIContent($"Pattern preview ({PatternSize.x * PatternSize.y} sprites)", "Colors:\n\n" +
                $"Gray:\t Sprites list is not the correct size!\n" +
                $"Red:\t Sprite is not set!\n" +
                $"Yellow:\t Sprite is not read and write enabled!"));

            int l = Sprites.Length;

            //
            for (int y = 0, i = 0; y < PatternSize.y; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Rect cellRect = new Rect(rect.x + xOffset + x * (spacing + tileSize), rect.y + 30 + y * (spacing + tileSize), tileSize, tileSize);

                    if (l > i && Sprites[i] != null)
                    {
                        if (Sprites[i].texture.isReadable)
                        {
                            Rect slice = Sprites[i].rect;
                            Color[] cols = Sprites[i].texture.GetPixels((int)slice.x, (int)slice.y, (int)slice.width, (int)slice.height);

                            //create texture
                            Texture2D texture = new Texture2D((int)slice.width, (int)slice.height, TextureFormat.ARGB32, false);
                            texture.SetPixels(0, 0, (int)slice.width, (int)slice.height, cols);
                            texture.filterMode = FilterMode.Point;
                            texture.Apply();

                            EditorGUI.DrawPreviewTexture(cellRect, texture);
                        }
                        else EditorGUI.DrawRect(cellRect, Color.yellow);
                    }
                    else if (l > i) EditorGUI.DrawRect(cellRect, new Color(.7f, .3f, .3f));
                    else EditorGUI.DrawRect(cellRect, Color.gray);

                    i++;
                }
            }

            //return space
            return (tileSize + spacing) * rows + 24;
        }

    }
}
#endif
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Internal;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using AreaOptions = VinTools.BetterRuleTiles.Editor.Data.TileSetImportDataClasses.TileSetImportAreaOptions;
using AnimationImportOptions = VinTools.BetterRuleTiles.Editor.Data.TileSetImportDataClasses.AnimationImportOptions;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows
{
    public partial class TileSetImportWindow : GUIWindow
    {
        /// <summary>
        /// The size of the window
        /// </summary>
        protected Vector2 WindowSize
        {
            get
            {
                switch (_currentlySelectedImportMode)
                {
                    // mode selection
                    case Tex.T_ID.NULL: return new Vector2(720, 350);
                    // Import options
                    case Tex.T_ID.ImportTilesetAnimated: return new Vector2(900, 500);
                    case Tex.T_ID.ImportTilesetPattern: return new Vector2(900, 500);
                    case Tex.T_ID.ImportTilesetRandom: return new Vector2(900, 500);
                    // default
                    default: return new Vector2(720, 350);
                }  
            }
        }
        
        /// <summary>
        /// Position used for scroll rects
        /// </summary>
        protected Vector2 ScrollPos = Vector2.zero;

        /// <summary>
        /// The file that imported textures will be added to
        /// </summary>
        protected readonly BetterRuleTileContainer File;

        /// <summary>
        /// The texture sheet that's currently being imported
        /// </summary>
        protected BetterRuleTileContainer.SpriteSheet TextureSheet;
        
        private Texture2D CheckeredBackground;
        
        /// <summary>
        /// This event gets called when a texture sheet got imported
        /// </summary>
        public event Action<BetterRuleTileContainer.SpriteSheet> OnImportedSpriteSheet;

        /// <summary>
        /// This event is called when the window tries to delete an already existing sprite sheet because it is being imported again
        /// </summary>
        public event Action<Texture2D> OnDeleteSpriteSheet;

        /// <summary>
        /// If this is true, we're working in the regular editor instead of the tileset editor
        /// </summary>
        public bool DontImportSheet = false;
        
        /// <summary>
        /// Object picker ID for the import window
        /// </summary>
        public int ImportTexturePickerID { get; private set; }// = 685741125;
        
        protected List<AreaOptions> ImportAreas = new List<AreaOptions> { new AreaOptions() };
        
        public TileSetImportWindow(BetterRuleTileContainer file, bool regularEditor = false, GUIStyle style = null) : base(WindowLayout.Center, new RectOffset(0, 0, 0, 0), Vector2.zero, (i) => Debug.LogWarning("Failed to assign window function"), new GUIContent("Import tile sets"), true, false, true, style)
        {
            this.windowFunction = UpdateUI;
            this.File = file;
            this.lockPosition = true;
            this.size = WindowSize;
            this.DontImportSheet = regularEditor;
            this.visible = false;
            
            ImportTexturePickerID = UnityEngine.Random.Range(100000000, 999999999);
        }

        private void UpdateUI(int windowID)
        {
            if (TextureSheet == null) CloseWindow();
            
            // fill background with color
            EditorGUI.DrawRect(new Rect(Vector2.zero, size + Vector2.one), Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            switch (_currentWindowState)
            {
                case Tex.T_ID.ImportTilesetAnimated: DrawAnimationModeUI(windowID); break;
                case Tex.T_ID.ImportTilesetRandom: DrawRandomizedModeUI(windowID); break;
                case Tex.T_ID.ImportTilesetPattern: DrawPatternModeUI(windowID); break;
                default: DrawModeSelectionUI(windowID); break;
            }
        }
        
        /// <summary>
        /// Close the window and reset some values
        /// </summary>
        protected void CloseWindow(bool invokeImported = false)
        {
            if (invokeImported) OnImportedSpriteSheet?.Invoke(TextureSheet);
            TextureSheet = null;
            CheckeredBackground = null;
            visible = false;
        }
        
        #region Import functions
        /// <summary>
        /// Creates a sprite sheet object from a Texture2D
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public BetterRuleTileContainer.SpriteSheet CreateSpriteSheet(Texture2D tex, out bool alreadyExists, int posX = 0, int posY = 0)
        {
            // regular tileset editor
            if (DontImportSheet)
            {
                alreadyExists = false;
                return new BetterRuleTileContainer.SpriteSheet(tex, posX, posY, 1);
            }
            
            if (File._spriteSheets.Exists(s => s.Texture == tex))
            {
                alreadyExists = true;
                return File._spriteSheets.Find(s => s.Texture == tex);
            }

            alreadyExists = false;
            var sheet = new BetterRuleTileContainer.SpriteSheet(tex, File._spriteSheetCurrentOffset.x, File._spriteSheetCurrentOffset.y);
            return sheet;
        }

        /// <summary>
        /// Calculate the area where the first frames appear
        /// </summary>
        /// <param name="topLeftCorner"></param>
        /// <param name="bottomRightCorner"></param>
        protected void CalculateFrameCorners(out Vector2Int topLeftCorner, out Vector2Int bottomRightCorner)
        {
            topLeftCorner = ImportAreas[0].Position;
            bottomRightCorner = ImportAreas[0].Position + ImportAreas[0].FrameSize;
            for (int i = 1; i < ImportAreas.Count; i++)
            {
                if (ImportAreas[i].Position.x < topLeftCorner.x) topLeftCorner.x = ImportAreas[i].Position.x;
                if (ImportAreas[i].Position.y < topLeftCorner.y) topLeftCorner.y = ImportAreas[i].Position.y;

                int right = ImportAreas[i].Position.x + ImportAreas[i].FrameSize.x;
                int bottom = ImportAreas[i].Position.y + ImportAreas[i].FrameSize.y;
                if (right > bottomRightCorner.x) bottomRightCorner.x = right;
                if (bottom > bottomRightCorner.y) bottomRightCorner.y = bottom;
            }
        }

        /// <summary>
        /// modify the sheet to only show the base sprites
        /// </summary>
        /// <param name="overrides"></param>
        /// <param name="topLeftCorner"></param>
        /// <param name="bottomRightCorner"></param>
        protected void ModifyToShowOnlyBaseSprites(List<BetterRuleTileContainer.UniversalSpriteData> overrides, Vector2Int topLeftCorner, Vector2Int bottomRightCorner)
        {
            //update values of sprite sheet
            TextureSheet.SpriteCount = bottomRightCorner - topLeftCorner;
            //update the lists
            int p = 0;
            while (p < TextureSheet.TexturePositions.Count)
            {
                //check if sprite is a base
                if (overrides.Exists(o => o.BaseSprite == TextureSheet.Sprites[p]))
                {
                    //adjust it's values
                    TextureSheet.GridCoords[p] -= topLeftCorner * TextureSheet.TilesetNeighboringSize;
                    TextureSheet.TexturePositions[p] -= topLeftCorner;

                    p++;
                }
                //if not, remove it
                else
                {
                    TextureSheet.Sprites.RemoveAt(p);
                    TextureSheet.GridCoords.RemoveAt(p);
                    TextureSheet.TexturePositions.RemoveAt(p);
                }
            }
        }

        /// <summary>
        /// Add the sheet to the file
        /// </summary>
        /// <param name="overrides"></param>
        protected void AddSpriteSheetToFile(List<BetterRuleTileContainer.UniversalSpriteData> overrides)
        {
            //add sheet
            AddSpriteSheetToFile(TextureSheet, overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                var alreadyExisting = File._overrideSprites.FindIndex(o => o.BaseSprite == overrides[i].BaseSprite);
                //override if already exists
                if (alreadyExisting != -1) File._overrideSprites[alreadyExisting] = overrides[i];
                //add if new
                else File._overrideSprites.Add(overrides[i]);
            }
        }
        /// <summary>
        /// Adds an already generated sprite sheet to the container, if it doesn't contain it already
        /// </summary>
        /// <param name="sheet"></param>
        public void AddSpriteSheetToFile(BetterRuleTileContainer.SpriteSheet sheet, List<BetterRuleTileContainer.UniversalSpriteData> overrides)
        {
            if (File._spriteSheets.Contains(sheet)) return;

            File._spriteSheets.Add(sheet);
            if (overrides == null || overrides.Count == 0) File.AddSpritesToGrid(sheet);
            else File.AddSpritesToGrid(sheet, overrides);

            File._spriteSheetCurrentOffset += new Vector2Int(0, sheet.SpriteCount.y);
        }
        
        #endregion
        #region UI Layout methods

        protected Rect GetControlRectWithPadding(float padding) => GetControlRectWithPadding(padding, padding);
        protected Rect GetControlRectWithPadding(float leftPadding, float rightPadding)
        {
            var rect = EditorGUILayout.GetControlRect();
            return new Rect(rect.x + leftPadding, rect.y, rect.width - leftPadding - rightPadding, rect.height);
        }
        protected void GetSplitControlRectWithPadding(float leftPadding, float rightPadding, out Rect leftRect, out Rect rightRect)
        {
            var rect = EditorGUILayout.GetControlRect();
            float halfSize = (rect.width - leftPadding - rightPadding) / 2;
            leftRect = new Rect(rect.x + leftPadding, rect.y, halfSize, rect.height);
            rightRect = new Rect(rect.x + leftPadding + halfSize, rect.y, halfSize, rect.height);
        }
        protected void GetSplitControlRectWithPadding(float leftPadding, float rightPadding, float split, out Rect leftRect, out Rect rightRect)
        {
            var rect = EditorGUILayout.GetControlRect();

            split = Mathf.Clamp01(split);
            float leftHalfSize = (rect.width - leftPadding - rightPadding) * split;
            float rightHalfSize = (rect.width - leftPadding - rightPadding) * (1 - split);

            leftRect = new Rect(rect.x + leftPadding, rect.y, leftHalfSize, rect.height);
            rightRect = new Rect(rect.x + leftPadding + leftHalfSize, rect.y, rightHalfSize, rect.height);
        }

        /// <summary>
        /// Draw a preview of the sprite sheet that's currently being imported
        /// </summary>
        /// <param name="rect">The area where the preview should be drawn</param>
        /// <param name="sheet">The sprite sheet that should be drawn</param>
        /// <param name="margin"></param>
        /// <param name="cellSize">Size of one sprite on the sheet</param>
        /// <returns>The exact rect of the preview</returns>
        protected Rect DrawImportedSpriteSheetPreview(Rect rect, BetterRuleTileContainer.SpriteSheet sheet, int margin, out Vector2 displayScale)
        {
            Rect rectWithMargin = new Rect(rect.x + margin, rect.y + margin, rect.width - 2 * margin, rect.height - 2 * margin);

            // determine if we want to scale based on height or width
            bool landscapePicture = (sheet.Texture.width / rectWithMargin.width) >= (sheet.Texture.height / rectWithMargin.width);

            // calculate picture rect
            Vector2 pictureSize = landscapePicture ?
                new Vector2(rectWithMargin.width, rectWithMargin.width / sheet.Texture.width * sheet.Texture.height) :
                new Vector2(rectWithMargin.height / sheet.Texture.height * sheet.Texture.width, rectWithMargin.height);
            // round to int
            pictureSize = new Vector2(
                Mathf.Floor(pictureSize.x / sheet.SpriteCount.x) * sheet.SpriteCount.x,
                Mathf.Floor(pictureSize.y / sheet.SpriteCount.y) * sheet.SpriteCount.y
                );

            // calculate cell size
            //cellSize = new Vector2(pictureSize.x / sheet.SpriteCount.x, pictureSize.y / sheet.SpriteCount.y);
            displayScale = new  Vector2(
                pictureSize.x / sheet.Texture.width,
                pictureSize.y / sheet.Texture.height
                );
            var cellSize = new Vector2(
                sheet.SpritePixelSize.x * displayScale.x,
                sheet.SpritePixelSize.y * displayScale.y
            );

            // make rect
            Rect pictureRect = new Rect(
                rectWithMargin.x + (rectWithMargin.width - pictureSize.x) / 2,
                rectWithMargin.y + (rectWithMargin.height - pictureSize.y) / 2,
                pictureSize.x,
                pictureSize.y
                );

            // draw checkerboard background
            GUI.DrawTexture(pictureRect, CheckeredBackground ? CheckeredBackground : Tex.Get(Tex.T_ID.t_checkerBoard));

            //draw image
            GUI.DrawTexture(pictureRect, sheet.Texture);

            return pictureRect;
        }

        protected void OutlineImportPreview(Rect previewRect, Vector2 displayScale, Vector2Int position, Vector2Int outlineSize, Color color, string description, int lineWidth = 1)
        {
            var cellSize = new Vector2(
                TextureSheet.SpritePixelSize.x * displayScale.x,
                TextureSheet.SpritePixelSize.y * displayScale.y
            );
            var padding = new Vector2(
                TextureSheet.SpritePixelPadding.x * displayScale.x,
                TextureSheet.SpritePixelPadding.y * displayScale.y
            );
            var initialPadding = new Vector2(
                TextureSheet.Sprites[0].rect.x * displayScale.x,
                TextureSheet.Sprites[0].rect.y * displayScale.y
            );
            
            // get the full rect of the area that needs to be outlined
            Rect printRect = new Rect(
                previewRect.x + initialPadding.x + position.x * cellSize.x + position.x * padding.x, 
                previewRect.y + initialPadding.y + position.y * cellSize.y + position.y * padding.y, 
                outlineSize.x * cellSize.x + (outlineSize.x - 1) * padding.x, 
                outlineSize.y * cellSize.y + (outlineSize.y - 1) * padding.y);

            // draw 4 edges (Up, Down, Left, Right)
            EditorGUI.DrawRect(new Rect(printRect.x, printRect.y, printRect.width, lineWidth), color);
            EditorGUI.DrawRect(new Rect(printRect.x, printRect.y + printRect.height - lineWidth, printRect.width, lineWidth), color);
            EditorGUI.DrawRect(new Rect(printRect.x, printRect.y, lineWidth, printRect.height), color);
            EditorGUI.DrawRect(new Rect(printRect.x + printRect.width - lineWidth, printRect.y, lineWidth, printRect.height), color);

            // create text style
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.LowerLeft;
            labelStyle.normal.textColor = color;
            // print text
            EditorGUI.LabelField(printRect, description, labelStyle);
        }
        
        /// <summary>
        /// Draw fields for all area options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="frameName"></param>
        /// <param name="padding"></param>
        protected void DrawAreaImportOptionsOnPreview(string displayName = "")
        {
            //draw UI for all options
            for (int i = 0; i < ImportAreas.Count; i++)
            {
                DrawImportOptions(ImportAreas[i], $"{displayName} #{i + 1}");
                //EditorGUILayout.Space();

                if (ImportAreas.Count <= i) break;
            }
        }
        
        /// <summary>
        /// Draw the options for a set of frames
        /// </summary>
        /// <param name="options"></param>
        /// <param name="frameName"></param>
        /// <param name="padding"></param>
        protected void DrawImportOptions(AreaOptions options, string frameName, float padding = 19)
        {
            bool animation = options is AnimationImportOptions;
            float lPad = padding - 4;
            float rPad = padding;
            Rect lRect;
            Rect rRect;

            //Title
            var titleRect = GetControlRectWithPadding(lPad, rPad);
            GUI.Label(titleRect, frameName, EditorStyles.boldLabel);
            var removeButtonRect = new Rect(titleRect.x + titleRect.width - titleRect.height, titleRect.y, titleRect.height, titleRect.height);
            if (GUI.Button(removeButtonRect, new GUIContent("X"))) ImportAreas.Remove(options);

            //first frame size vector 
            GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
            GUI.Label(lRect, "Frame size");
            options.FrameSize = EditorGUI.Vector2IntField(rRect, "", options.FrameSize);
            if (!animation)
            {
                options.FrameSize.x = Mathf.Clamp(options.FrameSize.x, 1, TextureSheet.SpriteCount.x);
                options.FrameSize.y = Mathf.Clamp(options.FrameSize.y, 1, TextureSheet.SpriteCount.y);
            }

            //first frame position vector 
            GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
            GUI.Label(lRect, "Frame position");
            options.Position = EditorGUI.Vector2IntField(rRect, "", options.Position);
            if (!animation)
            {
                options.Position.x = Mathf.Clamp(options.Position.x, 0, TextureSheet.SpriteCount.x - options.FrameSize.x);
                options.Position.y = Mathf.Clamp(options.Position.y, 0, TextureSheet.SpriteCount.y - options.FrameSize.y);
            }

            AnimationImportOptions animOptions = options as AnimationImportOptions;
            if (animation)
            {
                //frame spacing
                GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
                GUI.Label(lRect, "Space between frames");
                animOptions.Spacing = EditorGUI.IntField(rRect, "", animOptions.Spacing);

                //wrap direction popup
                GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
                GUI.Label(lRect, "Frame direction");
                animOptions.WrapMode = EditorGUI.Popup(rRect, animOptions.WrapMode, AnimationImportOptions.WrapDirectionLabels);
                animOptions.FrameDirection = AnimationImportOptions.WrapDirectionValues[animOptions.WrapMode]; 
            }
        }
        
        protected static BetterRuleTileContainer.UniversalSpriteData GenerateSpriteOverride(BetterRuleTileContainer.SpriteSheet sheet, AreaOptions areaOptions, ExtendedOutputSprite output, bool includeEmptyTiles = true)
        {
            //get first sprite
            Sprite mainSprite = sheet.Sprites[sheet.TexturePositions.IndexOf(areaOptions.Position)];
            //create data object
            var data = new BetterRuleTileContainer.UniversalSpriteData(mainSprite);
            data.OutputSprite = output;
            data.PatternSize = areaOptions.FrameSize;

            //create list of sprites
            List<Sprite> pattern = new List<Sprite>();

            for (int y = 0; y < areaOptions.FrameSize.y; y++)
            {
                for (int x = 0; x < areaOptions.FrameSize.x; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y) + areaOptions.Position;
                    var index = sheet.TexturePositions.IndexOf(pos);
                    
                    if ((index >= 0 && index < sheet.Sprites.Count) && (includeEmptyTiles || sheet.Sprites[index] != null))
                    {
                        pattern.Add(sheet.Sprites[index]);
                    }
                }
            }
            data.Sprites = pattern.ToArray();

            return data;
        }
        
        protected void DrawPatternOnPreview(Rect previewRect, Vector2 displayScale, AreaOptions options, string name)
        {
            OutlineImportPreview(
                previewRect,
                displayScale,
                options.Position,
                options.FrameSize,
                options.OutlineColor,
                name,
                2
            );

        }
        #endregion
    }
}
#endif
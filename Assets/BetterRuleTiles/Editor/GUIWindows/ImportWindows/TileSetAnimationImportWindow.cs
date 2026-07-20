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
    public partial class TileSetImportWindow
    {
        public void InitializeAnimationImportWindow()
        {
            this.ImportAreas = new List<AreaOptions> { new AnimationImportOptions() };
        }
        
        private Vector2 _animImportScrollPos = Vector2Int.zero;

        private bool _useAnimationRange = false;
        private float _minAnimationSpeed = 1;
        private float _maxAnimationSpeed = 1;

        void DrawAnimationModeUI(int windowID)
        {
            Rect rightRect = new Rect(width / 2, 0, width / 2, height);

            //draw background
            EditorGUI.DrawRect(rightRect, Tex.Get(Tex.C_ID.c_backgroundColor));
            //draw the preview
            var previewTextureRect = DrawImportedSpriteSheetPreview(rightRect, TextureSheet, 25, out Vector2 displayScale);

            EditorGUILayout.BeginVertical(GUILayout.Width(width / 2 - 4));
            float lPad = 15;
            float rPad = 19;
            Rect lRect;
            Rect rRect;

            EditorGUI.LabelField(GetControlRectWithPadding(lPad, rPad), "Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            //bool 
            GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
            GUI.Label(lRect, "Enable speed range");
            _useAnimationRange = EditorGUI.Toggle(rRect, "", _useAnimationRange);

            //single field
            if (!_useAnimationRange)
            {
                GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
                GUI.Label(lRect, "Animation Speed");
                _minAnimationSpeed = EditorGUI.FloatField(rRect, "", _minAnimationSpeed);
                _maxAnimationSpeed = _minAnimationSpeed;
            }
            //two fields
            else
            {
                GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
                GUI.Label(lRect, "Minimum animation Speed");
                _minAnimationSpeed = EditorGUI.FloatField(rRect, "", _minAnimationSpeed);

                GetSplitControlRectWithPadding(lPad, rPad, out lRect, out rRect);
                GUI.Label(lRect, "Maximum animation Speed");
                _maxAnimationSpeed = EditorGUI.FloatField(rRect, "", _maxAnimationSpeed);
            }

            //space before button
            EditorGUILayout.Space();

            //add frames button
            if (GUI.Button(GetControlRectWithPadding(lPad, rPad), "Add new set of frames"))
            {
                ImportAreas.Add(new AnimationImportOptions());
            }
            EditorGUILayout.Space();

            //create a different vertical layout for the scrollpos
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.Width(width / 2 - 4), GUILayout.Height(height - 184 - (_useAnimationRange ? EditorGUIUtility.singleLineHeight : 0)));
            _animImportScrollPos = EditorGUILayout.BeginScrollView(_animImportScrollPos);

            //draw UI for all options
            for (int i = 0; i < ImportAreas.Count; i++)
            {
                DrawImportOptions(ImportAreas[i], $"Frames #{i + 1}");
                EditorGUILayout.Space();

                if (ImportAreas.Count <= i) break;
            }

            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            //draw the preview
            foreach (var frameOptions in ImportAreas) DrawAnimationFramesOnPreview(previewTextureRect, displayScale, frameOptions as AnimationImportOptions);

            //import button
            var importButtonRect = new Rect(80, height - 45, width / 2 - 2 * 80, 30);
            if (GUI.Button(importButtonRect, new GUIContent("Import"))) ImportAnimationSheetFrames();
        }


        /// <summary>
        /// Draw frame outlines on the preview based on the options provided
        /// </summary>
        /// <param name="previewRect"></param>
        /// <param name="cellSize"></param>
        /// <param name="options"></param>
        void DrawAnimationFramesOnPreview(Rect previewRect, Vector2 displayScale, AnimationImportOptions options)
        {
            // calculate the amount of frames to draw
            int frames = 0;
            Vector2Int nextFramePos = options.Position;
            options.FrameDirection = new Vector2Int(
                Math.Sign(options.FrameDirection.x) * (options.FrameSize.x + options.Spacing),
                Math.Sign(options.FrameDirection.y) * (options.FrameSize.y + options.Spacing)
                );
            while (nextFramePos.x >= 0 && nextFramePos.y >= 0 && nextFramePos.x + options.FrameSize.x - 1 < TextureSheet.SpriteCount.x && nextFramePos.y + options.FrameSize.y - 1 < TextureSheet.SpriteCount.y)
            {
                frames++;
                nextFramePos += options.FrameDirection;

                // break out if the direction becomes zero because of user input
                if (options.FrameDirection == Vector2Int.zero) break;
            }

            // save the amount of frames so it doesn't need to be counted again
            options._frameCount = frames;

            // draw preview of the animation
            for (int i = 0; i < frames; i++)
            {
                OutlineImportPreview(
                    previewRect, 
                    displayScale,
                    options.Position + i * options.FrameDirection, 
                    options.FrameSize, 
                    options.OutlineColor, 
                    $"Frames {i}"
                    );
            }
        }

        /// <summary>
        /// Call this function to import the animation with the settings stored in the class
        /// </summary>
        void ImportAnimationSheetFrames()
        {
            //create an empty override list
            List<BetterRuleTileContainer.UniversalSpriteData> overrides = new List<BetterRuleTileContainer.UniversalSpriteData>();

            //create the sprite overrides for the frames
            foreach (var area in ImportAreas)
            {
                if (!(area is AnimationImportOptions frameSet)) break;

                //for every frame in the starting square
                for (int x = 0; x < frameSet.FrameSize.x; x++) {
                    for (int y = 0; y < frameSet.FrameSize.y; y++) 
                    {
                        var data = GenerateSpriteOverride(TextureSheet, new Vector2Int(x + frameSet.Position.x, y + frameSet.Position.y), frameSet, _minAnimationSpeed, _maxAnimationSpeed);
                        //check for overlap
                        if (overrides.Exists(d => d.BaseSprite == data.BaseSprite))
                        {
                            //tell user sprites cannot overlap in a popup window
                            EditorUtility.DisplayDialog("Invalid frame placement!", "Two animations cannot overlap with each other, fix this issue before importing!", "OK");
                            return;
                        }

                        if (data != null) overrides.Add(data);
                    }
                }
            }

            //calculate the area where the first frames appear
            CalculateFrameCorners(out Vector2Int topLeftCorner, out Vector2Int bottomRightCorner);
            //modify the sheet to only show the base sprites
            ModifyToShowOnlyBaseSprites(overrides, topLeftCorner, bottomRightCorner);
            AddSpriteSheetToFile(overrides);
            //close window
            CloseWindow(true);
        }

        static BetterRuleTileContainer.UniversalSpriteData GenerateSpriteOverride(BetterRuleTileContainer.SpriteSheet sheet, Vector2Int startPos, AnimationImportOptions frameOptions, float minSpeed, float maxSpeed)
        {
            //get first sprite
            Sprite mainSprite = sheet.Sprites[sheet.TexturePositions.IndexOf(startPos)];
            //create data object
            var data = new BetterRuleTileContainer.UniversalSpriteData(mainSprite);
            data.OutputSprite = ExtendedOutputSprite.Animation;
            data.MinAnimationSpeed = minSpeed;
            data.MaxAnimationSpeed = maxSpeed;

            //create list of sprites
            Sprite[] animation = new Sprite[frameOptions._frameCount];
            for (int i = 0; i < frameOptions._frameCount; i++)
            {
                var indexPos = startPos + i * frameOptions.FrameDirection;
                var index = sheet.TexturePositions.IndexOf(indexPos);
                //Debug.Log($"Position: {indexPos}, index: {index}, direction: {frameOptions.FrameDirection}");
                animation[i] = sheet.Sprites[index];
            }
            data.Sprites = animation;

            return data;
        }
        
    }
}
#endif
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Internal;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using AreaOptions = VinTools.BetterRuleTiles.Editor.Data.TileSetImportDataClasses.TileSetImportAreaOptions;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows
{
    public partial class TileSetImportWindow
    {
        public void InitializePatternImportWindow()
        {
            //empty list
            ImportAreas = new List<AreaOptions>();

            //create a pattern with the same size as the sprite sheet
            var defaultPatternOption = new AreaOptions();
            defaultPatternOption.FrameSize = TextureSheet.SpriteCount;

            //add it to the list
            ImportAreas.Add(defaultPatternOption);
        }

        
        
        void DrawPatternModeUI(int windowID)
        {
            Rect rightRect = new Rect(width / 2, 0, width / 2, height);

            //draw background
            EditorGUI.DrawRect(rightRect, Tex.Get(Tex.C_ID.c_backgroundColor));
            //draw the preview
            var previewTextureRect = DrawImportedSpriteSheetPreview(rightRect, TextureSheet, 25, out Vector2 displayScale);

            EditorGUILayout.BeginVertical(GUILayout.Width(width / 2 - 4));
            float lPad = 15;
            float rPad = 19;
            //Rect lRect;
            //Rect rRect;

            EditorGUI.LabelField(GetControlRectWithPadding(lPad, rPad), "Pattern Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            //add pattern button
            if (GUI.Button(GetControlRectWithPadding(lPad, rPad), "Add new pattern"))
            {
                ImportAreas.Add(new AreaOptions());
            }
            EditorGUILayout.Space();

            //create a different vertical layout for the scrollpos
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.Width(width / 2 - 4), GUILayout.Height(height - 140));
            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

            DrawAreaImportOptionsOnPreview("Pattern");

            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            //draw the preview
            for (int i = 0; i < ImportAreas.Count; i++)
            {
                DrawPatternOnPreview(previewTextureRect, displayScale, ImportAreas[i], $"Pattern #{i + 1}");
            }

            //import button
            var importButtonRect = new Rect(80, height - 45, width / 2 - 2 * 80, 30);
            if (GUI.Button(importButtonRect, new GUIContent("Import"))) ImportPattern();
        }


        void ImportPattern()
        {
            //create an empty override list
            List<BetterRuleTileContainer.UniversalSpriteData> overrides = new List<BetterRuleTileContainer.UniversalSpriteData>();

            //create the sprite overrides for the frames
            foreach (var pattern in ImportAreas)
            {
                var data = GenerateSpriteOverride(TextureSheet, pattern, ExtendedOutputSprite.Pattern);
                //check for overlap
                if (overrides.Exists(d => d.BaseSprite == data.BaseSprite))
                {
                    //tell user sprites cannot overlap in a popup window
                    EditorUtility.DisplayDialog("Invalid frame placement!", "Two patterns have the same sprite as the base, fix this issue before importing!", "OK");
                    return;
                }

                if (data != null) overrides.Add(data);
            }

            //calculate the area where the first frames appear
            CalculateFrameCorners(out Vector2Int topLeftCorner, out Vector2Int bottomRightCorner);
            //modify the sheet to only show the base sprites
            ModifyToShowOnlyBaseSprites(overrides, topLeftCorner, bottomRightCorner);
            AddSpriteSheetToFile(overrides);
            //close window
            CloseWindow(true);
        }
    }
}
#endif
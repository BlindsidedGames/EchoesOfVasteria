#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;
using AreaOptions = VinTools.BetterRuleTiles.Editor.Data.TileSetImportDataClasses.TileSetImportAreaOptions;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows
{
    public partial class TileSetImportWindow
    {
        /// <summary>
        /// The currently selected import mode during the mode selection screen
        /// </summary>
        private Tex.T_ID _currentlySelectedImportMode = Tex.T_ID.NULL;
        private Tex.T_ID _currentWindowState = Tex.T_ID.NULL;
        
        /// <summary>
        /// Call this function to impen the import window to import the specified texture
        /// </summary>
        /// <param name="importTex"></param>
        public void OpenWindow(Texture2D importTex, int posX = 0, int posY = 0)
        {
            _currentWindowState = Tex.T_ID.NULL;
            //_currentlySelectedTextureToImport = importTex;
            TextureSheet = CreateSpriteSheet(importTex, out bool alreadyExists, posX, posY);
            CheckeredBackground = TextureUtils.CreateTransparentTexture(importTex.width, importTex.height, 4, 4);

            if (alreadyExists)
            {
                CloseWindow();

                if (EditorUtility.DisplayDialog("Texture already imported!",
                        "This texture is already imported. Do you want to delete the sprite sheet and reimport it?\n\nWarning, deleting the sprite sheet will also delete any changes made to the sprites.",
                        "Delete sprite sheet", "Cancel"))
                {
                    //delete old 
                    OnDeleteSpriteSheet?.Invoke(importTex);
                    //open again
                    OpenWindow(importTex);
                    return;
                }
            }
            
            visible = true;
        }

        /// <summary>
        /// The contents of the UI will be shown according to this function when the user calls the GUIWindow.Show method
        /// </summary>
        /// <param name="windowID"></param>
        private void DrawModeSelectionUI(int windowID)
        {
            //calculate values
            int imgSize = Tex.Get(Tex.T_ID.ImportTilesetRegular).width;
            int buttonSize = imgSize + 12; //12 is the minimum (default padding)
            int spacing = 12;
            int numOfButtons = 4;
            float headerHeight = 80;
            float footerHeight = 80;
            float buttonMargin = (width - numOfButtons * buttonSize - (numOfButtons - 1) * spacing) / 2;

            GUI.Label(new Rect(0, (headerHeight - 20) / 2 - 12, width, 20),
                new GUIContent("Import sprite sheet"), Style.ImportWindowLabelStyle);
            GUI.Label(new Rect(0, (headerHeight - 20) / 2 + 12, width, 20),
                new GUIContent("What kind if spritesheet are you trying to import?"), Style.ImportWindowDescriptionStyle);

            //draw buttons and labels
            for (int i = 0; i < numOfButtons; i++)
            {
                // use the texture id as the identifier since it already has a corresponding texture
                Tex.T_ID currentMode = Tex.T_ID.NULL;
                
                string type = "";
                string description = "";
                switch (i)
                {
                    case 0:
                        type = "Regular";
                        description = "Simple sprite sheets";
                        currentMode = Tex.T_ID.ImportTilesetRegular;
                        break;
                    case 1:
                        type = "Animated";
                        description = "Animation sheets";
                        currentMode = Tex.T_ID.ImportTilesetAnimated;
                        break;
                    case 2:
                        type = "Randomized";
                        description = "Sheet of random sprites";
                        currentMode = Tex.T_ID.ImportTilesetRandom;
                        break;
                    case 3:
                        type = "Pattern";
                        description = "Multi-sprite patterns";
                        currentMode = Tex.T_ID.ImportTilesetPattern;
                        break;
                }

                Rect buttonRect = new Rect(buttonMargin + i * (buttonSize + spacing), headerHeight, buttonSize, buttonSize);
                Rect labelRect = new Rect(
                    buttonMargin + i * (buttonSize + spacing) + 5, 
                    headerHeight + buttonSize + 5,
                    buttonSize - 10, 
                    height - headerHeight - buttonSize - footerHeight
                    );

                //draw button
                if (GUI.Button(buttonRect, new GUIContent(Tex.Get(currentMode), $"Import {type.ToLower()} sprite sheet")))
                    _currentlySelectedImportMode = currentMode;

                //draw checkbox
                //EditorGUI.DrawRect(labelRect, Color.gray);
                if (EditorGUI.Toggle(new Rect(labelRect), _currentlySelectedImportMode == currentMode)) _currentlySelectedImportMode = currentMode;
                GUI.Label(new Rect(labelRect.x + 25, labelRect.y, labelRect.width - 25, labelRect.height / 2),
                    new GUIContent(type), EditorStyles.boldLabel);
                GUI.Label(
                    new Rect(labelRect.x + 25, labelRect.y + labelRect.height / 2, labelRect.width - 25,
                        labelRect.height / 2), new GUIContent(description), EditorStyles.miniLabel);
            }

            // next or import (basically OK) button
            var okButtonRect = new Rect((width - 120) / 2, height - (footerHeight + 30) / 2 + 5, 120, 30);
            var okButtonContent = new GUIContent(_currentlySelectedImportMode == Tex.T_ID.ImportTilesetRegular ? "Import" : "Next");
            // if clicked ok button
            if (GUI.Button(okButtonRect, okButtonContent)) SelectImportMode(_currentlySelectedImportMode);

            GUI.BringWindowToFront(windowID);
        }

        /// <summary>
        /// This function gets called once when the user selects an import mode
        /// This function calls initialization methods for the different import modes
        /// </summary>
        /// <param name="state"></param>
        void SelectImportMode(Tex.T_ID state)
        {
            _currentWindowState = state;
            
            switch (state)
            {
                // Regular import doesn't change the UI
                case Tex.T_ID.ImportTilesetRegular:
                    AddSpriteSheetToFile(TextureSheet, new List<BetterRuleTileContainer.UniversalSpriteData>());
                    //_currentlySelectedTextureToImport = null;
                    CloseWindow(true);
                    break;
                case Tex.T_ID.ImportTilesetAnimated:
                    InitializeAnimationImportWindow();
                    break;
                case Tex.T_ID.ImportTilesetRandom:
                    InitializeRandomizedImportWindow();
                    break;
                case Tex.T_ID.ImportTilesetPattern:
                    InitializePatternImportWindow();
                    break;
                // Change UI
                default:
                    break;
            }
        }
    }
}
#endif
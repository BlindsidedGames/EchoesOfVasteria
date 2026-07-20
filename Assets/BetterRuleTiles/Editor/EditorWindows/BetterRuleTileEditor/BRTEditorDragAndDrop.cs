#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.Utilities;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{
    public partial class BetterRuleTileEditor
    {
        private void CheckDragAndDrop()
        {
            if (IsMouseOverWindow() && DragAndDrop.objectReferences.Length > 0)
            {
                // drag drop into sprite drawer if selecting something
                if (IsMouseOverWindow(_window_SpriteDrawer))
                    DragDropToDrawer(DragAndDrop.objectReferences);

                // drag & drop into sprite settings
                if (IsMouseOverWindow(_window_UnivesalSpriteSettings))
                    DragDropToSpriteSettings(DragAndDrop.objectReferences);
                
                // drag and drop sprites array in universal sprite settings
                if (IsMouseOverWindow(_window_UnivesalSpriteSettings) && _window_UnivesalSpriteSettings.CheckDragDropSprites())
                    DragDropSpritesToUniversalSpriteSettings(DragAndDrop.objectReferences);
                
                // drag and drop to grid cell inspector
                if (IsMouseOverWindow(_window_GridCellInfo) && _window_GridCellInfo.CheckDragDropSprites())
                    DragDropSpritesToCellInspector(DragAndDrop.objectReferences);
            }
            else if (!IsMouseOverWindow())
            {
                //dragdrop
                if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Texture2D)
                    DragDropImage(DragAndDrop.objectReferences[0] as Texture2D);
                else if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences.All(t => t is Sprite))
                    DragDropSprites(DragAndDrop.objectReferences);
                else if (DragAndDrop.objectReferences.Length > 0)
                {

                    List<Object> sprites = new List<Object>();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is Sprite)
                        {
                            sprites.Add(obj);
                            continue;
                        }

                        if (obj is Texture2D)
                        {
                            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(obj));
                            foreach (Object sprite in subAssets.Where(t  => t is Sprite))
                                sprites.Add(sprite);
                        }
                        
                    }
                    
                    DragDropSprites(sprites.ToArray());
                }
                else DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        private void DragDropSprites(UnityEngine.Object[] sprites)
        {
            //set drag and drop icon
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //get box size
            int boxSize = 1;
            while (boxSize * boxSize < sprites.Length) boxSize++;

            //draw outline
            _grid.DrawGridTileOutline(_grid.GetMouseTilePos(), new Vector2Int(boxSize, boxSize),
                EditorTextures.Get(EditorTextures.C_ID.c_highlightColor));

            //add asset
            if (Event.current.type == EventType.DragPerform)
            {
                for (int i = 0; i < sprites.Length; i++)
                    _file.SetSprite(_grid.GetMouseTilePos() + new Vector2Int(i % boxSize, i / boxSize),
                        sprites[i] as Sprite);
            }

            //update ui
            _needsRepaint = true;
        }

        private void DragDropImage(Texture2D image)
        {
            // set drag and drop icon
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            // load sprites  
            List<Sprite> sprites = TextureImporterFunctions.LoadAllSpritesFromTexture(image);

            if (sprites.Count <= 0) return;

            // get texture grid size
            Vector2Int gridAmount = new Vector2Int(
                sprites.Select(t => t.rect.x).Distinct().Count(),
                sprites.Select(t => t.rect.y).Distinct().Count());

            // draw outline
            _grid.DrawGridTileOutline(_grid.GetMouseTilePos(), gridAmount,
                EditorTextures.Get(EditorTextures.C_ID.c_highlightColor));

            // add asset
            if (Event.current.type == EventType.DragPerform)
            {
                // only open import window if there are is than 1 sprite in the texture
                if (sprites.Count > 1) _window_ImportWindow.OpenWindow(image, _grid.GetMouseTilePos().x, _grid.GetMouseTilePos().y);
                else if (sprites.Count == 1) _file.SetSprite(_grid.GetMouseTilePos(), sprites[0]);
            }

            // update ui
            _needsRepaint = true;
        }

        private void DragDropToDrawer(UnityEngine.Object[] objects)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //return if not releasing mouse
            if (Event.current.type != EventType.DragPerform) return;

            //add sprites
            AddObjectsToSpriteCache(objects);
        }

        private void DragDropToSpriteSettings(UnityEngine.Object[] objects)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //return if not releasing mouse
            if (Event.current.type != EventType.DragPerform) return;
            //if mouse is inside the sprite dropdown UI don't add new sprites
            if (_spriteSettingsNoDropZone.Contains(Event.current.mousePosition)) return;

            foreach (var obj in objects)
            {
                if (obj is Sprite)
                {
                    var item = (Sprite)obj;

                    if (_file._overrideSprites.Exists(o =>
                            o.BaseSprite == item)) //sprite is already added to the overrides
                    {
                        EditorUtility.DisplayDialog("Sprite already added.",
                            "This sprite has been already added to the overrides.", "OK");
                    }
                    else if (_file._overrideSprites.Exists(o =>
                                 o.Sprites.Contains(item))) //sprite is part of an another animation
                    {
                        if (EditorUtility.DisplayDialog("Are you sure you want to add this sprite?",
                                "This sprite is already a part of an another sprites output. Do you still want to add it as a separate override?",
                                "Yes", "Nevermind")) AddSprite(item);
                    }
                    else //sprite is not in the list yet
                    {
                        AddSprite(item);
                    }
                }
            }

            void AddSprite(Sprite sprite)
            {
                _file._overrideSprites.Add(new BetterRuleTileContainer.UniversalSpriteData(sprite));
            }
        }

        public void AddAllTextureAssets()
        {
            var objects = Resources.FindObjectsOfTypeAll<Texture2D>();
            AddObjectsToSpriteCache(objects);
            Debug.LogWarning("Added all textures to the sprite drawer.");
        }
        /// <summary>
        /// Adds every object that is a Texture2D or sprite to the sprite drawer
        /// </summary>
        /// <param name="objects"></param>
        private void AddObjectsToSpriteCache(Object[] objects)
        {
            foreach (var sprite in DataUtils.GetSprites(objects)) SpriteCache.Add(sprite);
        }
        
        
        private void DragDropSpritesToCellInspector(Object[] objects)
        {
            if (objects.Length > 0) DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

            // when performed
            if (Event.current.type == EventType.DragPerform) 
                _inspectingCell.Sprites = _inspectingCell.Sprites.AddObjectsToSpritesArray(objects);
        }
        
        private void DragDropSpritesToUniversalSpriteSettings(Object[] objects)
        {
            if (objects.Length > 0) DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            
            var uSpriteData = _file._overrideSprites[CurrentlySepectedOverrideSpriteToModify];
            
            // when performed
            if (uSpriteData != null && Event.current.type == EventType.DragPerform) 
                uSpriteData.Sprites = uSpriteData.Sprites.AddObjectsToSpritesArray(objects);
        }
    }
}
#endif
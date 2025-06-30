#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace VinTools.BetterRuleTiles
{
    public class BetterRuleTileAssetProcessor : UnityEditor.AssetModificationProcessor
    {
        static void OnWillCreateAsset(string assetName)
        {
            BetterRuleTileContainer.CreatedAssetPath = assetName;
        }
    }
}
#endif
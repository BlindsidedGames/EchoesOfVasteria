#if UNITY_EDITOR
namespace VinTools.BetterRuleTiles.Editor.AssetHandling
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
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit {
 [CreateAssetMenu(menuName="Timeless Echoes/UI Toolkit/Pinned goals")]
 public sealed class ToolkitPinnedGoalsDefinition : ScriptableObject {
  public Sprite expanded,collapsed,ready;
  public Color textColor,backgroundColor;
  public float textSize=8;
  public float ascentRatio, descentRatio, lineHeightRatio;
 }
}

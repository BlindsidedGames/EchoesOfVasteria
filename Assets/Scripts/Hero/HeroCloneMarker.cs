using TimelessEchoes.Buffs;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Marker component used to track clone heroes spawned by a buff.
    /// </summary>
    public class HeroCloneMarker : MonoBehaviour
    {
        public BuffManager.ActiveBuff ownerBuff;
    }
}

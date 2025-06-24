using Sirenix.OdinInspector;
using UnityEngine;
using TimelessEchoes.Tasks;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    /// Utility component that generates the map chunk and procedural tasks.
    /// </summary>
    public class MapGenerateButton : MonoBehaviour
    {
        private TilemapChunkGenerator chunkGenerator;
        private ProceduralTaskGenerator taskGenerator;

        private void Awake()
        {
            chunkGenerator = GetComponent<TilemapChunkGenerator>();
            taskGenerator = GetComponent<ProceduralTaskGenerator>();
        }

        /// <summary>
        /// Generate the tilemap and tasks using the referenced generators.
        /// </summary>
        [Button(ButtonSizes.Large)]
        public void GenerateMap()
        {
            if (chunkGenerator == null)
                chunkGenerator = GetComponent<TilemapChunkGenerator>();
            if (taskGenerator == null)
                taskGenerator = GetComponent<ProceduralTaskGenerator>();

            chunkGenerator?.Generate();
            taskGenerator?.Generate();
        }

        /// <summary>
        /// Clear the currently generated map and tasks.
        /// </summary>
        [Button(ButtonSizes.Large)]
        public void ClearMap()
        {
            if (chunkGenerator == null)
                chunkGenerator = GetComponent<TilemapChunkGenerator>();
            if (taskGenerator == null)
                taskGenerator = GetComponent<ProceduralTaskGenerator>();

            chunkGenerator?.Clear();
            taskGenerator?.Clear();
        }
    }
}

#if MODULE_ENTITIES
using Pathfinding.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Pathfinding.Examples {
	[HelpURL("https://arongranberg.com/astar/documentation/stable/agentcounter.html")]
	public class AgentCounter : MonoBehaviour {
		public Text agentCountLabel;
		EntityQuery agentQuery;

		void Start () {
			agentQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(MovementState));
		}

		void OnDestroy () {
			if (World.DefaultGameObjectInjectionWorld != null) {
				var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
				if (entityManager.IsQueryValid(agentQuery)) agentQuery.Dispose();
			}
		}

		void Update () {
			int count = agentQuery.CalculateEntityCount();
			agentCountLabel.text = $"Agents: {count}";
		}
	}
}
#endif

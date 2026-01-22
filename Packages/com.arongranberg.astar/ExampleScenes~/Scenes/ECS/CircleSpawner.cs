using Pathfinding.Drawing;
using Unity.Collections;
using UnityEngine;
#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Pathfinding.Examples {
	/// <summary>
	/// Spawns a number of <see cref="FollowerEntity"/> entities in a circle around the GameObject.
	/// The spawned entities will move in a circle around the spawner.
	///
	/// This component requires the "Entities package" to be installed.
	///
	/// See: <see cref="MoveInCircle"/>
	/// See: <see cref="FollowerEntity"/>
	/// See: example_ecs (view in online documentation for working links)
	/// </summary>
	public partial class CircleSpawner : MonoBehaviourGizmos {
		public GameObject prefab;
		public int count;
		public float radius;
		public float spreadAngle = 360;
		public float spreadRandom = 0;
		public float followingOffset = 10;
		public int groups = 1;

		public override void DrawGizmos () {
			Draw.Circle(transform.position, transform.up, radius, Color.blue);
		}

#if UNITY_EDITOR
		class CircleSpawnerBaker : Baker<CircleSpawner> {
			public override void Bake (CircleSpawner authoring) {
				var center = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(center, new CircleSpawnerData {
					prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),

					count = authoring.count,
					radius = authoring.radius,
					spreadAngle = authoring.spreadAngle,
					spreadRandom = authoring.spreadRandom,
					followingOffset = authoring.followingOffset,
					groups = authoring.groups,
				});
			}
		}
#endif

		[System.Serializable]
		public struct CircleSpawnerData : IComponentData {
			public Entity prefab;
			public int count;
			public float radius;
			public float spreadAngle;
			public float spreadRandom;
			public float followingOffset;
			public int groups;
		}

		[UpdateInGroup(typeof(InitializationSystemGroup))]
		[RequireMatchingQueriesForUpdate]
		public partial struct CircleSpawnerSystem : ISystem {
			public void OnUpdate (ref SystemState state) {
				var entityManager = state.EntityManager;
				var buffer = new EntityCommandBuffer(Allocator.Temp);
				var rnd = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
				foreach (var (transform, spawnerData, entity) in SystemAPI.Query<LocalToWorld, RefRW<CircleSpawnerData> >().WithEntityAccess()) {
					ref var data = ref spawnerData.ValueRW;
					Debug.Log("Spawning... " + data.count);
					int remaining = data.count;
					for (int g = 0; g < data.groups; g++) {
						float angle0 = g / (float)data.groups * math.PI * 2;
						int cnt = remaining / (data.groups - g);
						remaining -= cnt;
						for (int i = 0; i < cnt; i++) {
							float angle = math.radians(data.spreadAngle) * i / (float)cnt + angle0;
							float3 position = new float3(
								math.cos(angle) * data.radius + (rnd.NextFloat() - 0.5f) * 2 * data.spreadRandom,
								0,
								math.sin(angle) * data.radius + (rnd.NextFloat() - 0.5f) * 2 * data.spreadRandom
								);
							position = math.transform(transform.Value, position);

							var spawnedEntity = buffer.Instantiate(data.prefab);
							buffer.SetComponent(spawnedEntity, new LocalTransform {
								Position = position,
								Rotation = Quaternion.identity,
								Scale = 1f
							});
							buffer.AddComponent(spawnedEntity, new MoveInCircle.DestinationMoveInCircle {
								target = entity,
								radius = data.radius,
								offset = data.followingOffset,
							});
							buffer.SetComponentEnabled<MoveInCircle.DestinationMoveInCircle>(spawnedEntity, true);
						}
					}

					buffer.RemoveComponent<CircleSpawnerData>(entity);
				}
				buffer.Playback(entityManager);
				buffer.Dispose();
			}
		}
	}
}
#else
namespace Pathfinding.Examples {
	[HelpURL("https://arongranberg.com/astar/documentation/stable/circlespawner.html")]
	public class CircleSpawner : MonoBehaviourGizmos {
		public void Start () {
			UnityEngine.Debug.LogError("The CircleSpawner component requires at least version 1.0 of the 'Entities' package to be installed. You can install it using the Unity package manager.");
		}
	}
}
#endif

# Timeless Echoes

Timeless Echoes is an incremental hero management game built with Unity **6000.1.6f1**.

## Overview
In this game you choose a hero who automatically runs through maps. Each run spawns a `Map` prefab which procedurally generates terrain and tasks.
Your hero spawns in the map and the `TaskController` assigns the earliest unfinished task. The hero uses A* pathfinding to reach each task in order.
Map generation combines `TilemapChunkGenerator` and `ProceduralTaskGenerator`
components so every run has a fresh layout and set of objectives.

Enemies may drop gear while chests always contain gear. Gear and experience are the main ways to progress your character.

The game continues to run maps while closed. When you return, the results of these offline runs are generated.

## Opening the Project
1. Clone or download this repository.
2. Open **Unity Hub** and add the project folder.
3. When prompted select Unity `6000.1.6f1` or a compatible editor.

## Playing
Open the `Main` scene in `Assets/Scenes` and press **Play**.

## Camera
The scene uses a **Cinemachine Camera** to follow the hero. Add the
`CameraClampExtension` component to the Cinemachine Camera to keep its Y
position locked at zero and prevent the camera from moving left of `x = 0`.

## Hero
The `HeroController` uses the A* Pathfinding Project to navigate between tasks.
If an enemy enters the hero's vision range the hero automatically engages in
combat using projectile attacks. Combat strength and movement speed are
modified by stat upgrades.

## Tasks
Task scripts can be found under `Assets/Scripts/Tasks`. A `TaskController`
builds the task list from assigned objects or from a `ProceduralTaskGenerator`.
When enabled it searches child objects for enemies and adds a `KillEnemyTask`
for each one.

Procedural tasks are ordered by their world X position so the hero progresses
from left to right. Mining and fishing tasks award resources on completion,
allowing you to upgrade your hero.

Water based tasks ignore blocking colliders and can spawn even when a tile on
the `Blocking` layer is present.
Grass tasks use a dedicated list and only appear on grass tiles. A toggle on the
generator controls whether they may spawn on the sand/grass edge.

## Map Generation
Maps are created by the `TilemapChunkGenerator` which lays out water, sand and
grass tiles. Decorative tiles can be spawned with weighted probabilities and
optional rotation. Use the `MapGenerateButton` to generate the tilemap and
procedural tasks in the editor or at runtime.

## Building
Use **File > Build Settings...** to create standalone builds.

## Development Guidelines
Refer to [Unity's official documentation](https://docs.unity3d.com) and ensure all changes work with **Unity 6000.1.6f1**.

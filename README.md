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

## Echoes
Clones spawned by milestones are referred to as **Echoes**. An Echo performs
tasks of a specified skill for a limited time when summoned.

## Tasks
Task scripts can be found under `Assets/Scripts/Tasks`. A `TaskController`
builds the task list from assigned objects or from a `ProceduralTaskGenerator`.
When enabled it searches child objects for enemies and adds a `KillEnemyTask`
for each one.

Procedural tasks are ordered by their world X position so the hero progresses
from left to right. Mining and fishing tasks award resources on completion,
allowing you to upgrade your hero.

`TaskController` also exposes a **backtrackingAdditionalWeight** setting. When
positive, tasks located behind the hero receive a priority bonus proportional
to how far back they are. Increase this value to force backtracking when the
player advances too far forward.

Water based tasks ignore blocking colliders and can spawn even when a tile on
the `Blocking` layer is present.
Grass tasks use a dedicated list and only appear on grass tiles. A toggle on the
generator controls whether they may spawn on the sand/grass edge.

## Map Generation
Maps are created by the `TilemapChunkGenerator` which lays out water, sand and
grass tiles. Decorative tiles can be spawned with weighted probabilities and
optional rotation.

## Building
Use **File > Build Settings...** to create standalone builds.

## Steam Achievements
Achievements are managed through the `AchievementManager` component which uses
Steamworks.NET. Meeting the NPC with id `Ivan1` awards the `MeetIvan` Steam
achievement, while meeting the NPC with id `Witch1` unlocks the `MeetEva`
achievement.
Engaging five slimes at once unlocks the `SlimeSwarm` achievement.
Completing Mildred's quest unlocks the `Mildred` achievement.

## Steam Rich Presence
The `RichPresenceManager` component uses Steamworks.NET to update Steam Rich
Presence. When in town, the status shows **In Town**. During a run it updates
each frame with the hero's travelled distance.
For the one-line rich presence shown under a friend's name, the script also
sets the **steam_display** key with a localization token. Example tokens are
`#Status_InTown`, `#Status_InRun` and `#Status_Distance`.

## Development Guidelines
Refer to [Unity's official documentation](https://docs.unity3d.com) and ensure all changes work with **Unity 6000.1.6f1**.

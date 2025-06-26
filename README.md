# Timeless Echoes

Timeless Echoes is an incremental hero management game built with Unity **6000.1.6f1**.

## Overview
In this game you choose a hero who automatically runs through maps. Each map is spawned from a prefab and comes with a set of objectives.
Your hero spawns at the entry point, selects a random task from the task controller and uses A* pathfinding to complete it. After finishing all tasks the hero moves to the exit and the next map begins.

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

## Tasks
Task scripts can be found under `Assets/Scripts/Tasks`. Add any of the
provided tasks to a `TaskController` component and assign entry and exit
points for your hero. When the controller enables it searches its child
objects for enemies and creates a `KillEnemyTask` for each one. Each task
stores a direct reference to that enemy, and tasks are ordered by distance
from the entry point to keep your hero on an efficient route.

Mining and fishing tasks award resources when completed. Use these resources to
upgrade your hero.

## Building
Use **File > Build Settings...** to create standalone builds.

## Development Guidelines
Refer to [Unity's official documentation](https://docs.unity3d.com) and ensure all changes work with **Unity 6000.1.6f1**.

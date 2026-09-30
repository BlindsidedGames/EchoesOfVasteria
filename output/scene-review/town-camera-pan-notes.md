# Town camera panning

Wheel sensitivity: the configured Input System uses `UniformAcrossAllPlatforms`, so one notch is 1, not the legacy Windows value of 120. Apply `exp(-scroll * 0.15)` directly. This gives about eight notches across the full 9-to-27 range. Synthetic wheel QA must use normalized values; injecting 120 or 2400 hides sensitivity mistakes.

The town Cinemachine camera follows `Hometown/Town Camera Pan Target`. `TownCameraPan` moves that target with left/middle mouse drag or one-finger touch, using the new Input System. UI-origin gestures are blocked. Adventure cameras are unchanged.

`Hometown/Town Camera Land Bounds` is an Ignore Raycast trigger used by Cinemachine Confiner 2D. Its 127 x 107 world-unit rectangle is inset half a tile from the main 128 x 108 land rectangle. It deliberately excludes river overshoot and decoration overhang. Adjust this authored rectangle if the main land boundary changes; do not derive it from combined tilemap bounds.

Preferred orthographic size is 18. Mouse wheel and two-finger pinch change it between 9 (half the default visible width/height) and 27 (1.5 times the default width/height), capped by the land bounds and actual letterboxed viewport aspect. The 32:9 maximum is about 17.84, so horizontal panning at that size is effectively locked while vertical panning remains possible. A 1/16-unit safety margin accommodates the existing pixel-grid snap. Bounds clamp the target as well as the rendered camera so overscrolling does not accumulate.

Directional panning reads the existing project-wide `InputSystem_Actions` profile's `Player/Move` action: WASD, arrows, gamepad left stick, joystick, and the added gamepad D-pad binding. The camera does not enable or disable the shared action. Movement speed scales with zoom, retains analog magnitude, and is suppressed while a town window is open. Pinching cannot transition accidentally into a one-finger drag until the gesture is released.

Validation: all eight rendered corners at 16:9 and 32:9 remained within land bounds. Rendered images were visually inspected. Input System event checks passed for mouse drag, UI-origin blocking, and single-finger touch. Physical touch hardware was not tested. Native desktop mouse automation did not reach Unity's mouse device, so mouse behavior was verified with queued Input System events.

Evidence: `town-pan-bounds-qa.txt`, `town-pan-input-qa.txt`, and `town-pan-corner-0.png` through `town-pan-corner-7.png` in this directory. The QA scripts are one-off editor probes, not runtime components.

Zoom validation: `town-zoom-qa.txt` confirms default/minimum/maximum sizes, all eight corners at maximum zoom-out for 16:9 and 32:9, WASD/arrows, analog stick, D-pad, and pinch. `town-zoom-out-0.png` and `town-zoom-out-1.png` were visually inspected. Inputs were supplied through Input System events; physical controller and touch hardware were not tested.

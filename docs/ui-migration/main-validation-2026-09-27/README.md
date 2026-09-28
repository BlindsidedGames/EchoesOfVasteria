# Main-project UI Toolkit validation

These captures come from the original EchoesOfVasteria project after the saved native cutover, not the closed temporary Editor.

| Evidence | Setup | Result |
| --- | --- | --- |
| `installed-routes.txt`, `main-*.png` | Private developed-save copy; Main; 3840x2160; VSync off; 30 FPS cap; time scale 0; Forge/Cauldron stopped | 12 checks pass; actual TownWindowManager routes and native HUD |
| `native-lifecycle.txt`, `native-*.png` | Same disposable profile; all five navigation map assets; paused setup and four seconds at normal game speed | 106 checks pass; native input/actions, run transitions and retired-renderer audit |
| `forge-edge-cases.txt` | Disposable developed profile; synthetic stock/gear; cloned core forces Vastium; crafting config temporarily 50/s; window closed/open; restored runtime config | 15 checks pass; no production assets or progression receive the synthetic test settings |
| `native-world-indicators-final.png`, `native-finishing.txt`, `native-notification.png` | Synthetic placement of enemy/task/echo to make indicators visible together; enemy initialized at 100 HP; echo remaining-time field staged | 11 state/lifetime checks plus visual inspection; not naturally reached gameplay or a throughput measurement |
| `player/` | Locally built Development Player; details/results added when validation completes | Pending |

Reproduction helpers are under `../tools/`. Run them only with the documented disposable product identity/save root and offline guards. The private fixture JSON and generated player/build output are intentionally excluded from version control. Source instrumentation and identity changes must be restored after validation.

The standard menu screenshot batch closes each route before opening the next, waits for layout, then captures at the end of a rendered frame. The lifecycle helper uses actual native NavigationSubmit/PointerDown callbacks for HUD controls; forced death and staged world values are synthetic correctness tests. It does not claim physical touchscreen testing.

The count of zero enabled uGUI graphics excludes `TMPro.TextMeshPro` world MeshRenderer effects. `TextMeshProUGUI`, Canvas and GraphicRaycaster are included. No test-time blanket suppression is used in the final lifecycle batch: the scenes/prefabs themselves contain the cutover.

Historical staged checks remain in `../validation-2026-09-27/`; old renderer comparisons there do not describe the final project's active UI.

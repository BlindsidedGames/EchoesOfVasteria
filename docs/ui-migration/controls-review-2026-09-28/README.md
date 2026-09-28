# Shared controls and spacing correction

This is a focused correction after the previous broad UI pass failed visual review. It is not approval of every window.

Changes: shared drawn checkbox (empty/checked), fixed hit area, common spacing roles 2/4/8/12, aligned menu top edges, removed duplicate inset padding in Skills/Statistics, properly laid-out skill-level labels, and a single modern close × without an embedded framed sprite.

Native runtime screenshots use a disposable developed profile, paused menus, Steam/UGS/feedback disabled, and Library-only saves. Unprefixed images are the 3840 × 2160 pass; `1280-` images are the smaller follow-up. Actions use the real button callbacks. The profile and harness remain outside version control.

`checks.txt` / `checks-1280.txt` record skill activation, task/settings/forge toggle state changes, all six skill levels remaining inside their button bounds, no old image inside Close, and the close callback actually closing the active window. Image aspect checks are mechanical checks, not a substitute for inspecting screenshots.

Skills, Tasks, Settings, Forge and Cauldron were inspected for this change. Per-window layout polish, disabled/focus visual acceptance on every surface, and physical mobile testing remain pending. No gameplay balance changes.

Follow-up: task controls were also captured disabled and with a focus request at 1280. The initial small-pass assertion assumed the skill began inactive; the disposable save retained the preceding activation, so the click correctly deactivated it. The assertion now compares before/after state and re-queries the rebuilt milestone row. This was a harness expectation error, not a gameplay failure.

Cleanup: seven temporary guards restored byte-for-byte; normal product/company restored; Editor left out of Play mode. Real saves and existing unrelated workspace changes preserved.

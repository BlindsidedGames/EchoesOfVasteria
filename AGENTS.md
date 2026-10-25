# Echoes of Vasteria agent guidance

If `AGENTS.local.md` exists in this checkout, read it before local builds or
deployments. It contains machine-specific paths and release procedures and is
gitignored. Keep credentials and private deployment notes out of tracked files.

Agents may exit Unity Play mode when necessary to complete the user's requested
work without asking for permission each time. Preserve unrelated unsaved edits
and the user's player saves; permission to stop Play does not authorize
discarding edits, overwriting progression, or changing save data.

## UI changes

UI changes must consider both supported layouts: landscape and portrait. Validate
layout, input hit targets, tutorial pointers, and transitions between orientations
in both modes; a check in only one layout is not sufficient.

## Deployment checks and patch notes

Before every deployment, check the app version, each platform's build number,
and the patch notes against the changes being shipped. Verify the current store
build numbers before choosing new ones, and confirm the built game's displayed
version matches the release. Keep the in-game Library notes and store notes in
sync with the approved release content.

Write patch notes for players: describe changes they can see or use, without
implementation details or internal development status. Use the `unslop` skill on
the draft before presenting it to the user for approval. Obtain approval for the
patch notes before publishing them or deploying the release.

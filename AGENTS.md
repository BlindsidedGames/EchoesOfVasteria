# Echoes of Vasteria agent guidance

If `AGENTS.local.md` exists in this checkout, read it before local builds or
deployments. It contains machine-specific paths and release procedures and is
gitignored. Keep credentials and private deployment notes out of tracked files.

Agents may exit Unity Play mode when necessary to complete the user's requested
work without asking for permission each time. Preserve unrelated unsaved edits
and the user's player saves; permission to stop Play does not authorize
discarding edits, overwriting progression, or changing save data.

## Farming terminology

When Matt says "seeds", include crop seed packs, fruit-tree saplings, and timber-tree
saplings unless he explicitly narrows the scope.

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

## Windows build cleanup

1. Resolve each approved folder to an absolute path under the build root.
   Check that it is inactive and that its path and contents have no reparse
   points. Keep current and rollback builds, crash symbols, release receipts,
   source, and saves. Measure free space before deleting.
2. In PowerShell, preview with `Remove-Item -LiteralPath $path -Recurse -WhatIf`.
   Delete with `Remove-Item -LiteralPath $path -Recurse -ErrorAction Stop`.
   Omit `-Force`, which triggered `blocked by policy`. Stop if you find hidden,
   read-only, or system items, or if the runner rejects the command. Report
   the affected paths and any exact error.
3. Verify the targets are gone and retained files are intact. Measure and
   report the space reclaimed.

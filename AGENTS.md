# Echoes of Vasteria agent guidance

Agents may exit Unity Play mode when necessary to complete the user's requested
work without asking for permission each time. Preserve unrelated unsaved edits
and the user's player saves; permission to stop Play does not authorize
discarding edits, overwriting progression, or changing save data.

## UI changes

UI changes must consider both supported layouts: landscape and portrait. Validate
layout, input hit targets, tutorial pointers, and transitions between orientations
in both modes; a check in only one layout is not sufficient.

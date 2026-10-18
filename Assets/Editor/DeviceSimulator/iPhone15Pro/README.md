# iPhone 15 Pro — Device Simulator

1179 × 2556 display with a Dynamic Island overlay. Portrait safe area: top 177 px, bottom 102 px. The device definition includes landscape safe areas.

Source: https://github.com/irksome-dev/com.unity.device-simulator-Apple-iPhone-14-and-15-models
License: see LICENSE.md. Only the iPhone 15 Pro profile and its overlay are included. The upstream system-info values are approximate; use this profile for layout, safe-area and touch checks, not performance benchmarking.

The border offsets match the supplied 1377 × 2729 overlay: left/right 99 px, top 86 px, bottom 87 px. Unity 6 draws the preview as a rectangle even beneath transparent exterior corners. `iPhone15ProScreenMask` rounds the preview mesh to match this screen, retaining Unity's UV orientation and leaving the original frame/Dynamic Island art intact. It only applies while this exact overlay is selected and has no player-build code. The small reflection adapter targets Unity 6's internal DeviceView because there is no public screen-shape hook; check it when upgrading Unity.

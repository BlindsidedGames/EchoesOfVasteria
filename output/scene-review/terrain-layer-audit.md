# Terrain layer audit

The original scene uses BG -5 for opaque ground and cliff-face cells, BG_Walkable -4 for transparent borders, concave corners and cliff shadows, and BG_Decor 0 in Individual mode for the original props.

The green upper plateau uses Stone_Cliff_1 grass tops and side borders. Where it meets olive lower terrain, the base of the face and its shadow use Stone_Cliff_3. Transparent edge cells need olive ground below them; otherwise green strips leak outside the raised plateau.

South-facing turns require a grass lip (12/13/14 with concave 6/7), a face row (21/22/23), a foot row (28/29/30), and a shadow row (35/36/37). Side borders continue on the overlay while their face backing is on BG -5. North-facing turns use the shallow upper rim. The repair follows inspected original examples around cells (-49,-14) and (-6,-13), and the green plateau at (24,11).

Validation: original central terrain cells on both maps match their pre-pass tile asset, transform/color indices and flags. All 769 expanded-landscape sprites use native 16 PPU, Point filtering and unit scale. Ridge and lower-cliff crops were visually inspected.

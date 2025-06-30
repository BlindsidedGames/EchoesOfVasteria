---
title: Tiling Rules
---
# Tiling rules

[[Tiling rule]]s are what makes it possible for the rule tiles to work. When placing the tile on the tilemap, it'll check all adjacent tiles, and look for a tiling rule that matches with the surroundings. In regular rule tiles you set up tiling rules by individually clicking and cycling through the available options on the small grid until you have your desired tiling rule. This can be very tedious, especially if you have a lot of rules, so Better Rule Tiles tries to overhaul this, by replacing the the small grid from the inspector with a large open [[Grid]], where you can freely place [[Tile]]s and [[Sprite]]s to create [[Tiling rule]]s.

# Creating a tiling rule

![[grid-to-rules.png]]

The concept of default tiles is the following:
- Place **Not this** rule to the side where you have an edge,
- And place a **This** rule where your tile continues.
- Leave the place empty if the tile doesn't effect the output.

The concept of tiling rules in Better Rule Tiles is the same, with the only difference on how you place them down, for which you'll need the [[Brush]] tool.
- Place a [[Sprite]] on a [[Tile]] to add that sprite to the [[Tiling rule]]s of the tile you place it on.
- Place the **same tile** next to it to mark that neighbor as continuous.
- Place the rule **Not same** to mark that neighbor as an edge.

There are also a few custom rules for more specific scenarios:
- **Empty** - it'll only match if there's no tile at that position
- **Any tile** - it'll match if there's any tile on that position

# Grouping tiling rules

The upside of having all tiles on one single large [[Grid]] instead of multiple individual grids is that you can place [[Tiling rule]]s directly next to each other. This means that you can draw out entire shapes using the sprites, than you can just draw tiles where sprites are.

> [!hint]
> Use the [[Brush]] tool to place sprites and tiles. You can see what is under a sprite using the **hide sprites** button on the [[Toolbar]].

![[group-tiling-rules.png]]

But it's not always the best to pack tiling rules tightly next to each other, as that'll require the need to set up every specific scenario that could happen. So it's a good idea to separate tiling rules in a way that'll make them more generic, leaving the position empty if that neighbor does not effect that specific output. This way you'll need to set up less rules.

![[specific-tiling-rules.png]]

If drawing out more complete shapes is easier for you to create tiling rules, than there's an option to simplify all those case specific rules into more generic rules.

If you enable the **Simplify similar rules** option in the [[Export options]], the tool will take care of creating those more generic tiling rules for you. The only downside of this option is that you'll need enough [[Tiling rule]]s for the tool to simplify.

![[The editor/images/simplify-rules.png]]

> [!info]
> For a better example, you can look at the [[Examples]] provided with the package.

# Extended tiling rules

By default, the rule tile checks in a 3x3 area around it to look for matching tiles, but what if you have more complicated tiles that require a larger area?

You can change this using the [[Grid cell inspector]] tool under the `Neighbor positions`. This small grid is similar how you'd set the rules in a regular rule tile, but here it only determines where the tiling rule will check for it's neighbors. The actual rules are determined by the tiles you place on the [[Grid]].

![[Getting started/images/grid-cell-inspector.png]]

[[Preset block]]s use this feature to create larger structures. It just basically looks for all the neighbors that are part of the preset block, and if they're in the right configuration, the rules will match.

# Interactions between tiles

Regular rule tiles don't have the ability to connect to each other, unless you create a custom scripted rule tile. But Better Rule Tiles has this feature by default.

To define a connection between two different tiles, just simply place it when the connection will happen. Just like when defining a hard edge using the `not same` rule, you can define a transition between two tiles by using the [[Tile]] you're transitioning to.

![[connection.png]]

If you want your tile to always connect to another one like it was the same tile, you can set that up in the [[Tile inspector]] window under the `Connect to` category. The tile will connect to any tile you add there just like it'd connect to itself.

> [!hint]
> You can check the [[Examples]] to see these features in action.

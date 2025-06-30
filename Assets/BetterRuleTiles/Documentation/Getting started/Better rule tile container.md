---
title: Container Asset
---
# Better rule tile container

To start using the tool first you need to create a [[Better rule tile container]] asset. This asset is the core of the tool. Everything you do will be saved into this container asset, therefore you must create one before you start doing anything else.

To create an asset, right click in the project window, and navigate to: **Create -> 2D -> Tiles -> Better Rule Tile Container**, and click on it.

![[create-asset.png]]

After you've created the asset you can either double-click to open it, or you can also select the asset and press the `Open in editor window` button to open it. 

![[open-asset.png]]

# Asset behavior

Opening and closing container assets behave as the following:
- If you have multiple container assets each of them will open it's own [[Editor window]]. 
- When trying to open a container that already has an editor open, it'll bring that [[Editor window]] into focus. 
- Deleting a container asset while editing it will also close the [[Editor window]].

> [!warning] Editing the container directly
> You shouldn't edit this asset directly, but only through the editor to avoid errors. Only edit the asset directly for debugging purposes!


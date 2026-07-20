---
title: FAQ
---
# Frequently Asked Questions

#### **My sprites get displayed as missing textures.**  
Select your image file, and in the inspector under advanced settings enable `Read/Write`, after that close the editor and reopen it again to see the changes get applied.

#### **My sprites are stretched on the grid.**  
Select your image file, and in the inspector change the `mesh type` from tight to `full rect`, this will make sure blank spaces are not left out from the image. After that close the editor and reopen it again to see the changes get applied.

#### **Sprite atlases don't work**
For the tool to be able to display the sprites in the editor, it needs read access to them. When packing the sprites into an atlas, the atlas needs to be readable as well. Select the atlas, and in the inspector under advanced settings enable `Read/Write`. (Keep in mind that this will double the memory used by this atlas, for best performance it is recommended to turn this off after the sprites are cached or when you're done editing the tiles)
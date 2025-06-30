---
title: FAQ
---
# Frequently Asked Questions

#### **My sprites get displayed as missing textures.**  
Select your image file, and in the inspector under advanced settings enable `Read/Write`, after that close the editor and reopen it again to see the changes get applied.

#### **My sprites are stretched on the grid.**  
Select your image file, and in the inspector change the `mesh type` from tight to `full rect`, this will make sure blank spaces are not left out from the image. After that close the editor and reopen it again to see the changes get applied.

#### **My tiles are blank when I place them into the tile palette.**  
You have to set the `default sprite` option in the tile. If no rules match (which are pretty likely in the tile palette) than the default sprite is shown, if that's empty it shows nothing.
---
title: Replace Selection
---
# Replace selection

The [[Replace selection]] tool is a useful tool when you want to create a new set of rules using rules you've already created, or if you just want to replace sprites or tiles. With this tool, you can replace every tile and sprite with another tile and sprite in the selection you made with the Selection tool.

![[replace-selection.gif]]

When replacing tiles, it just winds every type of tile you specified in the **replace from** section, and replaces them with the tile you specified in the **replace to** section.

When replacing tiles, it looks at the file name, finds the section which matches with the string you specified in the **replace from** section, and replaces that string to the one in the **replace to** section. It is basically a find and replace tool. A matching file will be searched in the asset database, and if a matching file was found the sprite will be replaced. If there's no matching tile nothing will happen.

```
Replace from = "Green"
Replace to = "Purple"

File = "Green_Grass" => "Purple_Grass"
```

> [!warning]
> The string is case sensitive! "Green" != "green"

> [!warning]
> The images have to have the same path, so they need to be in the same folder.
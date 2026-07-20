#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;

namespace VinTools.BetterRuleTiles.Editor.Data
{
    public static class CustomStyles
    {
        
        public static GUIStyle MiniGridButtonStyle = GenerateMiniGridButtonStyle();
        private static GUIStyle GenerateMiniGridButtonStyle()
        {
            var temp = new GUIStyle(EditorStyles.miniButtonMid);
            temp.normal.background = Tex.Get(Tex.T_ID.t_MiniButtonBG);
            temp.hover.background = Tex.Get(Tex.T_ID.t_MiniButtonBG);
            temp.active.background = Tex.Get(Tex.T_ID.t_MiniButtonBG);
            temp.onNormal.background = Tex.Get(Tex.T_ID.t_MiniButtonBG);
            temp.alignment = TextAnchor.MiddleCenter;
            temp.imagePosition = ImagePosition.ImageOnly;
            temp.padding = new RectOffset(1, 1, 2, 0);
            
            return temp;
        }
        
        
        public static GUIStyle GridWhiteTextStyle = GenerateGridWhiteTextStyle();
        private static GUIStyle GenerateGridWhiteTextStyle()
        {
            var temp = new GUIStyle("boldlabel");
            temp.normal.textColor = Color.white;

            return temp;
        }
        
        
        public static GUIStyle ToolbarButtonStyle = GenerateToolbarButtonStyle();
        private static GUIStyle GenerateToolbarButtonStyle()
        {
            //set up toolbar button style
            var temp = new GUIStyle();
            temp.normal.background = Tex.Get(Tex.T_ID.t_defaultButtonTexture);
            temp.hover.background = Tex.Get(Tex.T_ID.t_hoverButtonTexture);
            temp.active.background = Tex.Get(Tex.T_ID.t_activeButtonTexture);
            temp.onNormal.background = Tex.Get(Tex.T_ID.t_activeButtonTexture);
            temp.margin = new RectOffset(1, 1, 0, 0);
            temp.alignment = TextAnchor.MiddleCenter;
            temp.imagePosition = ImagePosition.ImageOnly;
            temp.padding = new RectOffset(2, 2, 2, 2);

            return temp;
        }

        
        public static GUIStyle SpriteSheetButtonTextStyle = GenerateSpriteSheetButtonTextStyle();
        private static GUIStyle GenerateSpriteSheetButtonTextStyle()
        {
            //sprite sheet button text
            var temp = new GUIStyle(EditorStyles.boldLabel);
            temp.alignment = TextAnchor.MiddleLeft;
            temp.wordWrap = true;

            return temp;
        }
        
        
        public static GUIStyle SpriteSheetButtonStyle = GenerateSpriteSheetButtonStyle();
        private static GUIStyle GenerateSpriteSheetButtonStyle()
        {
            //sprite sheet button
            var temp = new GUIStyle("button");
            
            return temp;
        }
        
        
        public static GUIStyle CenteredBoldLabel = GenerateCenteredBoldText();
        private static GUIStyle GenerateCenteredBoldText()
        {
            //text
            var temp = new GUIStyle(EditorStyles.boldLabel);
            temp.alignment = TextAnchor.MiddleCenter;

            return temp;
        }
        
        
        public static GUIStyle TileSelectButtonStyle = GenerateTileSelectButtonStyle();
        private static GUIStyle GenerateTileSelectButtonStyle()
        {
            //tile select button
            var temp = new GUIStyle("button");
            temp.fixedHeight = 19;
            temp.fixedWidth = 18;
            temp.padding = new RectOffset(1, 1, 1, 2);

            return temp;
        }
        
        
        public static GUIStyle MinimalInputField = GenerateMinimalInputField();
        private static GUIStyle GenerateMinimalInputField()
        {
            //inputfield
            var temp = new GUIStyle(EditorStyles.boldLabel);
            temp.margin = new RectOffset(3, 3, 1, 3);

            return temp;
        }
        
        
        public static GUIStyle IconButton = GenerateIconButton();
        private static GUIStyle GenerateIconButton()
        {
#if UNITY_2021_1_OR_NEWER
            return new GUIStyle(EditorStyles.iconButton);
#else
            return GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
#endif
        }

        
        public static GUIStyle LowerCenteredLabel = GenerateLowerCenteredLabel();
        private static GUIStyle GenerateLowerCenteredLabel()
        {
            //get style
            GUIStyle centeredTextStyle = new GUIStyle("label");
            centeredTextStyle.alignment = TextAnchor.LowerCenter;
            centeredTextStyle.wordWrap = true;

            return centeredTextStyle;
        }
        
        public static GUIStyle CenteredLabel = GenerateCenteredLabel();
        private static GUIStyle GenerateCenteredLabel()
        {
            //get style
            GUIStyle centeredTextStyle = new GUIStyle("label");
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;
            centeredTextStyle.wordWrap = true;

            return centeredTextStyle;
        }
        
        public static GUIStyle SmallCenteredLabel = GenerateSmallCenteredLabel();
        private static GUIStyle GenerateSmallCenteredLabel()
        {
            //get style
            GUIStyle centeredTextStyle = new GUIStyle("label");
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;
            centeredTextStyle.wordWrap = true;
            centeredTextStyle.fontSize = 9;

            return centeredTextStyle;
        }
        
        
        public static GUIStyle MiddleLeftLabel = GenerateMiddleLeftLabel();
        private static GUIStyle GenerateMiddleLeftLabel()
        {
            //get style
            GUIStyle centeredTextStyle = new GUIStyle("label");
            centeredTextStyle.alignment = TextAnchor.MiddleLeft;

            return centeredTextStyle;
        }
        
        
        public static GUIStyle ImportWindowLabelStyle = GenerateImportWindowLabelStyle();
        private static GUIStyle GenerateImportWindowLabelStyle()
        {
            var temp = new GUIStyle(EditorStyles.boldLabel);
            temp.fontSize = 16;
            temp.alignment = TextAnchor.LowerCenter;

            return temp;
        }
        
        
        public static GUIStyle ImportWindowDescriptionStyle = GenerateImportWindowDescriptionStyle();
        private static GUIStyle GenerateImportWindowDescriptionStyle()
        {
            var temp = new GUIStyle(EditorStyles.label);
            temp.alignment = TextAnchor.UpperCenter;

            return temp;
        }
    }
}
#endif
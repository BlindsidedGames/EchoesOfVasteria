using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VinTools.Utilities
{
    public static class TextureUtils
    {
        //texture creation
        public static Texture2D CreateColoredTexture(float color) => CreateColoredTexture(new Color(color, color, color));
        public static Texture2D CreateColoredTexture(Color color)
        {
            //create texture
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateOutlineTexture(float outlineC, int borderThickness = 1, bool fillBG = true) => CreateOutlineTexture(new Color(outlineC, outlineC, outlineC), borderThickness, fillBG);
        public static Texture2D CreateOutlineTexture(Color outlineColor, int borderThickness = 1, bool fillBG = true)
        {
            //create texture
            Texture2D tex = new Texture2D(64, 64);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color c;

                    //make outline lighter color
                    if (x < borderThickness || y < borderThickness || x >= tex.height - borderThickness || y >= tex.width - borderThickness) c = outlineColor;
                    else if (fillBG) c = new Color(40f / 256f, 40f / 256f, 40f / 256f);
                    else c = new Color(0, 0, 0, 0);

                    //set color
                    tex.SetPixel(x, y, c);
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateSquareTileButtonPreview(Color color)
        {
            //create texture
            Texture2D tex = new Texture2D(56, 56);
            Rect square = new Rect(11, 19, 34, 34);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    if (square.Contains(new Vector2(x, y))) tex.SetPixel(x, y, color);
                    else
                    {
                        float val = Math.Abs(x - square.xMin) + Math.Abs(x - square.xMax + 1) + Math.Abs(y - square.yMin) + Math.Abs(y - square.yMax + 1) - square.width - square.height;
                        float alpha = Mathf.Lerp(.3f, 0, val / 8f);

                        tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
                    }
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateMissingTexture()
        {
            //create texture
            Texture2D tex = new Texture2D(2, 2);

            //set texture colors
            tex.SetPixel(0, 0, Color.black);
            tex.SetPixel(0, 1, new Color(1, 0, 1));
            tex.SetPixel(1, 0, new Color(1, 0, 1));
            tex.SetPixel(1, 1, Color.black);

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateTransparentTexture(int width, int height, int squareWidth, int squareHeight) => CreateCheckerBoardTexture(width, height, squareWidth, squareHeight, new Color(192 / 255f, 192 / 255f, 192 / 255f), new Color(128 / 255f, 128 / 255f, 128 / 255f));
        public static Texture2D CreateCheckerBoardTexture(int width, int height, int squareWidth, int squareHeight, Color color1, Color color2)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    int w = (x / squareWidth) % 2;
                    int h = (y / squareHeight) % 2;

                    //set color
                    tex.SetPixel(x, y, w == h ? color1 : color2);
                }
            }
            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateFilledTexture(Color color, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //set color
                    tex.SetPixel(x, y, color);
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateSlopeTexture(Color color, float startHeight, float endHeight, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    float currentHeight = Mathf.Lerp(startHeight, endHeight, (float)x / (width - 1));
                    float heightPixel = (height * currentHeight);

                    //set color
                    tex.SetPixel(x, y, y <= heightPixel ? color : new Color(0, 0, 0, 0));
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreatePointedTopHexagonTexture(Color color, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {

                    if (y < tex.height / 4)
                    {
                        float currentHeight = Mathf.Lerp(0f, .5f, Mathf.Abs((width / 2 - (float)x) / (width)));
                        float heightPixel = (height * currentHeight);

                        //set color
                        tex.SetPixel(x, y, y >= heightPixel ? color : new Color(0, 0, 0, 0));
                    }
                    else if (y >= tex.height / 4 * 3)
                    {
                        float currentHeight = Mathf.Lerp(1f, .5f, Mathf.Abs((width / 2 - (float)x) / (width - 1)));
                        float heightPixel = (height * currentHeight);

                        //set color
                        tex.SetPixel(x, y, y <= heightPixel ? color : new Color(0, 0, 0, 0));
                    }
                    else
                    {
                        //set color
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateFlatTopHexagonTexture(Color color, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {

                    if (x < tex.width / 4)
                    {
                        float currentHeight = Mathf.Lerp(0f, .5f, Mathf.Abs((height / 2 - (float)y) / (height)));
                        float heightPixel = (width * currentHeight);

                        //set color
                        tex.SetPixel(x, y, x >= heightPixel ? color : new Color(0, 0, 0, 0));
                    }
                    else if (x >= tex.width / 4 * 3)
                    {
                        float currentHeight = Mathf.Lerp(1f, .5f, Mathf.Abs((height / 2 - (float)y) / (height - 1)));
                        float heightPixel = (width * currentHeight);

                        //set color
                        tex.SetPixel(x, y, x <= heightPixel ? color : new Color(0, 0, 0, 0));
                    }
                    else
                    {
                        //set color
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateCircleTexture(Color color, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //get distance
                    float dist = Vector2.Distance(new Vector2(width / 2f - .5f, height / 2f - .5f), new Vector2(x, y));

                    //set color
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, Mathf.Lerp(0, color.a, Mathf.InverseLerp(-1, 1, Mathf.Min(width, height) / 2f - dist - 1))));
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateDiamondTexture(Color color, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //draw IF
                    //the x distance from the center
                    //is smaller than
                    //the y distance from the edge
                    float diff = (height / 2 - .5f) - Mathf.Abs(height / 2 - .5f - y) - Mathf.Abs(width / 2 - .5f - x);

                    //set color
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, Mathf.Lerp(0f, color.a, Mathf.InverseLerp(-1, 1, diff))));
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateIsometricTexture(Color color, int width, int height)
        {
            //create texture
            Texture2D tex = new Texture2D(width, height);

            //clear texture
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y+=2)
                {
                    //draw IF
                    //the x distance from the center
                    //is smaller than
                    //the y distance from the edge
                    float diff = (height / 2 - .5f) - Mathf.Abs(height / 2 - .5f - y) - Mathf.Abs(width / 2 - .5f - x);

                    //set color
                    tex.SetPixel(x, height / 4 + y / 2, new Color(color.r, color.g, color.b, diff < -1 ? 0 : color.a));
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D Base64ToTexture(string base64)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.LoadImage(Convert.FromBase64String(base64));
            return tex;
        }
        public static string TextureToBase64(Texture2D texture)
        {
            byte[] bytes = texture.EncodeToPNG();
            string str = Convert.ToBase64String(bytes);
            //Debug.Log($"{texture.name}:\n\"{str}\"");
            return str;
        }
        
        //texture modification
        public static Texture2D MirrorTexture(Texture2D original, bool flipX, bool flipY)
        {
            if (!original.isReadable) return original;
            if (!flipX && !flipY) return original;

            //create texture
            Texture2D tex = new Texture2D(original.width, original.height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color color = original.GetPixel(flipX ? tex.width - x - 1 : x, flipY ? tex.height - y - 1 : y);
                    tex.SetPixel(x, y, color);
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D CreateButtonPreviewTexture(Texture2D original)
        {
            if (!original.isReadable) return CreateSquareTileButtonPreview(Color.white);

            //create texture
            Texture2D tex = new Texture2D(Mathf.RoundToInt(original.width * 1.647f), Mathf.RoundToInt(original.height * 1.647f));

            //clear texture
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //set color
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }

            //add original texture
            tex.SetPixels(Mathf.RoundToInt(tex.width * 0.1964f), Mathf.RoundToInt(tex.height * 0.3393f), original.width, original.height, original.GetPixels());

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }
        public static Texture2D AddBackgroundBlur(Texture2D original, int blurAmount)
        {
            if (!original.isReadable) return null;

            //create texture
            Texture2D tex = new Texture2D(Mathf.RoundToInt(original.width), Mathf.RoundToInt(original.height));

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, GetBlurredPixelOpacity(x, y)) + original.GetPixel(x, y));
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;

            //
            float GetBlurredPixelOpacity(int px, int py)
            {
                float numpixels = 0;
                float opacity = 0;

                for (int x = Mathf.Max(px - blurAmount, 0); x < Mathf.Min(px + blurAmount, tex.width); x++)
                {
                    for (int y = Mathf.Max(py - blurAmount, 0); y < Mathf.Min(py + blurAmount, tex.height); y++)
                    {
                        numpixels++;
                        opacity += original.GetPixel(x, y).a;
                    }
                }

                return opacity / numpixels;
            }
        }
        public static Texture2D MaskTexture(Texture2D original, Texture2D mask)
        {
            //check if sizes match
            if (original.width != mask.width || original.height != mask.height)
            {
                Debug.LogWarning("Texture and mask need to be the same size!");
                return original;
            }

            //create texture
            Texture2D tex = new Texture2D(original.width, original.height);

            //set texture colors
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color c = original.GetPixel(x, y);
                    Color m = mask.GetPixel(x, y);
                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, m.a * c.a));
                }
            }

            //apply texture
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

    }
}
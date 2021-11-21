using TMPro;
using UnityEditor;
using UnityEngine;

namespace FigmaImporter.Editor
{
    public class TMPUtils
    {
        public static TextAlignmentOptions FigmaAlignmentToTMP(string horizontalAlignment, string verticalAlignment)
        {
            int alignment = 0;
            alignment += (verticalAlignment == "TOP" ? 1 : 0) << 8;
            alignment += (verticalAlignment == "CENTER" ? 1 : 0) << 9;
            alignment += (verticalAlignment == "BOTTOM" ? 1 : 0) << 10;
            alignment += (horizontalAlignment == "LEFT" ? 1 : 0) << 0;
            alignment += (horizontalAlignment == "CENTER" ? 1 : 0) << 1;
            alignment += (horizontalAlignment == "RIGHT" ? 1 : 0) << 2;
            alignment += (horizontalAlignment == "JUSTIFIED" ? 1 : 0) << 3;
            return (TextAlignmentOptions) alignment;
        }

        public static FontStyles FigmaFontStyleToTMP(string textDecoration, string textCase)
        {
            FontStyles fontStyle = 0;
            fontStyle |= (textDecoration == "UNDERLINE" ? FontStyles.Underline : 0);
            fontStyle |= (textDecoration == "STRIKETHROUGH" ? FontStyles.Strikethrough : 0);

            fontStyle |= (textCase == "UPPER" ? FontStyles.UpperCase : 0);
            fontStyle |= (textCase == "LOWER" ? FontStyles.LowerCase : 0);
            fontStyle |= (textCase == "SMALL_CAPS" ? FontStyles.SmallCaps : 0);
            return fontStyle;
        }

        public static TextMeshProUGUI GetOrAddTMPComponentToObject(GameObject nodeGo)
        {
            var t = nodeGo.transform as RectTransform;
            var offsetMin = t.offsetMin;
            var offsetMax = t.offsetMax;
            var tmp = nodeGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = nodeGo.AddComponent<TextMeshProUGUI>(); // Somehow adding component changes size of the object???????
            t.offsetMin = offsetMin;
            t.offsetMax = offsetMax;
            return tmp;
        }

        public static void ApplyFigmaStyleToTMP(TextMeshProUGUI tmp, Style style, float scale)
        {
            tmp.fontSize = style.fontSize * scale;
            var fontLinksId = AssetDatabase.FindAssets("t:FontLinks")[0];
            FontLinks fl = AssetDatabase.LoadAssetAtPath<FontLinks>(AssetDatabase.GUIDToAssetPath(fontLinksId));

            var fontName = string.IsNullOrEmpty(style.fontPostScriptName)
                ? style.fontFamily
                : style.fontPostScriptName;
            var font = fl.Get(fontName);
            if (font == null)
            {
                Debug.LogError(
                    $"[FigmaImporter] Couldn't find font named {fontName}, please link it in FontLinks.asset");
                fl.AddName(fontName);
            }
            else
                tmp.font = font;
        }
    }
}
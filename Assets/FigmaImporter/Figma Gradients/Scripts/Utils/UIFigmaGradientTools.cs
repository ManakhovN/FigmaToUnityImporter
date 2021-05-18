using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Nox7atra.UIFigmaGradients
{
    public static class UIFigmaGradientTools
    {
        public static Color ParseColor(string colorParam, out float time)
        {
            Color color;
            if (colorParam.Contains("rgba"))
            {
                int startParseIndex = colorParam.IndexOf("(", StringComparison.Ordinal);
                int endIndex = colorParam.LastIndexOf(")", StringComparison.Ordinal);
                var colorSubstring = colorParam.Substring(startParseIndex + 1, endIndex - startParseIndex - 1);
                var colValues = colorSubstring.Split(',');
                color.r = float.Parse(colValues[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
                color.g = float.Parse(colValues[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
                color.b = float.Parse(colValues[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
                color.a = float.Parse(colValues[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
                var timeStr = colorParam.Substring(endIndex + 1, colorParam.Length - endIndex - 1).Trim()
                    .Replace("%", "");
                time = float.Parse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture);
                return color;
            }

            var parameters = colorParam.Trim().Split(' ');
            time = float.Parse(parameters[1].Trim().Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture) / 100;
            if (ColorUtility.TryParseHtmlString(parameters[0].Trim(), out color))
            {
                return color;
            }

            throw new Exception("Invalid color format!");
        }

        public static List<string> ParseLinearCssParams(string css)
        {
            List<string> result = new List<string>();
            int startParseIndex = css.IndexOf("(", StringComparison.Ordinal);
            int endIndex = css.LastIndexOf(")", StringComparison.Ordinal);
            string parametersStr = css.Substring(startParseIndex + 1, endIndex - startParseIndex - 1);
            bool ignoreComma = false;
            var tmpStr = "";
            for (int i = 0; i < parametersStr.Length; i++)
            {
                var ch = parametersStr[i];
                if (ch == '(')
                {
                    ignoreComma = true;
                }

                if (ch == ')')
                {
                    ignoreComma = false;
                }
                if (!ignoreComma)
                {
                    if (ch == ',')
                    {
                        result.Add(tmpStr);
                        tmpStr = "";
                        continue;
                    }
                }
                tmpStr += ch;
            }
            result.Add(tmpStr);
            return result;
        }

        public static List<string> ParseRadialCssParams(string css)
        {
            List<string> result = new List<string>();
            int startParseIndex = css.IndexOf("(", StringComparison.Ordinal);
            int endIndex = css.LastIndexOf(")", StringComparison.Ordinal);
            string parametersStr = css.Substring(startParseIndex + 1, endIndex - startParseIndex - 1);
            bool ignoreComma = false;
            var tmpStr = "";
            for (int i = 0; i < parametersStr.Length; i++)
            {
                var ch = parametersStr[i];
                if (ch == '(')
                {
                    ignoreComma = true;
                }

                if (ch == ')')
                {
                    ignoreComma = false;
                }
                if (!ignoreComma)
                {
                    if (ch == ',')
                    {
                        result.Add(tmpStr);
                        tmpStr = "";
                        continue;
                    }
                }
                tmpStr += ch;
            }
            result.Add(tmpStr);
            return result;
        }
    }
}
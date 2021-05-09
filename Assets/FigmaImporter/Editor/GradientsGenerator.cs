using System.Linq;
using UnityEngine;

namespace FigmaImporter.Editor
{
    [CreateAssetMenu(menuName = "FigmaImporter/GradientsGenerator")]
    public class GradientsGenerator : ScriptableObject //I wanted to make shaders for each gradient, that's why it is SO.
    {
        public Texture2D GetTexture(Fill fill, Vector2 nodeSize, int size = 128)
        {
            Texture2D result = new Texture2D(size, (int) (size * nodeSize.y / nodeSize.x), TextureFormat.RGBA32, false);
            switch (fill.type)
            {
                case "GRADIENT_RADIAL":
                    GenerateRadialGradient(result, fill);
                    break;
                case "GRADIENT_LINEAR":
                    GenerateLinearGradient(result, fill);
                    break;
                case "GRADIENT_DIAMOND":
                    GenerateDiamondGradient(result, fill);
                    break;
                case "GRADIENT_ANGULAR":
                    GenerateAngularGradient(result, fill);
                    break;
            }

            result.Apply();
            return result;
        }

        private void GenerateAngularGradient(Texture2D result, Fill fill)
        {
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            var p2 = fill.gradientHandlePositions[2].ToVector2();
            var pivot = fill.gradientHandlePositions[0].ToVector2();
            for (int x = 0; x < result.width; x++)
            {
                for (int y = 0; y < result.height; y++)
                {
                    float normalizedX = x / (float) result.width;
                    float normalizedY = y / (float) result.height;
                    var currentPoint = new Vector2(normalizedX, normalizedY);
                    float progress1 = (CalcProgress(pivot,
                        p1, currentPoint, true, true));
                    float progress2 = (CalcProgress(pivot,
                        p2, currentPoint, true, true));
                    float progress = Vector2.SignedAngle(Vector2.right, new Vector2(progress1, progress2));
                    if (progress < 0f)
                        progress = progress + 360;
                    progress /= 360f;
                    for (int i = 1; i < fill.gradientStops.Length; i++)
                    {
                        var prevGr = fill.gradientStops[i - 1];
                        var gr = fill.gradientStops[i];
                        if (prevGr.position <= progress && progress <= gr.position)
                            result.SetPixel(x, result.height - y - 1,
                                UnityEngine.Color.Lerp(prevGr.color.ToColor(), gr.color.ToColor(),
                                    (progress - prevGr.position) / (gr.position - prevGr.position)));
                    }
                }
            }
        }

        private void GenerateDiamondGradient(Texture2D result, Fill fill)
        {
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            var p2 = fill.gradientHandlePositions[2].ToVector2();

            var pivot = fill.gradientHandlePositions[0].ToVector2();
            for (int x = 0; x < result.width; x++)
            {
                for (int y = 0; y < result.height; y++)
                {
                    float normalizedX = x / (float) result.width;
                    float normalizedY = y / (float) result.height;
                    float progress1 = (CalcProgress(pivot,
                        p1, new Vector2(normalizedX, normalizedY), true));
                    float progress2 = (CalcProgress(pivot,
                        p2, new Vector2(normalizedX, normalizedY), true));
                    float progress = Mathf.Clamp01(progress1 + progress2);
                    for (int i = 1; i < fill.gradientStops.Length; i++)
                    {
                        var prevGr = fill.gradientStops[i - 1];
                        var gr = fill.gradientStops[i];
                        if (prevGr.position <= progress && progress <= gr.position)
                            result.SetPixel(x, result.height - y - 1,
                                UnityEngine.Color.Lerp(prevGr.color.ToColor(), gr.color.ToColor(),
                                    (progress - prevGr.position) / (gr.position - prevGr.position)));
                    }
                }
            }
        }

        private void GenerateLinearGradient(Texture2D result, Fill fill)
        {
            for (int x = 0; x < result.width; x++)
            {
                for (int y = 0; y < result.height; y++)
                {
                    float normalizedX = x / (float) result.width;
                    float normalizedY = y / (float) result.height;
                    float progress = (CalcProgress(fill.gradientHandlePositions[0].ToVector2(),
                        fill.gradientHandlePositions[1].ToVector2(), new Vector2(normalizedX, normalizedY)));
                    progress = Mathf.Clamp01(progress);
                    for (int i = 1; i < fill.gradientStops.Length; i++)
                    {
                        var prevGr = fill.gradientStops[i - 1];
                        var gr = fill.gradientStops[i];
                        if (prevGr.position <= progress && progress <= gr.position)
                            result.SetPixel(x, result.height - y - 1,
                                UnityEngine.Color.Lerp(prevGr.color.ToColor(), gr.color.ToColor(),
                                    (progress - prevGr.position) / (gr.position - prevGr.position)));
                    }
                }
            }
        }

        private void GenerateRadialGradient(Texture2D result, Fill fill)
        {
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            var p2 = fill.gradientHandlePositions[2].ToVector2();
            //bug: There is a bug in figma. If u change the size of the node, its vectors may not be perpendicular to each other.
            var pivot = fill.gradientHandlePositions[0].ToVector2();
            for (int x = 0; x < result.width; x++)
            {
                for (int y = 0; y < result.height; y++)
                {
                    float normalizedX = x / (float) result.width;
                    float normalizedY = y / (float) result.height;
                    float progress1 = (CalcProgress(pivot,
                        p1, new Vector2(normalizedX, normalizedY), true));
                    float progress2 = (CalcProgress(pivot,
                        p2, new Vector2(normalizedX, normalizedY), true));
                    float progress = Mathf.Clamp01(Mathf.Sqrt(progress1 * progress1 + progress2 * progress2));
                    for (int i = 1; i < fill.gradientStops.Length; i++)
                    {
                        var prevGr = fill.gradientStops[i - 1];
                        var gr = fill.gradientStops[i];
                        if (prevGr.position <= progress && progress <= gr.position)
                            result.SetPixel(x, result.height - y - 1,
                                UnityEngine.Color.Lerp(prevGr.color.ToColor(), gr.color.ToColor(),
                                    (progress - prevGr.position) / (gr.position - prevGr.position)));
                    }
                }
            }
        }

        private float CalcProgress(Vector2 pivot, Vector2 p1, Vector2 p2, bool twoWays = false, bool sign = false)
        {
            var v2 = p2 - pivot;
            var v1 = p1 - pivot;
//            var angle = Vector2.Angle(v2, v1);
//            if (v2.magnitude * Mathf.Cos(angle) >= 0.5f)
//            {
//                Debug.Log("sadasdsad");
//            }

            //return (v2.magnitude * Mathf.Cos(angle));

            var dot = Vector2.Dot(v2, v1);
            if (!twoWays && dot < 0f)
                return 0f;
            if (sign)
                return Mathf.Sign(dot) * ((dot / v1.sqrMagnitude) * v1).magnitude / v1.magnitude;
            return ((dot / v1.sqrMagnitude) * v1).magnitude / v1.magnitude;
        }
    }
}
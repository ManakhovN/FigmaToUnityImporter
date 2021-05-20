using System.Linq;
using Nox7atra.UIFigmaGradients;
using UnityEngine;
using UnityEngine.UI;

namespace FigmaImporter.Editor
{
    [CreateAssetMenu(menuName = "FigmaImporter/GradientsGenerator")]
    public class
        GradientsGenerator : ScriptableObject //I wanted to make shaders for each gradient, that's why it is SO.
    {
        public void AddGradient(Fill fill, Image image)
        {
            var go = image.gameObject;
            DestroyImmediate(image);
            switch (fill.type)
            {
                case "GRADIENT_RADIAL":
                    GenerateRadialGradient(go, fill);
                    break;
                case "GRADIENT_LINEAR":
                    GenerateLinearGradient(go, fill);
                    break;
                case "GRADIENT_DIAMOND":
                    GenerateDiamondGradient(go, fill);
                    break;
                case "GRADIENT_ANGULAR":
                    GenerateAngularGradient(go, fill);
                    break;
            }
        }

        private void GenerateAngularGradient(GameObject go, Fill fill)
        {
            var angularGradient = go.AddComponent<UIFigmaGradientAngularDrawer>();
            Gradient gradient = GenerateGradient(fill);
            var p0 = fill.gradientHandlePositions[0].ToVector2();
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            float angle = Vector2.SignedAngle(Vector2.down, p1 - p0);
            angularGradient.SetParameters(gradient, angle, p0);
        }

        private Gradient GenerateGradient(Fill fill)
        {
            var gradientStopsLength = fill.gradientStops.Length;
            GradientColorKey[] colorKeys = new GradientColorKey[gradientStopsLength];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[gradientStopsLength];

            for (int i = 0; i < gradientStopsLength; i++)
            {
                float time = fill.gradientStops[i].position;
                var col = fill.gradientStops[i].color.ToColor();
                var colorKey = new GradientColorKey();
                colorKey.color = col;
                colorKey.time = time;
                var alphaKey = new GradientAlphaKey();
                alphaKey.alpha = col.a;
                alphaKey.time = time;
                colorKeys[i] = colorKey;
                alphaKeys[i] = alphaKey;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
            return gradient;
        }

        private void GenerateDiamondGradient(GameObject go, Fill fill)
        {
            var angularGradient = go.AddComponent<UIFigmaGradinetDiamondDrawer>();
            Gradient gradient = GenerateGradient(fill);
            var p0 = fill.gradientHandlePositions[0].ToVector2();
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            var p2 = fill.gradientHandlePositions[2].ToVector2();
            float angle = Vector2.SignedAngle(Vector2.right, p1 - p0);
            float r1 = (p0 - p1).magnitude * 2;
            float r2 = (p0 - p2).magnitude * 2;
            angularGradient.SetParameters(gradient, angle, p0, r1, r2);
        }

        private void GenerateLinearGradient(GameObject go, Fill fill)
        {
            var angularGradient = go.AddComponent<UIFigmaGradientLinearDrawer>();
            Gradient gradient = GenerateGradient(fill);
            var p0 = fill.gradientHandlePositions[0].ToVector2();
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            float angle = Vector2.SignedAngle(Vector2.down, p1 - p0);
            float d = (p0 - p1).magnitude;
            p0.y = 1 - p0.y;
            p1.y = 1 - p1.y;
            Vector2 center = (p0 + p1) / 2;
            Debug.Log(center);
            angularGradient.SetParameters(gradient, angle, d, center);
        }

        private void GenerateRadialGradient(GameObject go, Fill fill)
        {
            var angularGradient = go.AddComponent<UIFigmaGradientRadialDrawer>();
            Gradient gradient = GenerateGradient(fill);
            var p0 = fill.gradientHandlePositions[0].ToVector2();
            var p1 = fill.gradientHandlePositions[1].ToVector2();
            var p2 = fill.gradientHandlePositions[2].ToVector2();
            float angle = Vector2.SignedAngle(Vector2.right, p1 - p0);
            float r1 = (p0 - p1).magnitude;
            float r2 = (p0 - p2).magnitude;
            angularGradient.SetParameters(gradient, angle, p0, r1, r2);
        }
    }
}
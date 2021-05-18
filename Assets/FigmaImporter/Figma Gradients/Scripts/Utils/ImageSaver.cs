using System.IO;
using UnityEngine;

namespace Nox7atra.UIFigmaGradients
{
    [RequireComponent(typeof(UIFigmaGradientLinearDrawer))]
    public class ImageSaver : MonoBehaviour
    {
        [EditorButton]
        private void SaveImage()
        {
            var testImagesPath = Path.Combine(Application.dataPath, "Figma Gradients/GeneratedGradientImages");
            var image = GetComponent<UIFigmaGradientLinearDrawer>();
            File.WriteAllBytes(
                Path.Combine(testImagesPath, $"{image.name}{image.GetHashCode()}.png"),
                (image.mainTexture as Texture2D).EncodeToPNG());
        }
    }
}
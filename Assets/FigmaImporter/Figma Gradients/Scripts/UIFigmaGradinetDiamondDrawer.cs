using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nox7atra.UIFigmaGradients
{
    public class UIFigmaGradinetDiamondDrawer : UIFigmaGradientRadialDrawer
    {
        protected override Material GradientMaterial => new Material(Shader.Find("UI/DiamondGradientShader"));
    }
}
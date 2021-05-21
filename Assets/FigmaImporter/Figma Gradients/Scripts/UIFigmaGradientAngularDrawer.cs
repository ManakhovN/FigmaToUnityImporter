using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nox7atra.UIFigmaGradients
{
    public class UIFigmaGradientAngularDrawer : UIFigmaGradientLinearDrawer
    {
        protected override Material GradientMaterial => new Material(Shader.Find("UI/AngularGradientShader"));
        protected override TextureWrapMode WrapMode => TextureWrapMode.Repeat;

        
        protected override void GenerateHelperUvs(VertexHelper vh)
        {
            UIVertex vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++) {
                vh.PopulateUIVertex(ref vert, i);
                vert.normal = new Vector3(_Center.x, 1 - _Center.y, _Angle);
                vh.SetUIVertex(vert, i);
            }
        }
        
        public override void SetParameters(params object[] parameters)
        {
            _Gradient = (Gradient)parameters[0];
            _Angle = (float)parameters[1];
            _Center = (Vector2) parameters[2];
            OnValidate();         
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Nox7atra.UIFigmaGradients
{
    public class UIFigmaGradientRadialDrawer : UIFigmaGradientLinearDrawer
    {
        [SerializeField] protected Vector2 _Center;
        [Range(0.01f, 10)]
        [SerializeField] protected float _Radius1 = 1;
        [Range(0.01f, 10)]
        [SerializeField] protected float _Radius2 = 1;
        protected override Material GradientMaterial => new Material(Shader.Find("UI/RadialGradientShader"));

        protected override void GenerateHelperUvs(VertexHelper vh)
        {
            UIVertex vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++) {
                vh.PopulateUIVertex(ref vert, i);
                vert.normal = new Vector3(_Radius1, _Radius2, _Angle);
                vert.uv1 = new Vector2(_Center.x, 1 - _Center.y);
                vh.SetUIVertex(vert, i);
            }
        }

        public override void SetParameters(params object[] parameters)
        {
            _Gradient = (Gradient)parameters[0];
            _Angle = (float)parameters[1];
            _Center = (Vector2) parameters[2];
            _Radius1 = (float) parameters[3];
            _Radius2 = (float) parameters[4];
            OnValidate();         
        }
    }
}
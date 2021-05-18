using System;
using System.Collections;
using System.Collections.Generic;
using Nox7atra.UIFigmaGradients;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(UIFigmaGradientLinearDrawer))]
public class UIFigmaGradientCustomEditor : Editor
{
    private UIFigmaGradientLinearDrawer _Target;
    private string _Css;
    private void OnEnable()
    {
        _Target = target as UIFigmaGradientLinearDrawer;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        _Css = EditorGUILayout.TextField("Figma CSS string", _Css);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(
            "Parse CSS"
        ))
        {
            _Target.ParseCss(_Css);   
        }
        GUILayout.EndHorizontal();
    }
}

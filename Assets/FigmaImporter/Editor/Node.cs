using System;
using UnityEngine;

namespace FigmaImporter.Editor
{
    [Serializable]
    public class Node
    {
        public string id;
        public string name;
        public string type;
        public string blendMode;
        public Node[] children;
        public AbsoluteBoundingBox absoluteBoundingBox; // done
        public Constraints constraints; // done
        public bool clipsContent;
        public Fill[] background;
        public Fill[] fills;
        public Fill[] strokes;
        public float strokeWeight;
        public string strokeAlign;
        public Color backgroundColor;
        public Grid[] layoutGrids;
        public Effect[] effects;
        public string characters;
        public Style style;
        public string transitionNodeID;
        public float transitionDuration;
        public string transitionEasing;
    }

    [Serializable]
    public class AbsoluteBoundingBox
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Vector2 GetPosition()
        {
            return new Vector2(x, y);
        }

        public Vector2 GetSize()
        {
            return new Vector2(width, height);
        }
    }

    [Serializable]
    public class Constraints
    {
        public string vertical;
        public string horizontal;
    }
    [Serializable]
    public class Fill
    {
        public string blendMode;
        public string visible;
        public string type;
        public Color color;
        public string imageRef;
        public Vector[] gradientHandlePositions;
        public GradientStops[] gradientStops;
    }
    [Serializable]
    public class Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public UnityEngine.Color ToColor()
        {
            return new UnityEngine.Color(r,g,b,a);
        }
    }

    [Serializable]
    public class Grid
    {
        public string pattern;
        public float sectionSize;
        public bool visible;
        public Color color;
        public string alignment;
        public int gutterSize;
        public float offset;
        public int count;
    }
    
    [Serializable]
    public class Effect
    {
        public string type;
        public bool visible;
        public Color color;
        public string blendMode;
        public Vector offset;
        public float radius;
    }
    
    [Serializable]
    public class Vector
    {
        public float x;
        public float y;

        public Vector2 ToVector2()
        {
            return new Vector2(x,y);
        }
    }

    [Serializable]
    public class GradientStops
    {
        public Color color;
        public float position;
    }

    [Serializable]
    public class Style
    {
        public string fontFamily;
        public string fontPostScriptName;
        public int fontWeight;
        public float fontSize;
        public string textAlignHorizontal;
        public string textAlignVertical;
        public float letterSpacing;
        public float lineHeightPx;
        public float lineHeightPercent;
        public string lineHeightUnit;
        public string textCase;
        public string textDecoration;
    }

    public enum FontWeight
    {
        Thin = 100, Light = 300, Regular = 400, Medium = 500, Bold = 700, Black = 900,
        ThinItalic = 100
    }
}

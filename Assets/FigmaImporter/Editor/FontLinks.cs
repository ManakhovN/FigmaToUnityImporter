using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace FigmaImporter.Editor
{
    [CreateAssetMenu(menuName = "FigmaImporter/FontLinks")]
    public class FontLinks : ScriptableObject
    {
        [SerializeField] private List<FontStringPair> _fonts;
        public TMP_FontAsset Get(string name)
        {
            var font = _fonts?.FirstOrDefault(x => x.Name == name);
            return font?.Font;
        }

        public void AddName(string font)
        {
            if (_fonts?.FirstOrDefault(x => x.Name == name) == null)
                _fonts.Add(new FontStringPair(font, null));
        }
    }

    [Serializable]
    public class FontStringPair
    {
        public string Name;
        public TMP_FontAsset Font;

        public FontStringPair(string name, TMP_FontAsset font)
        {
            Name = name;
            Font = font;
        }
    }
}

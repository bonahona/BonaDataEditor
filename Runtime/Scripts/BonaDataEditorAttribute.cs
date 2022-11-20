using System;
using UnityEngine;

namespace Fyrvall.DataEditor
{
    public class BonaDataEditorAttribute : Attribute
    {
        public string DisplayName = string.Empty;
        public bool UseIcon = false;
        public int IconGroupIndex = 0;
        public string IconPath = string.Empty;
        public KeyCode HotKey = KeyCode.None;
    }
}
using System;
using UnityEngine;

namespace CollisionBear.BearDataEditor
{
    public class BearDataEditorAttribute : Attribute
    {
        public string DisplayName = string.Empty;
        public bool UseIcon = false;
        public int IconGroupIndex = 0;
        public string IconPath = string.Empty;
        public KeyCode HotKey = KeyCode.None;
    }
}
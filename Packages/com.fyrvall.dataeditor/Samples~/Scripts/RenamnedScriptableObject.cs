using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fyrvall.DataEditor;       // Namespace for the Data editor code. In order to use the DataEditor attribute, this namespace should be included. Alternatively use [Fyrvall.DataEditor.BonaDataEditor].

namespace Fyrvall.Example
{
    // By using this attribute on the class, it will appear in the data editor. Only works on classes inheriting from ScriptableObject and MonoBehaviours.
    // Is this example, its display name is overidden. Instead of displaying it's class name "Renamed Scriptable Object" it will read "Another Class".
    [BonaDataEditor(DisplayName = "Another Class")]            
    public class RenamedScriptableObject: ScriptableObject
    {
        // All these properties will be displayed in the Data Editor window and serialized as if it was opened in the normal inspector.
        public string TestString;
        [TextArea(minLines: 4, maxLines: 4)]
        public string TestLongText;
        public int TestInt;
        public Vector3 TestVector;
    }
}
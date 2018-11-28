using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fyrvall.DataEditor;       // Namespace for the Data editor code. In order to use the DataEditor attribute, this namespace should be included. Alternatively use [Fyrvall.DataEditor.BonaDataEditor].

namespace Fyrvall.Example
{
    [BonaDataEditor]            // By using this attribute on the class, it will appear in the data editor. Only works on classes inheriting from ScriptableObject and MonoBehaviours.
    public class TestScriptableObject: ScriptableObject
    {
        // All these properties will be displayed in the Data Editor window and serialized as if it was opened in the normal inspector.
        public string TestString;
        [TextArea(minLines: 4, maxLines: 4)]
        public string TestLongText;
        public int TestInt;
        public Vector3 TestVector;
    }
}
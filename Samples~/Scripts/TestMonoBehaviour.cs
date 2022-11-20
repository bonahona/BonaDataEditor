using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fyrvall.DataEditor;       // Namespace for the Data editor code. In order to use the DataEditor attribute, this namespace should be included. Alternatively use [Fyrvall.DataEditor.BonaDataEditor].

namespace Fyrvall.Example
{
    [BonaDataEditor]            // By using this attribute on the class, it will appear in the data editor. Only works on classes inheriting from ScriptableObject and MonoBehaviours.
    public class TestMonoBehaviour: MonoBehaviour
    {
        // All these properties will be displayed in the Data Editor window and serialized as if it was opened in the normal inspector.
        public float TestFloat1;
        public float TestFloat2;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;

public class BonaDataEditorAttribute : Attribute
{
    public string DisplayName = string.Empty;
    public int SortOrder = int.MaxValue;
}

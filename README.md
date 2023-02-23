# Bona Data Editor
![alt Bona Data editor splash](http://gamedevelopersplayground.com/files/38100729-e7d9-4164-825d-64b35ecd929e.webp)
Bona Data Editor is a Unity3D editor extension, made to simplify working with data. 
Classes inheriting from ScriptableObject and MonoBehaviour can use the ``[BonaDataEditor]``
attribute (available in the ``Fyrvall.BonaEditor`` namespace)to be exposed in the editor window.
When exposed and chosen, any scriptable object instances of a given type, or prefab with a specific
monobehaviour component will be displayed in the editor sorted by name, regardless of their
location in the project folder.
Several editor instances can be open simultaneously. Searching by name among instances are supported and
next to every asset is a folder icon, allowing you to see its location in your Unity project folder.

## Getting started
First you need a cope of the software. 

### Unity Package
The editor extension can be added Unity's package manager from 'Add package from git URL'
* <https://github.com/bonahona/bonadataeditor.git>


### Asset store
* <https://assetstore.unity.com/packages/tools/utilities/bona-data-editor-134191>

### Manual download
It can be downloaded from the following sources.
You need to put the PreviewField content inside your Unity project's Asset folder.
* <https://github.com/bonahona/previewfield.git>

## Example
Decorate a class with the attribute. By default the class's name will be displayed.
```cs
using Fyrvall.DataEditor;

[BonaDataEditor]
class TestClass: ScriptableObject {}
```
or
```cs
  [Fyrvall.DataEditor.BonaDataEditor]
  class TestClass: ScriptableObject {}
  ```
If you want to display another name for your class in the editor, enter a `DisplayName` for it in the attribute.
```cs
using Fyrvall.DataEditor;

[BonaDataEditor(DisplayName = "Some other name")]
class TestClass: ScriptableObject {}
```

## License
This project is released as Open Source under a [MIT license](https://opensource.org/licenses/MIT).

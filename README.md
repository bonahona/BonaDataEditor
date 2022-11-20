# Bona Data Editor
Bona Data Editor is a Unity3D editor extension, made to simplify working with data. 
Classes inheriting from ScriptableObject and MonoBehaviour can use the ``[BonaDataEditor]``
attribute (available in the ``Fyrvall.BonaEditor`` namespace)to be exposed in the editor window.
When exposed and chosen any scriptable object instances of a given type or prefab with a specific
monobehaviour component will be displayed in the editor sorted by name, irregardless of their
location in the project folder.
Several editor instances can be open simultaneous. Searching by name among instances are supported and
next to every asset is a folder icon, allowing you to see its location in tour Unity project folder.

## Getting started
First you need a cope of the software. It can be downloaded from the following sources.
* <https://github.com/bonahona/bonadataeditor.git>

If grabbed from Unity's Asset Store, everything should be set up already.
If you grabbed from any other source, you need to put the BonaDataEditor inside your Unity project's Asset folder.

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
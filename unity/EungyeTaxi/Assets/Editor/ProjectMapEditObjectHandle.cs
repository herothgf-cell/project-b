using UnityEngine;
// A handle contains one object only: Undo never serializes the map or its baseline.
public sealed class ProjectMapEditObjectHandle : ScriptableObject { public ProjectMapObject value; }

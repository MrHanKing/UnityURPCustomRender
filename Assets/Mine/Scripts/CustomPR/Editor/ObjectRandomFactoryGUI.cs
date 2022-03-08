using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectRandomFactory))]
public class ObjectRandomFactoryGUI : Editor
{
    ObjectRandomFactory targetScript;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        this.targetScript = target as ObjectRandomFactory;

        if (this.targetScript == null)
        {
            return;
        }

        if (GUILayout.Button("创建对象"))
        {
            this.targetScript.CreateOne();
        }
    }
}


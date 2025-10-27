using UnityEngine;
using UnityEditor;

public class RenameByIndex : EditorWindow
{
    string oldPrefix = "1_15.";
    string newPrefix = "9_";
    int oldStartIndex = 1;
    int oldEndIndex = 18;
    int newStartIndex = 0;

    [MenuItem("Tools/Rename By Index")]
    public static void ShowWindow()
    {
        GetWindow<RenameByIndex>("Rename By Index");
    }

    void OnGUI()
    {
        GUILayout.Label("Rename Objects by Index Range", EditorStyles.boldLabel);
        oldPrefix = EditorGUILayout.TextField("Old Prefix", oldPrefix);
        oldStartIndex = EditorGUILayout.IntField("Old Start Index", oldStartIndex);
        oldEndIndex = EditorGUILayout.IntField("Old End Index", oldEndIndex);
        newPrefix = EditorGUILayout.TextField("New Prefix", newPrefix);
        newStartIndex = EditorGUILayout.IntField("New Start Index", newStartIndex);

        if (GUILayout.Button("Rename"))
        {
            RenameObjects();
        }
    }

    void RenameObjects()
    {
        int newIndex = newStartIndex;

        for (int i = oldStartIndex; i <= oldEndIndex; i++)
        {
            string oldName = oldPrefix + i.ToString("D3"); // e.g., 1_15.001
            GameObject obj = GameObject.Find(oldName);
            if (obj != null)
            {
                Undo.RecordObject(obj, "Rename Object");
                obj.name = newPrefix + newIndex;
                newIndex++;
            }
            else
            {
                Debug.LogWarning("Object not found: " + oldName);
            }
        }

        Debug.Log("Renaming complete!");
    }
}

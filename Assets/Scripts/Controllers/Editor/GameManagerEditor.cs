using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


        EditorGUILayout.Space(10);

        Color originalColor = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Add Debug Creature", GUILayout.Height(30)))
        {
            var method = typeof(GameManager).GetMethod("AddDebugCreature",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(target, null);
        }
        GUI.backgroundColor = originalColor;

        // Set button color to a soft red (not "oczojebny")
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);

        if (GUILayout.Button("RESET SAVE DATA", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Reset Save Data",
                "Are you sure you want to delete all save data? This cannot be undone.",
                "Yes, Delete", "Cancel"))
            {
                GameManager manager = (GameManager)target;
                manager.ResetSaveData();
            }
        }

        // Restore original color
        GUI.backgroundColor = originalColor;
    }
}

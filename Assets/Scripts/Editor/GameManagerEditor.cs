using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gameManager = (GameManager)target;

        EditorGUILayout.Space(10);
        
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Add Debug Creature", GUILayout.Height(30)))
        {
            var method = typeof(GameManager).GetMethod("AddDebugCreature", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(gameManager, null);
        }
        GUI.backgroundColor = Color.white;
    }
}

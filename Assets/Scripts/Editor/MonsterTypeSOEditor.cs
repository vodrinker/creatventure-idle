using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonsterTypeSO))]
public class MonsterTypeSOEditor : Editor
{
    private SerializedProperty typeNameProp;
    private SerializedProperty iconProp;
    private SerializedProperty colorProp;
    private SerializedProperty damageMultipliersProp;

    private bool showDamageMultipliers = true;

    private Color removeButtonColor = new Color(0.9f, 0.6f, 0.6f);
    private Color addButtonColor = new Color(0.6f, 0.9f, 0.6f);

    private void OnEnable()
    {
        typeNameProp = serializedObject.FindProperty("typeName");
        iconProp = serializedObject.FindProperty("icon");
        colorProp = serializedObject.FindProperty("color");
        damageMultipliersProp = serializedObject.FindProperty("damageMultipliers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(typeNameProp);
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.PropertyField(colorProp);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Gameplay", EditorStyles.boldLabel);

        showDamageMultipliers = EditorGUILayout.Foldout(showDamageMultipliers, "Damage Multipliers", true, EditorStyles.foldoutHeader);
        if (showDamageMultipliers)
        {
            EditorGUI.indentLevel++;
            if (damageMultipliersProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No damage multipliers defined.", MessageType.Info);
            }
            for (int i = 0; i < damageMultipliersProp.arraySize; i++)
            {
                SerializedProperty element = damageMultipliersProp.GetArrayElementAtIndex(i);

                SerializedProperty typeAgainstProp = element.FindPropertyRelative("type");
                SerializedProperty multiplierProp = element.FindPropertyRelative("damageMultiplier");

                EditorGUILayout.BeginHorizontal();
                if (typeAgainstProp != null)
                {
                    EditorGUILayout.PropertyField(typeAgainstProp, GUIContent.none);
                }
                else
                {
                    EditorGUILayout.LabelField("Type: (Error finding property)");
                }

                GUILayout.Label("Multiplier:", GUILayout.Width(60));
                if (multiplierProp != null)
                {
                    EditorGUILayout.PropertyField(multiplierProp, GUIContent.none, GUILayout.Width(50));
                }
                else
                {
                    EditorGUILayout.LabelField("Multiplier: (Error)");
                }

                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = removeButtonColor;
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    damageMultipliersProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                GUI.backgroundColor = originalColor;
                EditorGUILayout.EndHorizontal();
            }

            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = addButtonColor;
            if (GUILayout.Button("Add Damage Multiplier"))
            {
                damageMultipliersProp.InsertArrayElementAtIndex(damageMultipliersProp.arraySize);
            }
            GUI.backgroundColor = defaultBgColor;
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}

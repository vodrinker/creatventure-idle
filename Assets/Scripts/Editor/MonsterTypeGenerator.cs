using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MonsterTypeGenerator : EditorWindow
{
    private string outputPath = "Assets/Scripts/Data/Resources/MonsterTypes";

    [MenuItem("Tools/Generate Monster Types")]
    public static void ShowWindow()
    {
        GetWindow<MonsterTypeGenerator>("Monster Type Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Monster Type Generator", EditorStyles.boldLabel);
        
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Generate All Types", GUILayout.Height(40)))
        {
            GenerateTypes();
        }
        
        EditorGUILayout.HelpBox(
            "This will create MonsterTypeSO assets for:\n" +
            "Normal, Water, Fire, Nature, Ground, Electric, Ice, Air, Psychic, Ghost\n\n" +
            "With damage multipliers based on the type effectiveness table.", 
            MessageType.Info);
    }

    private void GenerateTypes()
    {
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            string[] folders = outputPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        string[] typeNames = { "Normal", "Water", "Fire", "Nature", "Ground", "Electric", "Ice", "Air", "Psychic", "Ghost" };
        
        Dictionary<string, MonsterTypeSO> types = new Dictionary<string, MonsterTypeSO>();
        foreach (string typeName in typeNames)
        {
            string assetPath = $"{outputPath}/{typeName}.asset";
            MonsterTypeSO existing = AssetDatabase.LoadAssetAtPath<MonsterTypeSO>(assetPath);
            
            if (existing != null)
            {
                types[typeName] = existing;
            }
            else
            {
                MonsterTypeSO newType = ScriptableObject.CreateInstance<MonsterTypeSO>();
                newType.typeName = typeName;
                newType.color = GetColorForType(typeName);
                newType.damageMultipliers = new List<TypeDamageMultiplier>();
                
                AssetDatabase.CreateAsset(newType, assetPath);
                types[typeName] = newType;
            }
        }

        // Row = attacker, Column = defender
        // Format: types[attacker].damageMultipliers.Add(defender, multiplier)
        var multipliers = new Dictionary<string, Dictionary<string, float>>
        {
            { "Normal", new Dictionary<string, float> { { "Ghost", 0f } } },
            { "Water", new Dictionary<string, float> { { "Fire", 2f }, { "Ground", 1.5f } } },
            { "Fire", new Dictionary<string, float> { { "Nature", 2f }, { "Ice", 2f } } },
            { "Nature", new Dictionary<string, float> { { "Water", 2f }, { "Ground", 1.5f } } },
            { "Ground", new Dictionary<string, float> { { "Fire", 2f }, { "Ghost", 0f } } },
            { "Electric", new Dictionary<string, float> { { "Water", 2f }, { "Ground", 0f }, { "Psychic", 2f } } },
            { "Ice", new Dictionary<string, float> { { "Water", 1.5f }, { "Nature", 1.5f }, { "Ground", 1.5f } } },
            { "Air", new Dictionary<string, float> { { "Nature", 2f }, { "Ground", 2f } } },
            { "Psychic", new Dictionary<string, float> { { "Ground", 0f }, { "Ghost", 2f } } },
            { "Ghost", new Dictionary<string, float>() }
        };

        foreach (var attackerPair in multipliers)
        {
            string attackerName = attackerPair.Key;
            MonsterTypeSO attacker = types[attackerName];
            
            attacker.damageMultipliers.Clear();
            
            foreach (var defenderPair in attackerPair.Value)
            {
                string defenderName = defenderPair.Key;
                float multiplier = defenderPair.Value;
                
                var dmgMult = new TypeDamageMultiplier();
                var typeField = typeof(TypeDamageMultiplier).GetField("type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var multField = typeof(TypeDamageMultiplier).GetField("damageMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                typeField.SetValue(dmgMult, types[defenderName]);
                multField.SetValue(dmgMult, multiplier);
                
                attacker.damageMultipliers.Add(dmgMult);
            }
            
            EditorUtility.SetDirty(attacker);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Generated {typeNames.Length} MonsterTypeSO assets in {outputPath}");
        EditorUtility.DisplayDialog("Success", $"Generated {typeNames.Length} monster types!", "OK");
    }

    private Color GetColorForType(string typeName)
    {
        return typeName switch
        {
            "Normal" => new Color(0.66f, 0.66f, 0.47f),
            "Water" => new Color(0.39f, 0.56f, 0.94f),
            "Fire" => new Color(0.93f, 0.51f, 0.19f),
            "Nature" => new Color(0.47f, 0.78f, 0.30f),
            "Ground" => new Color(0.88f, 0.75f, 0.40f),
            "Electric" => new Color(0.97f, 0.82f, 0.17f),
            "Ice" => new Color(0.59f, 0.85f, 0.84f),
            "Air" => new Color(0.66f, 0.56f, 0.95f),
            "Psychic" => new Color(0.98f, 0.33f, 0.53f),
            "Ghost" => new Color(0.44f, 0.34f, 0.59f),
            _ => Color.white
        };
    }
}

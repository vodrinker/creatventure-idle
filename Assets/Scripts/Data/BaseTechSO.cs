using UnityEngine;
using System.Collections.Generic;

public abstract class BaseTechSO : ScriptableObject
{
    public string displayName;
    [TextArea(3, 10)]
    public string description;
    public Sprite icon;
    
    [HideInInspector]
    public string id; // Unique ID (auto-generated or name-based)

    [HideInInspector]
    public Vector2Int position;

    public List<BaseTechSO> connectedNodes = new List<BaseTechSO>();

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = name;
        }
    }
}

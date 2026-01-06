using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Technology", menuName = "Creatures/Technology")]
public class TechnologySO : ScriptableObject
{
    public string displayName;
    [TextArea(3, 10)]
    public string description;
    public Vector2Int position;
    public List<TechnologySO> connectedNodes = new List<TechnologySO>();
}

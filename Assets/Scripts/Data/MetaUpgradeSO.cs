using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Meta Upgrade", menuName = "Creatures/Technology/Meta Upgrade")]
public class MetaUpgradeSO : BaseTechSO
{
    [Header("Meta Upgrade Settings")]
    public int maxLevels = 5;
    
    // Base cost for level 1
    public int baseCost = 10;
    
    // Cost multiplier per level
    public float costMultiplier = 1.5f;

    [Header("Effects")]
    // Effects applied per level (e.g. Value * Level)
    // Or you can treat 'Value' as the base value per level.
    public List<TechEffect> effectsPerLevel = new List<TechEffect>();

    public int GetCostForLevel(int level)
    {
        // Example formula: Base * Multiplier ^ (Level - 1)
        if (level <= 1) return baseCost;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, level - 1));
    }
}

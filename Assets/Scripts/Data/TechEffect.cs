using UnityEngine;
using System;

public enum TechEffectType
{
    GlobalStatMultiplier,   // Global stats like Production, Click Strength, Heal Rate, Exp Gain
    CreatureStatMultiplier, // Damage, Health (Global or Per Element)
    UnlockFeature,          // Crafting, Auto-Battle, Speed 2x
    LimitIncrease,          // Captured Creatures Limit, Squad Size
    Economy,                // Reduce Upgrade Costs, specific item yields
}

[Serializable]
public struct TechEffect
{
    public TechEffectType type;
    
    // Value parameter (e.g., 0.1f for 10%)
    public float value;
    
    // Optional: Target Element/Type (if applicable)
    public MonsterTypeSO targetElement;
    
    // Optional: String parameter for Feature IDs or specific keys
    public string stringParam;
}

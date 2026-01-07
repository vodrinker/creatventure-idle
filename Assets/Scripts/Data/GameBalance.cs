using UnityEngine;

public static class GameBalance
{
    // --- Combat Settings ---
    public const float BattleInterval = 3f;
    public const float PassiveHealRate = 1f;

    /// <summary>
    /// Calculates exp gained when defeating an enemy of a specific level.
    /// Formula: 10 + EnemyLevel * 2
    /// </summary>
    public static int CalculateExpGain(int enemyLevel)
    {
        return 10 + enemyLevel * 2;
    }

    /// <summary>
    /// Calculates experience required to reach the next level.
    /// Formula: 100 * 1.3^(TargetLevel - 1)
    /// </summary>
    public static int CalculateExpRequiredForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float exp = 100f; // BaseLevelExp
        // Start from 2 because level 2 requires base exp (100), level 3 requires base * multiplier
        for (int i = 2; i < targetLevel; i++)
        {
            exp *= 1.3f; // LevelExpMultiplier
        }
        return Mathf.CeilToInt(exp);
    }

    /// <summary>
    /// Basic damage calculation.
    /// Formula: Attacker.Attack * TypeMultiplier
    /// </summary>
    public static float CalculateDamage(Creature attacker, Creature defender)
    {
        if (attacker == null || defender == null) return 0;

        float baseDamage = attacker.Attack;
        float multiplier = 1f;
        if (attacker.Type != null && defender.Type != null)
        {
            multiplier = attacker.Type.GetDamageMultiplier(defender.Type);
        }
        return baseDamage * multiplier;
    }


    // --- Economy Settings ---
    public const float ProductionMultiplierPerLevel = 1.15f;
    public const float UpgradeCostMultiplierPerLevel = 1.2f;

    /// <summary>
    /// Calculates node upgrade cost based on base cost and current level.
    /// Formula: BaseCost * 1.2^Level
    /// </summary>
    public static long CalculateNodeUpgradeCost(long baseCost, int currentLevel)
    {
        return (long)(baseCost * System.Math.Pow(UpgradeCostMultiplierPerLevel, currentLevel));
    }

    /// <summary>
    /// Calculates production rate for an item based on base amount and node level.
    /// Formula: BaseAmount * 1.15^Level
    /// </summary>
    public static float CalculateProductionRate(float baseAmount, int currentLevel)
    {
        return baseAmount * Mathf.Pow(ProductionMultiplierPerLevel, currentLevel);
    }


    // --- Progression Settings ---

    /// <summary>
    /// Calculates enemy level based on distance from center and selected adventure level.
    /// Formula: Distance + 1 + AdventureLevel
    /// </summary>
    public static int CalculateEnemyLevel(int distance, int adventureLevel)
    {
        return distance + 1 + adventureLevel;
    }

    /// <summary>
    /// Kills required to unlock the next adventure level on a node.
    /// Formula: (CurrentAdventureLevel + 1) * 10
    /// </summary>
    public static int CalculateRequiredKillsForAdventure(int currentAdventureLevel)
    {
        return (currentAdventureLevel + 1) * 10;
    }

    // --- Creature Stat Settings ---
    public const int BaseLevelExp = 100;
    public const float LevelExpMultiplier = 1.3f; // 30% more exp per level
    public const float StatMultiplierPerLevel = 1.1f; // 10% stats increase per level

    public static int CalculateCreatureLevel(int currentExp)
    {
        if (currentExp < BaseLevelExp) return 1;

        int level = 1;
        float requiredExp = BaseLevelExp;
        while (currentExp >= requiredExp)
        {
            level++;
            requiredExp *= LevelExpMultiplier;
        }
        return level;
    }

    public static float CalculateCreatureMaxHealth(float baseHealth, int level)
    {
        return baseHealth * Mathf.Pow(StatMultiplierPerLevel, level);
    }

    public static float CalculateCreatureAttack(float baseAttack, int level)
    {
        return baseAttack * Mathf.Pow(StatMultiplierPerLevel, level);
    }

    public static int CalculateExpForNextLevel(int currentLevel)
    {
        float requiredExp = BaseLevelExp;
        for (int i = 1; i < currentLevel; i++)
        {
            requiredExp *= LevelExpMultiplier;
        }
        return Mathf.CeilToInt(requiredExp * LevelExpMultiplier);
    }

    // --- Technology Settings ---
    public const long BaseTechPointCost = 1000;
    public const float TechPointCostMultiplier = 1.5f;

    public static long CalculateTechPointCost(int ownedPoints)
    {
        // Formula: 1000 * 1.5 ^ OwnedPoints
        return (long)(BaseTechPointCost * System.Math.Pow(TechPointCostMultiplier, ownedPoints));
    }
}

using System;
using UnityEngine;

[Serializable]
public class Creature
{
    public CreatureSO Definition { get; private set; }
    public MonsterTypeSO Type => Definition?.type;
    public int Exp { get; set; }
    public float CurrentHealth { get; private set; }

    private const int BaseLevelExp = 100;
    private const float LevelMultiplier = 1.3f;
    private const float StatMultiplier = 1.1f;

    public int Level
    {
        get
        {
            if (Exp < BaseLevelExp) return 1;

            int level = 1;
            float requiredExp = BaseLevelExp;
            while (Exp >= requiredExp)
            {
                level++;
                requiredExp *= LevelMultiplier;
            }
            return level;
        }
    }

    public float MaxHealth => Definition.baseHealth * Mathf.Pow(StatMultiplier, Level);
    public float Attack => Definition.baseAttack * Mathf.Pow(StatMultiplier, Level);
    public bool IsAlive => CurrentHealth > 0;
    public bool IsFullHealth => CurrentHealth >= MaxHealth;

    public Creature(CreatureSO definition, int exp = 0)
    {
        Definition = definition;
        Exp = exp;
        CurrentHealth = MaxHealth;
    }

    public static Creature CreateAtLevel(CreatureSO definition, int targetLevel)
    {
        int exp = CalculateExpForLevel(targetLevel);
        return new Creature(definition, exp);
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
    }

    public int GetExpForNextLevel()
    {
        float requiredExp = BaseLevelExp;
        for (int i = 1; i < Level; i++)
        {
            requiredExp *= LevelMultiplier;
        }
        return Mathf.CeilToInt(requiredExp * LevelMultiplier);
    }

    private static int CalculateExpForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float exp = 0;
        float required = BaseLevelExp;
        for (int i = 1; i < targetLevel; i++)
        {
            exp = required;
            required *= LevelMultiplier;
        }
        return Mathf.CeilToInt(exp);
    }
}

using System;
using UnityEngine;

[Serializable]
public class Creature
{
    public CreatureSO Definition { get; private set; }
    public MonsterTypeSO Type => Definition?.type;
    public int Exp { get; set; }
    public float CurrentHealth { get; private set; }



    public int Level
    {
        get
        {
            return GameBalance.CalculateCreatureLevel(Exp);
        }
    }

    public float MaxHealth => GameBalance.CalculateCreatureMaxHealth(Definition.baseHealth, Level);
    public float Attack => GameBalance.CalculateCreatureAttack(Definition.baseAttack, Level);
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
        int exp = GameBalance.CalculateExpRequiredForLevel(targetLevel);
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
        return GameBalance.CalculateExpForNextLevel(Level);
    }


}

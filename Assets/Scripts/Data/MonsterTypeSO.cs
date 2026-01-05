using UnityEngine;
using System.Collections.Generic;

//[System.Serializable]
//public struct StatusEffectChancePair
//{
//    public StatusEffectSO Effect;

//    [Range(0f, 1f)]
//    public float Chance;
//}

[System.Serializable]
public class TypeDamageMultiplier
{
    [SerializeField] private MonsterTypeSO type;
    [SerializeField] private float damageMultiplier;

    public MonsterTypeSO Type => type;
    public float DamageMultiplier => damageMultiplier;
}

[CreateAssetMenu(fileName = "New Monster Type", menuName = "Monster Type")]
public class MonsterTypeSO : ScriptableObject
{
    public string typeName;
    public Sprite icon;
    public Color color;

    //[Header("Audio")]
    //public SoundSO damageSound;

    [Header("Gameplay")]
    public List<TypeDamageMultiplier> damageMultipliers;

    //public List<StatusEffectChancePair> AssociatedStatusEffects;
    //public List<StatusEffectSO> ImmuneToEffects;

    public float GetDamageMultiplier(MonsterTypeSO otherType)
    {
        foreach (TypeDamageMultiplier multiplier in damageMultipliers)
        {
            if (multiplier.Type == otherType)
            {
                return multiplier.DamageMultiplier;
            }
        }
        return 1f;
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "NewCreature", menuName = "Game/Creature")]
public class CreatureSO : ScriptableObject
{
    public Sprite sprite;
    public string creatureName;
    public MonsterTypeSO type;
    public int baseHealth;
    public int baseAttack;
}

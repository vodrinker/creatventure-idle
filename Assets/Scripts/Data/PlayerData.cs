using System.Collections.Generic;

public class PlayerData
{
    public int Money { get; set; }
    public Dictionary<ItemSO, int> Items { get; private set; }
    public List<Creature> Creatures { get; private set; }

    public PlayerData()
    {
        Money = 0;
        Items = new Dictionary<ItemSO, int>();
        Creatures = new List<Creature>();
    }

    public void AddItem(ItemSO item, int amount)
    {
        if (Items.ContainsKey(item))
        {
            Items[item] += amount;
        }
        else
        {
            Items.Add(item, amount);
        }
    }

    public void AddCreature(Creature creature)
    {
        Creatures.Add(creature);
    }
}

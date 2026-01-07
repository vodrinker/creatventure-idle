using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSave
{
    public long money;
    public int traps; // New field
    public List<ItemCount> items;
    public List<CreatureSave> creatures;
    public List<CreatureSave> catchableCreatures; // New field
}

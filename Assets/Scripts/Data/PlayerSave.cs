using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSave
{
    public long money;
    public List<ItemCount> items;
    public List<CreatureSave> creatures;
}

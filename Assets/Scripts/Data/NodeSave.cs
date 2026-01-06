using System;
using System.Collections.Generic;

[Serializable]
public class NodeSave
{
    public int x;
    public int y;
    public bool isVisible;
    public bool isOwned;
    public int productionLevel;
    public int adventureLevel;
    public int maxUnlockedAdventureLevel;
    public int adventureProgress;
    public long baseCost;
    public long upgradeCost;
    public List<ProductionProgressSave> productionProgresses;
    public int assignedCreatureIndex = -1;
}

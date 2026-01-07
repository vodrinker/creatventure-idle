using System;
using System.Collections.Generic;

[Serializable]
public class TechSave
{
    // Game Techs (Run-specific)
    public int ownedTechPoints;
    public int totalTechPointsBought; // Track lifetime purchases for cost scaling
    public List<string> unlockedGameTechs = new List<string>();

    // Meta Upgrades (Persistent)
    public long metaCoins;
    public List<UpgradeLevelPair> metaUpgrades = new List<UpgradeLevelPair>();
}

[Serializable]
public struct UpgradeLevelPair
{
    public string id;
    public int level;

    public UpgradeLevelPair(string id, int level)
    {
        this.id = id;
        this.level = level;
    }
}

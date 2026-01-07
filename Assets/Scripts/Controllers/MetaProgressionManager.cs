using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance { get; private set; }

    private TechSave saveData;
    private Dictionary<string, MetaUpgradeSO> allMetaUpgrades = new Dictionary<string, MetaUpgradeSO>();
    private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadAllResources();
    }

    private void LoadAllResources()
    {
        var upgrades = Resources.LoadAll<MetaUpgradeSO>("Technologies/Meta");
        foreach (var u in upgrades)
        {
            if (!allMetaUpgrades.ContainsKey(u.name))
            {
                allMetaUpgrades.Add(u.name, u);
            }
        }
    }

    public void LoadData(TechSave save)
    {
        this.saveData = save;
        
        // Convert List to Dictionary for fast lookup
        upgradeLevels.Clear();
        if (this.saveData.metaUpgrades != null)
        {
            foreach (var pair in this.saveData.metaUpgrades)
            {
                upgradeLevels[pair.id] = pair.level;
            }
        }
    }

    public void SyncSaveData()
    {
        if (saveData == null) return;
        saveData.metaUpgrades.Clear();
        foreach (var kv in upgradeLevels)
        {
            saveData.metaUpgrades.Add(new UpgradeLevelPair(kv.Key, kv.Value));
        }
    }

    // --- Actions ---

    public int GetLevel(string upgradeId)
    {
        return upgradeLevels.ContainsKey(upgradeId) ? upgradeLevels[upgradeId] : 0;
    }

    public bool CanUpgrade(MetaUpgradeSO upgrade)
    {
        if (upgrade == null || saveData == null) return false;
        
        int currentLevel = GetLevel(upgrade.name);
        if (currentLevel >= upgrade.maxLevels) return false;

        long cost = upgrade.GetCostForLevel(currentLevel + 1);
        if (saveData.metaCoins < cost) return false;

        // Check Parents
        // Similar logic: need at least one parent unlocked/upgraded>0?
        // Or specific dependency? 
        // For meta tree, usually just reaching the node is enough (parent level >= 1).
        return HasUnlockedParent(upgrade);
    }

    public void UpgradeTech(MetaUpgradeSO upgrade)
    {
        if (!CanUpgrade(upgrade)) return;

        int currentLevel = GetLevel(upgrade.name);
        long cost = upgrade.GetCostForLevel(currentLevel + 1);

        saveData.metaCoins -= cost;
        upgradeLevels[upgrade.name] = currentLevel + 1;
        
        SyncSaveData(); // update the list
    }

    // --- Helpers ---

    private bool HasUnlockedParent(MetaUpgradeSO target)
    {
        bool hasParents = false;
        foreach (var kv in allMetaUpgrades)
        {
            var tech = kv.Value;
            if (tech.connectedNodes.Contains(target))
            {
                hasParents = true;
                if (GetLevel(tech.name) > 0) return true;
            }
        }
        return !hasParents;
    }

    // --- Effects ---

    public float GetTotalEffectValue(TechEffectType type, string param = null)
    {
        if (saveData == null) return 0f;

        float total = 0f;
        foreach (var kv in upgradeLevels)
        {
            string id = kv.Key;
            int level = kv.Value;
            if (level <= 0) continue;

            if (allMetaUpgrades.TryGetValue(id, out var upgrade))
            {
                foreach (var effect in upgrade.effectsPerLevel)
                {
                    if (effect.type == type && effect.stringParam == param)
                    {
                        // Effect Value * Level
                        total += effect.value * level;
                    }
                }
            }
        }
        return total;
    }
}

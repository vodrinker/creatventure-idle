using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TechManager : MonoBehaviour
{
    public static TechManager Instance { get; private set; }

    private TechSave saveData;
    private Dictionary<string, GameTechSO> allGameTechs = new Dictionary<string, GameTechSO>();

    // Cached effects for performance (optional, but good for frequent lookups)
    // For now, calculating on fly or caching simple values.

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
        var techs = Resources.LoadAll<GameTechSO>("Technologies/Game");
        foreach (var t in techs)
        {
            if (!allGameTechs.ContainsKey(t.name))
            {
                allGameTechs.Add(t.name, t);
            }
        }
    }

    public void LoadData(TechSave save)
    {
        this.saveData = save;
        if (this.saveData.unlockedGameTechs == null)
            this.saveData.unlockedGameTechs = new List<string>();
    }

    public TechSave GetSaveData()
    {
        return saveData; // Reference is modified directly
    }

    // --- Actions ---

    public bool CanBuyTechPoint()
    {
        if (saveData == null) return false;
        long cost = GameBalance.CalculateTechPointCost(saveData.totalTechPointsBought);
        return GameManager.Instance.Player.Money >= cost;
    }

    public void BuyTechPoint()
    {
        if (!CanBuyTechPoint()) return;

        long cost = GameBalance.CalculateTechPointCost(saveData.totalTechPointsBought);
        GameManager.Instance.Player.Money -= cost;
        saveData.ownedTechPoints++;
        saveData.totalTechPointsBought++;

        GameManager.Instance.TriggerPlayerDataUpdate();
    }

    public bool CanUnlock(GameTechSO tech)
    {
        if (tech == null || saveData == null) return false;
        if (IsUnlocked(tech.name)) return false;
        if (saveData.ownedTechPoints < 1) return false; // Basic cost 1 TP

        // Check parents
        if (tech.connectedNodes != null && tech.connectedNodes.Count > 0)
        {
            // If it has incoming connections (parents), at least one must be unlocked? 
            // The SO 'connectedNodes' usually defines CHILDREN (outgoing).
            // We need to find if any parent connects TO this node.
            // Or usually the graph implies direction: Start -> Child.
            // If specific node has no parents (root), it's unlockable?
            // "connectedNodes" field in BaseTechSO: "public List<BaseTechSO> connectedNodes".
            // Typically this is "Forward" connection.
            // So to unlock B, you need A if A->B.
            // Determining parents requires reverse lookup.
            return HasUnlockedParent(tech);
        }

        // No parents? Root node?
        // We need a way to define Root nodes. Position 0,0?
        return true;
    }

    public void UnlockTech(GameTechSO tech)
    {
        if (!CanUnlock(tech)) return;

        saveData.ownedTechPoints--;
        saveData.unlockedGameTechs.Add(tech.name);

        // Notify?
    }

    public bool IsUnlocked(string techId)
    {
        return saveData != null && saveData.unlockedGameTechs.Contains(techId);
    }

    // --- Helpers ---

    private bool HasUnlockedParent(GameTechSO target)
    {
        // Expensive check: iterate all techs to find who points to target
        // Optimization: Build parent cache on load.
        bool hasParents = false;
        foreach (var kv in allGameTechs)
        {
            var tech = kv.Value;
            if (tech.connectedNodes.Contains(target))
            {
                hasParents = true;
                if (IsUnlocked(tech.name)) return true;
            }
        }
        // If no parents found, it's a root node, so it's unlockable (if we treat orphans as roots)
        return !hasParents;
    }

    // --- Effects ---

    public float GetTotalEffectValue(TechEffectType type, string param = null)
    {
        if (saveData == null) return 0f;

        float total = 0f;
        foreach (var id in saveData.unlockedGameTechs)
        {
            if (allGameTechs.TryGetValue(id, out var tech))
            {
                foreach (var effect in tech.effects)
                {
                    if (effect.type == type && effect.stringParam == param)
                    {
                        total += effect.value;
                    }
                }
            }
        }
        return total;
    }
}

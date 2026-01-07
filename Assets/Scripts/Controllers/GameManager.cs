using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Map Generation")]
    [SerializeField] private int mapWidth = 7;

    [SerializeField] private int mapHeight = 11;
    [SerializeField] private NodeDefinitionSO[] nodeDefinitionPool;
    [SerializeField] private NodeDefinitionSO centerNodeDefinition;
    [SerializeField] private Sprite lockedNodeSprite;

    [Header("Debug Creature")]
    [SerializeField] private CreatureSO debugCreatureSO;
    [SerializeField] private int debugCreatureLevel = 1;

    public Dictionary<Vector2Int, NodeData> Map { get; private set; }
    public PlayerData Player { get; private set; }

    public event Action OnPlayerDataUpdated;
    public event Action OnCreaturesUpdated;
    public event Action<NodeData> OnNodeUpdated;

    public void TriggerPlayerDataUpdate()
    {
        OnPlayerDataUpdated?.Invoke();
    }

    private const string SaveKey = "game_save_v1";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GetComponent<TechManager>() == null) gameObject.AddComponent<TechManager>();
        if (GetComponent<MetaProgressionManager>() == null) gameObject.AddComponent<MetaProgressionManager>();

        InitializeGame();

        // subscribe to save on changes
        OnPlayerDataUpdated += SaveGame;
        OnNodeUpdated += (n) => SaveGame();
    }

    private const float BattleInterval = GameBalance.BattleInterval;
    private const float HealRate = GameBalance.PassiveHealRate;

    private void Start()
    {
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        ProcessProduction(deltaTime);
        ProcessBattles(deltaTime);
    }

    private void InitializeGame()
    {
        var mapGenerator = new MapGenerator();
        Map = mapGenerator.GenerateMap(mapWidth, mapHeight, nodeDefinitionPool, centerNodeDefinition);
        Player = new PlayerData();

        if (PlayerPrefs.HasKey(SaveKey))
        {
            // create a PlayerData instance, then load saved values
            Player = new PlayerData();
            LoadGame();

            // Spawn enemies for already owned nodes if missing (because enemies are not saved)
            foreach (var node in Map.Values)
            {
                if (node.isOwned && node.Enemy == null)
                {
                    SpawnEnemyForNode(node);
                }
            }
        }
        else
        {
            Player = new PlayerData();
            // default: unlock center
            UnlockNode(GetNodeAt(Vector2Int.zero));
            SaveGame();
        }

        // Ensure Tech Managers have data (even if empty)
        if (TechManager.Instance.GetSaveData() == null)
        {
            var newTechSave = new TechSave();
            TechManager.Instance.LoadData(newTechSave);
            MetaProgressionManager.Instance.LoadData(newTechSave);
        }
    }

    private void ProcessProduction(float deltaTime)
    {
        bool itemsProduced = false;
        foreach (var node in Map.Values.Where(n => n.isOwned))
        {
            foreach (var production in node.productionProgresses)
            {
                // New formula: base * 1.15^lvl
                float productionRate = GameBalance.CalculateProductionRate(production.baseAmount, node.productionLevel);

                // Apply Tech Multiplier
                float techMult = 1f + TechManager.Instance.GetTotalEffectValue(TechEffectType.GlobalStatMultiplier, "Production");
                productionRate *= techMult;

                production.currentProductionProgress += productionRate * deltaTime;

                if (production.currentProductionProgress >= 1)
                {
                    int producedAmount = Mathf.FloorToInt(production.currentProductionProgress);
                    Player.AddItem(production.item, producedAmount);
                    production.currentProductionProgress -= producedAmount;
                    itemsProduced = true;
                }
            }
        }
        if (itemsProduced)
        {
            OnPlayerDataUpdated?.Invoke();
        }
    }

    private void ProcessBattles(float deltaTime)
    {
        foreach (var node in Map.Values.Where(n => n.isOwned))
        {
            if (node.AssignedCreature == null) continue;

            if (node.IsHealing)
            {
                node.AssignedCreature.Heal(HealRate * deltaTime);
                if (node.AssignedCreature.IsFullHealth)
                {
                    node.IsHealing = false;
                    SpawnEnemyForNode(node);
                }
                OnNodeUpdated?.Invoke(node);
                continue;
            }

            if (node.Enemy == null)
            {
                SpawnEnemyForNode(node);
                continue;
            }

            if (!node.Enemy.IsAlive)
            {
                HandleEnemyDeath(node);
                continue;
            }

            node.BattleTimer += deltaTime;
            if (node.BattleTimer >= BattleInterval)
            {
                node.BattleTimer -= BattleInterval;

                float playerDamage = CalculateDamage(node.AssignedCreature, node.Enemy);
                float enemyDamage = CalculateDamage(node.Enemy, node.AssignedCreature);

                node.Enemy.TakeDamage(playerDamage);
                node.AssignedCreature.TakeDamage(enemyDamage);

                if (!node.Enemy.IsAlive)
                {
                    HandleEnemyDeath(node);
                }
                else
                {
                    OnNodeUpdated?.Invoke(node);
                }
            }
            else if (!node.AssignedCreature.IsAlive)
            {
                node.IsHealing = true;
            }

            OnNodeUpdated?.Invoke(node);
        }
    }


    private float CalculateDamage(Creature attacker, Creature defender)
    {
        return GameBalance.CalculateDamage(attacker, defender);
    }

    private void SpawnEnemyForNode(NodeData node)
    {
        if (node.PossibleCreatures == null || node.PossibleCreatures.Count == 0) return;

        var randomCreatureSO = node.PossibleCreatures[UnityEngine.Random.Range(0, node.PossibleCreatures.Count)];
        node.Enemy = Creature.CreateAtLevel(randomCreatureSO, node.EnemyLevel);
        node.BattleTimer = 0f;
        node.IsHealing = false;
    }

    // Duplicate SellItem removed. The correct one is below.

    public NodeData GetNodeAt(Vector2Int coordinates)
    {
        Map.TryGetValue(coordinates, out NodeData node);
        return node;
    }

    public List<NodeData> GetNeighborsOf(NodeData node)
    {
        var neighbors = new List<NodeData>();
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;

                var neighborCoords = node.Coordinates + new Vector2Int(x, y);
                var neighborNode = GetNodeAt(neighborCoords);
                if (neighborNode != null)
                {
                    neighbors.Add(neighborNode);
                }
            }
        }
        return neighbors;
    }

    private void UnlockNeighborVisibility(Vector2Int targetCoords)
    {
        var neighbor = GetNodeAt(targetCoords);
        if (neighbor != null)
        {
            neighbor.isVisible = true;
            OnNodeUpdated?.Invoke(neighbor);
        }
    }

    public bool TryUnlockNode(NodeData node)
    {
        if (node == null || node.isOwned) return false;
        long cost = node.UpgradeCost; // At lvl 0 cost is baseCost
        if (Player.Money >= cost)
        {
            Player.Money -= cost;
            node.isOwned = true;
            node.isVisible = true;

            // Reveal neighbors
            int x = node.Coordinates.x;
            int y = node.Coordinates.y;
            UnlockNeighborVisibility(new Vector2Int(x + 1, y));
            UnlockNeighborVisibility(new Vector2Int(x - 1, y));
            UnlockNeighborVisibility(new Vector2Int(x, y + 1));
            UnlockNeighborVisibility(new Vector2Int(x, y - 1));

            SpawnEnemyForNode(node);

            OnNodeUpdated?.Invoke(node);
            OnPlayerDataUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public bool TryUpgradeNode(NodeData node)
    {
        if (node == null || !node.isOwned) return false;
        long cost = node.UpgradeCost;

        if (Player.Money >= cost)
        {
            Player.Money -= cost;
            node.productionLevel++;
            OnNodeUpdated?.Invoke(node);
            OnPlayerDataUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public void SellItem(ItemSO item, int amount)
    {
        if (Player.Items.ContainsKey(item) && Player.Items[item] >= amount)
        {
            Player.Items[item] -= amount;
            if (Player.Items[item] == 0)
            {
                Player.Items.Remove(item);
            }
            Player.Money += item.value * amount;
            OnPlayerDataUpdated?.Invoke();
        }
    }

    private void UnlockNode(NodeData node)
    {
        if (node == null) return;

        node.isOwned = true;
        node.isVisible = true;
        SpawnEnemyForNode(node);
        OnNodeUpdated?.Invoke(node);

        foreach (var neighbor in GetNeighborsOf(node))
        {
            if (!neighbor.isVisible)
            {
                neighbor.isVisible = true;
                OnNodeUpdated?.Invoke(neighbor);
            }
        }
    }

    public Sprite GetLockedNodeSprite()
    {
        return lockedNodeSprite;
    }

    // --- Save / Load implementation ---
    private void SaveGame()
    {
        if (Player == null)
        {
            Debug.LogWarning("GameManager: SaveGame called but Player is null.");
            return;
        }
        var save = new SaveData();
        save.player = new PlayerSave();
        save.player.money = Player.Money;
        save.player.traps = Player.Traps;
        save.player.items = new List<ItemCount>();
        if (Player.Items != null)
        {
            foreach (var kv in Player.Items)
            {
                if (kv.Key == null) continue;
                save.player.items.Add(new ItemCount { itemName = kv.Key.itemName, amount = kv.Value });
            }
        }

        save.player.creatures = new List<CreatureSave>();
        if (Player.Creatures != null)
        {
            foreach (var c in Player.Creatures)
            {
                if (c.Definition == null) continue;
                save.player.creatures.Add(new CreatureSave
                {
                    creatureName = c.Definition.creatureName,
                    typeName = c.Type != null ? c.Type.typeName : "",
                    exp = c.Exp
                });
            }
        }

        save.player.catchableCreatures = new List<CreatureSave>();
        if (Player.CatchableCreatures != null)
        {
            foreach (var c in Player.CatchableCreatures)
            {
                if (c.Definition == null) continue;
                save.player.catchableCreatures.Add(new CreatureSave
                {
                    creatureName = c.Definition.creatureName,
                    typeName = c.Type != null ? c.Type.typeName : "",
                    exp = c.Exp
                });
            }
        }

        save.nodes = new List<NodeSave>();
        foreach (var kv in Map)
        {
            var n = kv.Value;
            var ns = new NodeSave();
            ns.x = n.Coordinates.x;
            ns.y = n.Coordinates.y;
            ns.isOwned = n.isOwned;
            ns.isVisible = n.isVisible;
            ns.productionLevel = n.productionLevel;
            ns.adventureLevel = n.adventureLevel;
            ns.maxUnlockedAdventureLevel = n.MaxUnlockedAdventureLevel;
            ns.adventureProgress = n.AdventureProgress;
            ns.baseCost = n.baseCost;
            ns.upgradeCost = n.UpgradeCost;
            ns.productionProgresses = new List<ProductionProgressSave>();
            if (n.productionProgresses != null)
            {
                foreach (var p in n.productionProgresses)
                {
                    if (p.item == null) continue;
                    ns.productionProgresses.Add(new ProductionProgressSave
                    {
                        itemName = p.item.itemName,
                        currentProductionProgress = p.currentProductionProgress,
                        baseAmount = p.baseAmount
                    });
                }
            }
            ns.assignedCreatureIndex = n.AssignedCreature != null
                ? Player.Creatures.IndexOf(n.AssignedCreature)
                : -1;
            save.nodes.Add(ns);
            save.nodes.Add(ns);
        }

        // Save Tech Data
        MetaProgressionManager.Instance.SyncSaveData();
        save.tech = TechManager.Instance.GetSaveData();

        string json = JsonUtility.ToJson(save);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;
        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return;

        var save = JsonUtility.FromJson<SaveData>(json);
        if (save == null)
        {
            Debug.LogWarning("GameManager: failed to parse save JSON");
            return;
        }

        Player.Money = save.player != null ? save.player.money : 0;
        if (Player.Items != null)
        {
            Player.Items.Clear();
        }
        if (save.player != null && save.player.items != null)
        {
            var keptItems = new List<ItemCount>();
            bool modified = false;
            foreach (var it in save.player.items)
            {
                var itemRef = FindItemByName(it.itemName);
                if (itemRef != null)
                {
                    Player.AddItem(itemRef, it.amount);
                    keptItems.Add(it);
                }
                else
                {
                    modified = true;
                }
            }

            if (modified)
            {
                save.player.items = keptItems;
                string newJson = JsonUtility.ToJson(save);
                PlayerPrefs.SetString(SaveKey, newJson);
                PlayerPrefs.Save();
            }
        }

        if (save.player != null && save.player.creatures != null)
        {
            foreach (var cs in save.player.creatures)
            {
                var creatureSO = FindCreatureByName(cs.creatureName);
                if (creatureSO == null) continue;
                var creature = new Creature(creatureSO, cs.exp);
                Player.AddCreature(creature);
            }
        }

        Player.Traps = save.player != null ? save.player.traps : 1;
        // Default to 1 if 0 and new save? Logic says "Start with 1". 
        // If loading old save, traps might be 0 (default int). 
        // We should handle migration or just give 1 if 0? 
        // Let's assume 0 is valid (used up). But for migration, maybe check if new field?
        // Simpler: if save.player.traps is 0, it is 0. 
        // If it's a fresh game, InitializeGame sets it to 1 via new PlayerData().

        if (save.player != null && save.player.catchableCreatures != null)
        {
            foreach (var cs in save.player.catchableCreatures)
            {
                var creatureSO = FindCreatureByName(cs.creatureName);
                if (creatureSO == null) continue;
                var creature = new Creature(creatureSO, cs.exp);
                Player.CatchableCreatures.Add(creature);
            }
        }

        if (save.nodes != null)
        {
            foreach (var ns in save.nodes)
            {
                var coords = new Vector2Int(ns.x, ns.y);
                var node = GetNodeAt(coords);
                if (node == null) continue;
                node.isVisible = ns.isVisible;
                node.isOwned = ns.isOwned;
                node.productionLevel = ns.productionLevel;
                node.adventureLevel = ns.adventureLevel;
                node.MaxUnlockedAdventureLevel = ns.maxUnlockedAdventureLevel;
                node.AdventureProgress = ns.adventureProgress;
                node.adventureLevel = ns.adventureLevel; // Ensure loaded level is set
                node.adventureLevel = Mathf.Clamp(node.adventureLevel, 0, node.MaxUnlockedAdventureLevel); // Safety clamp
                node.baseCost = ns.baseCost;
                if (node.Coordinates == Vector2Int.zero && node.baseCost < 1)
                {
                    node.baseCost = 100;
                }

                if (ns.productionProgresses != null && node.productionProgresses != null)
                {
                    foreach (var pSave in ns.productionProgresses)
                    {
                        var prod = node.productionProgresses.FirstOrDefault(pp => pp.item != null && pp.item.itemName == pSave.itemName);
                        if (prod != null)
                        {
                            prod.currentProductionProgress = pSave.currentProductionProgress;
                            prod.baseAmount = pSave.baseAmount;
                        }
                    }
                }

                if (ns.assignedCreatureIndex >= 0 && ns.assignedCreatureIndex < Player.Creatures.Count)
                {
                    node.AssignedCreature = Player.Creatures[ns.assignedCreatureIndex];
                }

                OnNodeUpdated?.Invoke(node);
            }
        }

        OnPlayerDataUpdated?.Invoke();

        // Load Tech Data
        if (save.tech != null)
        {
            TechManager.Instance.LoadData(save.tech);
            MetaProgressionManager.Instance.LoadData(save.tech);
        }
        else
        {
            var newTechSave = new TechSave();
            TechManager.Instance.LoadData(newTechSave);
            MetaProgressionManager.Instance.LoadData(newTechSave);
        }
    }

    private ItemSO FindItemByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (nodeDefinitionPool != null)
        {
            foreach (var nd in nodeDefinitionPool)
            {
                if (nd == null || nd.productionItems == null) continue;
                foreach (var pi in nd.productionItems)
                {
                    if (pi.item != null && pi.item.itemName == name) return pi.item;
                }
            }
        }
        if (centerNodeDefinition != null && centerNodeDefinition.productionItems != null)
        {
            foreach (var pi in centerNodeDefinition.productionItems)
            {
                if (pi.item != null && pi.item.itemName == name) return pi.item;
            }
        }
        var loaded = Resources.LoadAll<ItemSO>("Items");
        if (loaded != null)
        {
            foreach (var li in loaded)
            {
                if (li != null && li.itemName == name) return li;
            }
        }

        return null;
    }

    private CreatureSO FindCreatureByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var loaded = Resources.LoadAll<CreatureSO>("Creatures");
        if (loaded != null)
        {
            foreach (var c in loaded)
            {
                if (c != null && c.creatureName == name) return c;
            }
        }
        return null;
    }

    private MonsterTypeSO FindMonsterTypeByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var loaded = Resources.LoadAll<MonsterTypeSO>("MonsterTypes");
        if (loaded != null)
        {
            foreach (var t in loaded)
            {
                if (t != null && t.typeName == name) return t;
            }
        }
        return null;
    }

    public void AssignCreatureToNode(NodeData node, Creature creature)
    {
        if (node == null || creature == null || !node.isOwned) return;

        foreach (var n in Map.Values)
        {
            if (n.AssignedCreature == creature)
            {
                n.AssignedCreature = null;
                OnNodeUpdated?.Invoke(n);
            }
        }

        node.AssignedCreature = creature;
        OnNodeUpdated?.Invoke(node);
    }

    public void RemoveCreatureFromNode(NodeData node)
    {
        if (node == null || node.AssignedCreature == null) return;
        node.AssignedCreature = null;
        OnNodeUpdated?.Invoke(node);
    }

    public List<Creature> GetUnassignedCreatures()
    {
        var assignedCreatures = Map.Values
            .Where(n => n.AssignedCreature != null)
            .Select(n => n.AssignedCreature)
            .ToHashSet();
        return Player.Creatures.Where(c => !assignedCreatures.Contains(c)).ToList();
    }

    [ContextMenu("Add Debug Creature")]
    private void AddDebugCreature()
    {
        if (debugCreatureSO == null)
        {
            Debug.LogWarning("GameManager: debugCreatureSO is not assigned.");
            return;
        }
        int exp = CalculateExpForLevel(debugCreatureLevel);
        var creature = new Creature(debugCreatureSO, exp);
        Player.AddCreature(creature);
        OnCreaturesUpdated?.Invoke();
        Debug.Log($"Added creature: {debugCreatureSO.creatureName} at level {creature.Level}");
    }

    private int CalculateExpForLevel(int targetLevel)
    {
        return GameBalance.CalculateExpRequiredForLevel(targetLevel);
    }

    public void AddCatchableCreature(Creature enemy)
    {
        if (enemy == null || enemy.Definition == null) return;

        var existing = Player.CatchableCreatures.FirstOrDefault(c => c.Definition == enemy.Definition);
        if (existing == null)
        {
            // Add new species. Clone to avoid reference issues with node.Enemy which gets reused/deleted.
            // CreateAtLevel sets health to max.
            Player.CatchableCreatures.Add(Creature.CreateAtLevel(enemy.Definition, enemy.Level));
        }
        else
        {
            // Update if higher level
            if (enemy.Level > existing.Level)
            {
                Player.CatchableCreatures.Remove(existing);
                Player.CatchableCreatures.Add(Creature.CreateAtLevel(enemy.Definition, enemy.Level));
            }
        }
    }

    public bool TryCatchCreature(Creature catchable)
    {
        if (catchable == null) return false;
        if (Player.Traps <= 0) return false;

        Player.Traps--;
        // Add to owned collection
        // Create fresh instance to distinguish from the "Catchable" template
        var newCreature = Creature.CreateAtLevel(catchable.Definition, catchable.Level);
        Player.AddCreature(newCreature);

        // Remove from catchable list
        Player.CatchableCreatures.Remove(catchable);

        OnCreaturesUpdated?.Invoke();
        OnPlayerDataUpdated?.Invoke();
        return true;
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
    private void HandleEnemyDeath(NodeData node)
    {
        // Add to catchable list (Max level rule)
        if (node.Enemy != null)
        {
            AddCatchableCreature(node.Enemy);
        }

        // Logic for assigned creature (Exp, Progress)
        if (node.AssignedCreature != null)
        {
            int expGain = GameBalance.CalculateExpGain(node.EnemyLevel);
            node.AssignedCreature.Exp += expGain;

            // Progress System
            if (node.adventureLevel == node.MaxUnlockedAdventureLevel)
            {
                node.AdventureProgress++;
                // Target: (level + 1) * 10. E.g. Lvl 0 -> 10 kills. Lvl 1 -> 20 kills.
                int requiredKills = GameBalance.CalculateRequiredKillsForAdventure(node.adventureLevel);
                if (node.AdventureProgress >= requiredKills)
                {
                    node.MaxUnlockedAdventureLevel++;
                    node.AdventureProgress = 0;
                }
            }
            node.IsHealing = true;
        }
        else
        {
            // Manual Kill (No assigned creature) -> Instant Respawn to allow farming
            SpawnEnemyForNode(node);
        }

        OnCreaturesUpdated?.Invoke();
        OnNodeUpdated?.Invoke(node);
    }
    public void ResetSaveData()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("Save data reset. Reloading scene...");
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
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

    private const string SaveKey = "game_save_v1";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeGame();

        // subscribe to save on changes
        OnPlayerDataUpdated += SaveGame;
        OnNodeUpdated += (n) => SaveGame();
    }

    private const float BattleInterval = 3f;
    private const float HealRate = 1f;

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
    }

    private void ProcessProduction(float deltaTime)
    {
        bool itemsProduced = false;
        foreach (var node in Map.Values.Where(n => n.isOwned))
        {
            foreach (var production in node.productionProgresses)
            {
                production.currentProductionProgress += production.baseAmount * node.productionLevel * deltaTime;
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
                    int expGain = 10 + node.EnemyLevel * 2;
                    node.AssignedCreature.Exp += expGain;
                    node.IsHealing = true;
                    OnCreaturesUpdated?.Invoke();
                }
                else if (!node.AssignedCreature.IsAlive)
                {
                    node.IsHealing = true;
                }

                OnNodeUpdated?.Invoke(node);
            }
        }
    }

    private float CalculateDamage(Creature attacker, Creature defender)
    {
        float baseDamage = attacker.Attack;
        float multiplier = 1f;
        if (attacker.Type != null && defender.Type != null)
        {
            multiplier = attacker.Type.GetDamageMultiplier(defender.Type);
        }
        return baseDamage * multiplier;
    }

    private void SpawnEnemyForNode(NodeData node)
    {
        if (node.PossibleCreatures == null || node.PossibleCreatures.Count == 0) return;

        var randomCreatureSO = node.PossibleCreatures[UnityEngine.Random.Range(0, node.PossibleCreatures.Count)];
        node.Enemy = Creature.CreateAtLevel(randomCreatureSO, node.EnemyLevel);
        node.BattleTimer = 0f;
        node.IsHealing = false;
    }

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

    public bool TryUnlockNode(NodeData node)
    {
        if (node != null && node.isVisible && !node.isOwned && Player.Money >= node.UpgradeCost)
        {
            Player.Money -= node.UpgradeCost;
            UnlockNode(node);
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
        }

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
                node.baseCost = ns.baseCost;

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
        if (targetLevel <= 1) return 0;
        float exp = 100f;
        for (int i = 1; i < targetLevel; i++)
        {
            exp *= 1.3f;
        }
        return Mathf.CeilToInt(exp);
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
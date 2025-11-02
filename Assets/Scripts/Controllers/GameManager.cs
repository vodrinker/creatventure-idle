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
    [SerializeField] private Sprite lockedNodeSprite;

    public Dictionary<Vector2Int, NodeData> Map { get; private set; }
    public PlayerData Player { get; private set; }

    public event Action<int> OnMoneyChanged;

    public event Action<NodeData> OnNodeStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeGame();
    }

    private void Start()
    {
        InvokeRepeating(nameof(ProcessProduction), 1f, 1f);
    }

    private void InitializeGame()
    {
        var mapGenerator = new MapGenerator();
        Map = mapGenerator.GenerateMap(mapWidth, mapHeight, nodeDefinitionPool);
        Player = new PlayerData();

        UnlockNode(GetNodeAt(Vector2Int.zero));
    }

    private void ProcessProduction()
    {
        int totalProduction = Map.Values.Where(node => node.isOwned).Sum(node => node.Production);

        if (totalProduction > 0)
        {
            Player.Money += totalProduction;
            OnMoneyChanged?.Invoke(Player.Money);
        }
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
        if (node != null && node.isVisible && !node.isOwned && Player.Money >= node.Cost)
        {
            Player.Money -= node.Cost;
            OnMoneyChanged?.Invoke(Player.Money);
            UnlockNode(node);
            return true;
        }
        return false;
    }

    private void UnlockNode(NodeData node)
    {
        if (node == null) return;

        node.isOwned = true;
        node.isVisible = true;
        OnNodeStateChanged?.Invoke(node);

        foreach (var neighbor in GetNeighborsOf(node))
        {
            if (!neighbor.isVisible)
            {
                neighbor.isVisible = true;
                OnNodeStateChanged?.Invoke(neighbor);
            }
        }
    }

    public Sprite GetLockedNodeSprite()
    {
        return lockedNodeSprite;
    }
}
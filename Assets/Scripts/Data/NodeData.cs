using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NodeData
{
    public Vector2Int Coordinates { get; private set; }
    public string NodeName { get; private set; }
    public Sprite NodeSprite { get; private set; }

    public bool isVisible { get; set; }
    public bool isOwned { get; set; }

    public int productionLevel { get; set; }
    public int adventureLevel { get; set; }

    public float baseCost { get; set; }
    public int UpgradeCost => (int)((Mathf.Pow(productionLevel, 2) + 1) * baseCost);

    public List<NodeProductionProgress> productionProgresses;
    public List<CreatureSO> PossibleCreatures { get; private set; }
    public Creature AssignedCreature { get; set; }

    public Creature Enemy { get; set; }
    public float BattleTimer { get; set; }
    public bool IsHealing { get; set; }
    public int EnemyLevel => GetDistanceTo(Vector2Int.zero) + 1;

    public NodeData(Vector2Int coordinates, NodeDefinitionSO definition)
    {
        Coordinates = coordinates;
        NodeName = definition.nodeName;
        NodeSprite = definition.nodeSprite;

        productionLevel = 1;
        adventureLevel = 0;

        productionProgresses = new List<NodeProductionProgress>();
        foreach (var item in definition.productionItems)
        {
            productionProgresses.Add(new NodeProductionProgress { item = item.item, currentProductionProgress = 0, baseAmount = item.baseAmount });
        }

        PossibleCreatures = definition.possibleCreatures != null
            ? new List<CreatureSO>(definition.possibleCreatures)
            : new List<CreatureSO>();
    }

    public int GetDistanceTo(Vector2Int targetCoordinates)
    {
        int dx = Mathf.Abs(Coordinates.x - targetCoordinates.x);
        int dy = Mathf.Abs(Coordinates.y - targetCoordinates.y);
        return Mathf.Max(dx, dy);
    }

    public int GetDistanceTo(NodeData otherNode)
    {
        if (otherNode == null) return int.MaxValue;
        return GetDistanceTo(otherNode.Coordinates);
    }
}

using UnityEngine;

public class NodeData
{
    public Vector2Int Coordinates { get; private set; }
    public NodeDefinitionSO Definition { get; private set; }

    public bool isVisible { get; set; }
    public bool isOwned { get; set; }

    public int Cost => 100;
    public int Production => 1;

    public NodeData(Vector2Int coordinates, NodeDefinitionSO definition)
    {
        Coordinates = coordinates;
        Definition = definition;
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
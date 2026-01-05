using UnityEngine;
using System.Collections.Generic;

public class MapGenerator
{
    public Dictionary<Vector2Int, NodeData> GenerateMap(int width, int height, NodeDefinitionSO[] nodePool, NodeDefinitionSO centerNodeDefinition)
    {
        var mapData = new Dictionary<Vector2Int, NodeData>();
        if (nodePool == null || nodePool.Length == 0)
        {
            Debug.LogError("Node pool is empty! Cannot generate map.");
            return mapData;
        }

        int xOffset = width / 2;
        int yOffset = height / 2;

        for (int y = -yOffset; y <= yOffset; y++)
        {
            for (int x = -xOffset; x <= xOffset; x++)
            {
                var coordinates = new Vector2Int(x, y);
                NodeDefinitionSO definition;

                if (x == 0 && y == 0)
                {
                    definition = centerNodeDefinition;
                }
                else
                {
                    definition = nodePool[Random.Range(0, nodePool.Length)];
                }

                var newNode = new NodeData(coordinates, definition);

                int distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                newNode.baseCost = distance * 100;

                mapData.Add(coordinates, newNode);
            }
        }

        if (width % 2 == 0 || height % 2 == 0)
        {
            Debug.LogWarning("Map dimensions are even. The center is not a single tile.");
        }

        return mapData;
    }
}
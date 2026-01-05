using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNodeDefinition", menuName = "Game/Node Definition")]
public class NodeDefinitionSO : ScriptableObject
{
    public string nodeName;
    public Sprite nodeSprite;
    public List<ProductionItem> productionItems;
    public List<CreatureSO> possibleCreatures;
}

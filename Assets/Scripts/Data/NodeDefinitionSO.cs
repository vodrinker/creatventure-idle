using UnityEngine;

[CreateAssetMenu(fileName = "NewNodeDefinition", menuName = "Game/Node Definition")]
public class NodeDefinitionSO : ScriptableObject
{
    public string nodeName;
    public Sprite nodeSprite;
}
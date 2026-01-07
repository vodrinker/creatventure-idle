using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public PlayerSave player;
    public List<NodeSave> nodes;
    public TechSave tech;
}

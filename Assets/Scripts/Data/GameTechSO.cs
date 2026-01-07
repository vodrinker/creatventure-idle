using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Game Tech", menuName = "Creatures/Technology/Game Tech")]
public class GameTechSO : BaseTechSO
{
    [Header("Game Tech Settings")]
    // Cost is implicitly 1 Tech Point in the current design, but we can add overrides if needed later.
    
    public List<TechEffect> effects = new List<TechEffect>();
}

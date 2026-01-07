using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class CreaturesViewController
{
    private VisualElement root;
    private GameManager gameManager;
    private VisualTreeAsset creatureListElementAsset;

    // UI Elements
    private Label trapsLabel;
    private Button tabCollection;
    private Button tabWild;
    private VisualElement collectionContainer;
    private ScrollView collectionScroll;
    private VisualElement creaturesListContent; // Container inside ScrollView
    private VisualElement wildContainer;
    private ScrollView wildList;

    private bool isWildTabActive = false;

    public CreaturesViewController(VisualElement root, GameManager gameManager)
    {
        this.root = root;
        this.gameManager = gameManager;
        this.creatureListElementAsset = Resources.Load<VisualTreeAsset>("CreatureListElement");

        QueryElements();
        RegisterCallbacks();
        UpdateUI();
    }

    private void QueryElements()
    {
        trapsLabel = root.Q<Label>("TrapsLabel");
        tabCollection = root.Q<Button>("TabCollection");
        tabWild = root.Q<Button>("TabWild");
        collectionContainer = root.Q<VisualElement>("CollectionContainer"); // The ScrollView itself
        creaturesListContent = root.Q<VisualElement>("CreaturesListContent");
        collectionScroll = root.Q<ScrollView>("CollectionContainer");

        wildContainer = root.Q<VisualElement>("WildContainer");
        wildList = root.Q<ScrollView>("WildList");
    }

    private void RegisterCallbacks()
    {
        if (tabCollection != null) tabCollection.clicked += () => SwitchTab(false);
        if (tabWild != null) tabWild.clicked += () => SwitchTab(true);
    }

    public void UnregisterCallbacks()
    {
        // cleanup if needed
    }

    private void SwitchTab(bool wild)
    {
        isWildTabActive = wild;

        // Update Tab Buttons
        if (wild)
        {
            tabWild?.AddToClassList("selected");
            tabCollection?.RemoveFromClassList("selected");
            
            wildContainer.style.display = DisplayStyle.Flex;
            collectionContainer.style.display = DisplayStyle.None;
        }
        else
        {
            tabCollection?.AddToClassList("selected");
            tabWild?.RemoveFromClassList("selected");

            collectionContainer.style.display = DisplayStyle.Flex;
            wildContainer.style.display = DisplayStyle.None;
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (gameManager == null) return;

        // Update Traps
        if (trapsLabel != null)
        {
            trapsLabel.text = $"Traps: {gameManager.Player.Traps}";
        }

        if (isWildTabActive)
        {
            RefreshWildList();
        }
        else
        {
            RefreshCollectionList();
        }
    }

    private void RefreshCollectionList()
    {
        if (creaturesListContent == null) return;
        creaturesListContent.Clear();

        foreach (var creature in gameManager.Player.Creatures)
        {
            var element = creatureListElementAsset.CloneTree();
            var listItem = element.Q<VisualElement>("CreatureListItem");
            if (listItem != null) listItem.AddToClassList("creature-list-item");

            var iconElement = element.Q<VisualElement>("CreatureIcon");
            var nameLabel = element.Q<Label>("CreatureName");
            var levelLabel = element.Q<Label>("CreatureLevel");

            if (creature.Definition != null && creature.Definition.sprite != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(creature.Definition.sprite);
            }
            nameLabel.text = creature.Definition != null ? creature.Definition.creatureName : "Unknown";
            levelLabel.text = $"Lv. {creature.Level}";

            creaturesListContent.Add(element);
        }
    }

    private void RefreshWildList()
    {
        if (wildList == null) return;
        wildList.Clear();

        if (gameManager.Player.CatchableCreatures == null) return;

        // Sort by level descending?
        var sorted = gameManager.Player.CatchableCreatures.OrderByDescending(c => c.Level).ToList();

        foreach (var creature in sorted)
        {
            var row = new VisualElement();
            row.AddToClassList("wild-row");

            var nameLabel = new Label(creature.Definition != null ? creature.Definition.creatureName : "Unknown");
            nameLabel.AddToClassList("row-name");
            row.Add(nameLabel);

            var levelLabel = new Label($"Lv. {creature.Level}");
            levelLabel.AddToClassList("row-level");
            row.Add(levelLabel);

            var catchBtn = new Button();
            catchBtn.text = "Catch";
            catchBtn.AddToClassList("catch-btn");
            
            bool canAfford = gameManager.Player.Traps > 0;
            catchBtn.SetEnabled(canAfford);
            
            var cParams = creature; // Capture for lambda
            catchBtn.clicked += () =>
            {
                if (gameManager.TryCatchCreature(cParams))
                {
                   // TryCatchCreature triggers events which will call UpdateUI via UIManager
                }
            };
            row.Add(catchBtn);

            wildList.Add(row);
        }
        
        if (sorted.Count == 0)
        {
            var emptyLabel = new Label("No creatures sighted yet.");
            emptyLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.paddingTop = 20;
            wildList.Add(emptyLabel);
        }
    }
}

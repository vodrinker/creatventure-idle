using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class UIManager : MonoBehaviour
{
    public UIDocument uiDocument;

    private VisualElement contentContainer;
    private VisualElement popupHost;
    private VisualElement topBar;
    private Label moneyLabel;
    private Button mapButton;
    private Button techTreeButton;
    private Button itemsButton;
    private Button creaturesButton;

    private readonly Stack<VisualElement> popupStack = new Stack<VisualElement>();

    private VisualTreeAsset mapViewAsset;
    private VisualTreeAsset mapTilePopupAsset;
    private VisualTreeAsset itemsViewAsset;
    private VisualTreeAsset itemListElementAsset;
    private VisualTreeAsset creaturesViewAsset;
    private VisualTreeAsset creatureListElementAsset;
    private VisualTreeAsset creaturePickerPopupAsset;

    private ListView creaturesListView;
    private MapView currentMapView;

    // Flash feedback tracking
    private Dictionary<VisualElement, Coroutine> activeFlashes = new Dictionary<VisualElement, Coroutine>();
    private bool isShowingItemsView;
    private Dictionary<ItemSO, VisualElement> itemRowCache = new Dictionary<ItemSO, VisualElement>();

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        QueryVisualElements();
        LoadAssets();
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnPlayerDataUpdated += UpdatePlayerData;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnPlayerDataUpdated -= UpdatePlayerData;
        }
    }

    private void Start()
    {
        RegisterCallbacks();
        ShowMapView();
        UpdatePlayerData(); // Initial update
    }

    private void UpdatePlayerData()
    {
        if (gameManager == null) return;

        UpdateMoneyLabel(gameManager.Player.Money);

        if (isShowingItemsView)
        {
            RefreshItemsView();
        }

        if (creaturesListView != null && creaturesListView.parent != null)
        {
            creaturesListView.itemsSource = gameManager.Player.Creatures;
            creaturesListView.Rebuild();
        }
    }

    private void UpdateMoneyLabel(long newAmount)
    {
        if (moneyLabel == null)
        {
            moneyLabel = new Label($"Money: {NumberFormatter.Format(newAmount)}");
            moneyLabel.name = "MoneyLabel";
            topBar.Add(moneyLabel);
        }
        else
        {
            moneyLabel.text = $"Money: {NumberFormatter.Format(newAmount)}";
        }
    }

    private void QueryVisualElements()
    {
        var root = uiDocument.rootVisualElement;
        contentContainer = root.Q<VisualElement>("ContentContainer");
        popupHost = root.Q<VisualElement>("PopupHost");
        topBar = root.Q<VisualElement>("TopBar");
        mapButton = root.Q<Button>("MenuButton_Map");
        techTreeButton = root.Q<Button>("MenuButton_TechTree");
        itemsButton = root.Q<Button>("MenuButton_Items");
        creaturesButton = root.Q<Button>("MenuButton_Creatures");
    }

    private void RegisterCallbacks()
    {
        mapButton.RegisterCallback<ClickEvent>(evt => ShowMapView());
        techTreeButton.RegisterCallback<ClickEvent>(evt => ShowTechTreeView());
        itemsButton.RegisterCallback<ClickEvent>(evt => ShowItemsView());
        creaturesButton.RegisterCallback<ClickEvent>(evt => ShowCreaturesView());
    }

    private void LoadAssets()
    {
        mapViewAsset = Resources.Load<VisualTreeAsset>("MapView");
        mapTilePopupAsset = Resources.Load<VisualTreeAsset>("MapTilePopup");
        itemsViewAsset = Resources.Load<VisualTreeAsset>("ItemsView");
        itemListElementAsset = Resources.Load<VisualTreeAsset>("ItemListElement");
        creaturesViewAsset = Resources.Load<VisualTreeAsset>("CreaturesView");
        creatureListElementAsset = Resources.Load<VisualTreeAsset>("CreatureListElement");
        creaturePickerPopupAsset = Resources.Load<VisualTreeAsset>("CreaturePickerPopup");
    }

    private void CloseAllPopups()
    {
        while (popupStack.Count > 0)
        {
            var popup = popupStack.Pop();
            popup.RemoveFromHierarchy();
        }
        popupHost.style.display = DisplayStyle.None;
    }

    private void ShowMapView()
    {
        CloseAllPopups();
        contentContainer.Clear();
        if (currentMapView != null) currentMapView.UnregisterCallbacks();
        creaturesListView = null;
        isShowingItemsView = false;
        var mapViewInstance = mapViewAsset.CloneTree();
        contentContainer.Add(mapViewInstance);
        currentMapView = new MapView(mapViewInstance, this, gameManager);
    }

    private void ShowTechTreeView()
    {
        CloseAllPopups();
        contentContainer.Clear();
        if (currentMapView != null) currentMapView.UnregisterCallbacks();
        currentMapView = null;
        creaturesListView = null;
        isShowingItemsView = false;
    }

    private void RefreshItemsView()
    {
        if (gameManager == null || !isShowingItemsView) return;

        // Find total label in current view
        var sellAllValue = contentContainer.Q<Label>("SellAllValue");
        long totalValue = 0;
        var items = new List<KeyValuePair<ItemSO, int>>(gameManager.Player.Items);

        // Update or Create rows
        var itemsTable = contentContainer.Q<VisualElement>("ItemsTable");

        foreach (var itemData in items)
        {
            long itemValue = (long)itemData.Key.value * itemData.Value;
            totalValue += itemValue;

            if (itemRowCache.TryGetValue(itemData.Key, out var row))
            {
                // Update existing
                var qtyLabel = row.Q<Label>(className: "item-qty");
                var valueLabel = row.Q<Label>(className: "item-value");
                if (qtyLabel != null) qtyLabel.text = NumberFormatter.Format(itemData.Value);
                if (valueLabel != null) valueLabel.text = $"{NumberFormatter.Format(itemValue)}$";
            }
            else
            {
                // Create new row (if new item appeared while view is open)
                if (itemsTable != null)
                {
                    var newRow = CreateItemRow(itemData.Key, itemData.Value, itemValue, items);
                    itemsTable.Add(newRow);
                    itemRowCache[itemData.Key] = newRow;
                }
            }
        }

        if (sellAllValue != null) sellAllValue.text = $"{NumberFormatter.Format(totalValue)}$";

        // Remove rows for items that no longer exist (sold out)
        var keysToRemove = new List<ItemSO>();
        foreach (var key in itemRowCache.Keys)
        {
            if (!gameManager.Player.Items.ContainsKey(key))
            {
                var row = itemRowCache[key];
                row.RemoveFromHierarchy();
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            itemRowCache.Remove(key);
        }
    }

    private VisualElement CreateItemRow(ItemSO item, int qty, long totalValue, List<KeyValuePair<ItemSO, int>> allItems)
    {
        var row = new VisualElement();
        row.AddToClassList("item-row");

        var nameLabel = new Label(item.itemName);
        nameLabel.AddToClassList("item-name");
        row.Add(nameLabel);

        var qtyLabel = new Label(NumberFormatter.Format(qty));
        qtyLabel.AddToClassList("item-qty");
        row.Add(qtyLabel);

        var valueLabel = new Label($"{NumberFormatter.Format(totalValue)}$");
        valueLabel.AddToClassList("item-value");
        row.Add(valueLabel);

        var sellButton = new Button(() =>
        {
            gameManager.SellItem(item, qty);
            // We rely on OnPlayerDataUpdated -> RefreshItemsView, 
            // but immediate feedback might be nice. 
            // Actually, SellItem triggers OnPlayerDataUpdated, so it loopbacks.
        });
        sellButton.text = "Sell";
        sellButton.AddToClassList("item-sell-button");
        row.Add(sellButton);

        return row;
    }

    private void ShowItemsView()
    {
        if (gameManager == null) return;

        CloseAllPopups();
        contentContainer.Clear();
        if (currentMapView != null) currentMapView.UnregisterCallbacks();
        currentMapView = null;
        isShowingItemsView = true;
        itemRowCache.Clear();

        var itemsViewInstance = itemsViewAsset.CloneTree();
        var itemsTable = itemsViewInstance.Q<VisualElement>("ItemsTable");
        var sellAllValue = itemsViewInstance.Q<Label>("SellAllValue");
        var sellAllButton = itemsViewInstance.Q<Button>("SellAllButton");

        long totalValue = 0;
        var items = new List<KeyValuePair<ItemSO, int>>(gameManager.Player.Items);

        foreach (var itemData in items)
        {
            long itemValue = (long)itemData.Key.value * itemData.Value;
            totalValue += itemValue;

            var row = CreateItemRow(itemData.Key, itemData.Value, itemValue, items);
            itemsTable.Add(row);
            itemRowCache[itemData.Key] = row;
        }

        sellAllValue.text = $"{NumberFormatter.Format(totalValue)}$";
        sellAllButton.clicked += () =>
        {
            // Sell all needs to iterate a copy because collection modifies
            var itemsCopy = new List<KeyValuePair<ItemSO, int>>(gameManager.Player.Items);
            foreach (var itemData in itemsCopy)
            {
                gameManager.SellItem(itemData.Key, itemData.Value);
            }
        };

        contentContainer.Add(itemsViewInstance);
    }

    private void ShowCreaturesView()
    {
        if (gameManager == null) return;

        CloseAllPopups();
        contentContainer.Clear();
        if (currentMapView != null) currentMapView.UnregisterCallbacks();
        currentMapView = null;
        creaturesListView = null;
        isShowingItemsView = false;

        var creaturesViewInstance = creaturesViewAsset.CloneTree();
        var creaturesContainer = creaturesViewInstance.Q<VisualElement>("CreaturesContainer");

        foreach (var creature in gameManager.Player.Creatures)
        {
            var element = creatureListElementAsset.CloneTree();
            var listItem = element.Q<VisualElement>("CreatureListItem");
            if (listItem != null)
            {
                listItem.AddToClassList("creature-list-item");
            }

            var iconElement = element.Q<VisualElement>("CreatureIcon");
            var nameLabel = element.Q<Label>("CreatureName");
            var levelLabel = element.Q<Label>("CreatureLevel");

            if (creature.Definition != null && creature.Definition.sprite != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(creature.Definition.sprite);
            }
            nameLabel.text = creature.Definition != null ? creature.Definition.creatureName : "Unknown";
            levelLabel.text = $"Lv. {creature.Level}";

            creaturesContainer.Add(element);
        }

        contentContainer.Add(creaturesViewInstance);
    }

    public void OpenMapTilePopup(NodeData nodeData)
    {
        OpenPopup(mapTilePopupAsset, (popupInstance) =>
        {
            SetupNodePopupContent(popupInstance, nodeData);

            var scheduler = popupInstance.schedule.Execute(() =>
            {
                UpdateProgressBars(popupInstance, nodeData);
            }).Every(16);

            popupInstance.RegisterCallback<DetachFromPanelEvent>(evt => scheduler.Pause());
        });
    }

    private void SetupNodePopupContent(VisualElement popupInstance, NodeData nodeData)
    {
        var titleLabel = popupInstance.Q<Label>("PopupTitle");
        var imageElement = popupInstance.Q<VisualElement>("PopupImage");
        var buyButton = popupInstance.Q<Button>("BuyButton");

        var economyTab = popupInstance.Q<Button>("EconomyTab");
        var battleTab = popupInstance.Q<Button>("BattleTab");
        var economyContent = popupInstance.Q<VisualElement>("EconomyContent");
        var battleContent = popupInstance.Q<VisualElement>("BattleContent");

        var productionList = popupInstance.Q<VisualElement>("ProductionList");

        var enemySection = popupInstance.Q<VisualElement>("EnemySection");
        var playerSection = popupInstance.Q<VisualElement>("PlayerSection");

        var enemyInfo = popupInstance.Q<Label>("EnemyInfo");
        var enemyHP = popupInstance.Q<Label>("EnemyHP");
        var enemyImage = popupInstance.Q<VisualElement>("EnemyImage");
        var playerInfo = popupInstance.Q<Label>("PlayerInfo");
        var playerHP = popupInstance.Q<Label>("PlayerHP");
        var playerImage = popupInstance.Q<VisualElement>("PlayerImage");
        var assignCreatureButton = popupInstance.Q<Button>("AssignCreatureButton");
        var removeCreatureButton = popupInstance.Q<Button>("RemoveCreatureButton");

        // Flash Colors
        Color enemyBase = new Color(120f / 255f, 50f / 255f, 50f / 255f, 0.4f);
        Color enemyFlash = new Color(180f / 255f, 80f / 255f, 80f / 255f, 0.6f);
        Color playerBase = new Color(50f / 255f, 100f / 255f, 50f / 255f, 0.4f);
        Color playerFlash = new Color(80f / 255f, 150f / 255f, 80f / 255f, 0.6f);

        // manual click interaction
        if (enemySection != null)
        {
            enemySection.RegisterCallback<ClickEvent>(evt =>
            {
                if (nodeData.Enemy != null && nodeData.Enemy.IsAlive)
                {
                    nodeData.Enemy.TakeDamage(1);
                    TriggerFlash(enemySection, enemyBase, enemyFlash, 0.1f);
                    UpdateBattleUI(popupInstance, nodeData);
                }
            });
        }

        if (playerSection != null)
        {
            playerSection.RegisterCallback<ClickEvent>(evt =>
            {
                if (nodeData.AssignedCreature != null)
                {
                    nodeData.AssignedCreature.Heal(1);
                    TriggerFlash(playerSection, playerBase, playerFlash, 0.1f);
                    UpdateBattleUI(popupInstance, nodeData);
                }
            });
        }

        if (titleLabel != null) titleLabel.text = nodeData.NodeName;
        if (imageElement != null) imageElement.style.backgroundImage = new StyleBackground(nodeData.NodeSprite);

        if (economyTab != null)
        {
            economyTab.clicked += () =>
            {
                economyTab.AddToClassList("tab-active");
                if (battleTab != null) battleTab.RemoveFromClassList("tab-active");
                if (economyContent != null) economyContent.style.display = DisplayStyle.Flex;
                if (battleContent != null) battleContent.style.display = DisplayStyle.None;
                if (imageElement != null) imageElement.style.display = DisplayStyle.Flex; // Show image in economy
            };
        }

        if (battleTab != null)
        {
            battleTab.clicked += () =>
            {
                battleTab.AddToClassList("tab-active");
                if (economyTab != null) economyTab.RemoveFromClassList("tab-active");
                if (battleContent != null) battleContent.style.display = DisplayStyle.Flex;
                if (economyContent != null) economyContent.style.display = DisplayStyle.None;
                if (imageElement != null) imageElement.style.display = DisplayStyle.None; // Hide image in battle
            };
        }

        if (productionList != null && nodeData.productionProgresses != null)
        {
            productionList.Clear();
            for (int i = 0; i < nodeData.productionProgresses.Count; i++)
            {
                var prod = nodeData.productionProgresses[i];
                var row = new VisualElement();
                row.AddToClassList("production-item-row");

                var nameLabel = new Label(prod.item != null ? prod.item.itemName : "Unknown");
                nameLabel.AddToClassList("production-item-name");
                row.Add(nameLabel);

                float rate = GameBalance.CalculateProductionRate(prod.baseAmount, nodeData.productionLevel);
                var rateLabel = new Label($"{NumberFormatter.Format(rate)}/s");
                rateLabel.AddToClassList("production-rate");
                row.Add(rateLabel);

                var progressContainer = new VisualElement();
                progressContainer.AddToClassList("progress-bar-container");
                var progressFill = new VisualElement();
                progressFill.AddToClassList("progress-bar-fill");
                progressFill.name = $"ProgressFill_{i}";
                float fillWidth = prod.currentProductionProgress * 100f;
                progressFill.style.width = new Length(fillWidth, LengthUnit.Pixel);
                progressContainer.Add(progressFill);
                row.Add(progressContainer);

                productionList.Add(row);
            }
        }

        UpdateBattleUI(popupInstance, nodeData);

        if (assignCreatureButton != null)
            assignCreatureButton.clicked += () => OpenCreaturePickerPopup(nodeData, popupInstance);

        if (removeCreatureButton != null)
        {
            removeCreatureButton.clicked += () =>
            {
                gameManager.RemoveCreatureFromNode(nodeData);
                UpdateBattleUI(popupInstance, nodeData);
            };
        }

        var levelDownBtn = popupInstance.Q<Button>("LevelDownBtn");
        var levelUpBtn = popupInstance.Q<Button>("LevelUpBtn");

        if (levelDownBtn != null && levelUpBtn != null)
        {
            levelDownBtn.clicked += () =>
            {
                if (nodeData.adventureLevel > 0)
                {
                    nodeData.adventureLevel--;
                    UpdateBattleUI(popupInstance, nodeData);
                }
            };

            levelUpBtn.clicked += () =>
            {
                if (nodeData.adventureLevel < nodeData.MaxUnlockedAdventureLevel)
                {
                    nodeData.adventureLevel++;
                    UpdateBattleUI(popupInstance, nodeData);
                }
            };
        }

        if (nodeData.isOwned)
        {
            buyButton.style.display = DisplayStyle.None;
            economyTab.style.display = DisplayStyle.Flex;
            battleTab.style.display = DisplayStyle.Flex;

            titleLabel.text = $"{nodeData.NodeName} Lvl {nodeData.productionLevel}";

            var upgradeButton = popupInstance.Q<Button>("UpgradeButton");
            if (upgradeButton != null)
            {
                upgradeButton.style.display = DisplayStyle.Flex;
                upgradeButton.text = $"Upgrade ({nodeData.UpgradeCost})";
                upgradeButton.clicked += () =>
                {
                    bool success = gameManager.TryUpgradeNode(nodeData);
                    if (success)
                    {
                        // Refresh logic
                        titleLabel.text = $"{nodeData.NodeName} Lvl {nodeData.productionLevel}";
                        upgradeButton.text = $"Upgrade ({NumberFormatter.Format(nodeData.UpgradeCost)})";

                        // Recalculate rates display
                        var rateLabels = popupInstance.Query<Label>(className: "production-rate").ToList();
                        for (int i = 0; i < rateLabels.Count && i < nodeData.productionProgresses.Count; i++)
                        {
                            var prod = nodeData.productionProgresses[i];
                            float rate = GameBalance.CalculateProductionRate(prod.baseAmount, nodeData.productionLevel);
                            rateLabels[i].text = $"{NumberFormatter.Format(rate)}/s";
                        }
                    }
                };
            }
        }
        else
        {
            var upgradeButton = popupInstance.Q<Button>("UpgradeButton");
            if (upgradeButton != null) upgradeButton.style.display = DisplayStyle.None;

            economyTab.style.display = DisplayStyle.None;
            battleTab.style.display = DisplayStyle.None;
            economyContent.style.display = DisplayStyle.None;
            battleContent.style.display = DisplayStyle.None;

            if (nodeData.isVisible)
            {
                buyButton.text = $"Buy ({NumberFormatter.Format(nodeData.UpgradeCost)})";
                // Note: UpgradeCost at lvl 0 is same as baseCost (formula: base * 1.2^0 = base)
                buyButton.style.display = DisplayStyle.Flex;
                buyButton.clicked += () =>
                {
                    if (gameManager != null)
                    {
                        bool success = gameManager.TryUnlockNode(nodeData);
                        if (success)
                        {
                            CloseCurrentPopup(); // Close the "buy" popup first
                            OpenMapTilePopup(nodeData); // Open the "owned" popup
                        }
                    }
                };
            }
        }
    }

    private void UpdateBattleUI(VisualElement popupInstance, NodeData nodeData)
    {
        if (popupInstance == null || nodeData == null) return;

        var enemyInfo = popupInstance.Q<Label>("EnemyInfo");
        var enemyHP = popupInstance.Q<Label>("EnemyHP");
        var enemyImage = popupInstance.Q<VisualElement>("EnemyImage");
        var enemyHPBar = popupInstance.Q<VisualElement>("EnemyHPBar");

        var playerInfo = popupInstance.Q<Label>("PlayerInfo");
        var playerHP = popupInstance.Q<Label>("PlayerHP");
        var playerImage = popupInstance.Q<VisualElement>("PlayerImage");
        var playerHPBar = popupInstance.Q<VisualElement>("PlayerHPBar");

        var assignCreatureButton = popupInstance.Q<Button>("AssignCreatureButton");
        var removeCreatureButton = popupInstance.Q<Button>("RemoveCreatureButton");

        var progressLabel = popupInstance.Q<Label>("ProgressLabel");
        var levelLabel = popupInstance.Q<Label>("LevelLabel");
        var levelDownBtn = popupInstance.Q<Button>("LevelDownBtn");
        var levelUpBtn = popupInstance.Q<Button>("LevelUpBtn");
        var upgradeButton = popupInstance.Q<Button>("UpgradeButton");

        if (upgradeButton != null && nodeData.isOwned)
        {
            bool canAfford = gameManager.Player.Money >= nodeData.UpgradeCost;
            upgradeButton.SetEnabled(canAfford);
            upgradeButton.style.opacity = canAfford ? 1f : 0.5f;
            // Optional: Update text if cost could change dynamically or just to be safe
            upgradeButton.text = $"Upgrade ({NumberFormatter.Format(nodeData.UpgradeCost)})";
        }

        if (progressLabel != null)
        {
            if (nodeData.adventureLevel < nodeData.MaxUnlockedAdventureLevel)
            {
                progressLabel.text = "Progress: Completed";
            }
            else
            {
                int target = (nodeData.adventureLevel + 1) * 10;
                progressLabel.text = $"Progress: {nodeData.AdventureProgress}/{target}";
            }
        }

        if (levelLabel != null) levelLabel.text = $"Lvl: {nodeData.adventureLevel}";
        if (levelDownBtn != null) levelDownBtn.SetEnabled(nodeData.adventureLevel > 0);
        if (levelUpBtn != null) levelUpBtn.SetEnabled(nodeData.adventureLevel < nodeData.MaxUnlockedAdventureLevel);

        if (nodeData.Enemy != null && nodeData.Enemy.Definition != null)
        {
            if (enemyInfo != null) enemyInfo.text = $"{nodeData.Enemy.Definition.creatureName} Lv. {nodeData.Enemy.Level}";
            if (enemyHP != null) enemyHP.text = $"HP: {NumberFormatter.Format(nodeData.Enemy.CurrentHealth)}/{NumberFormatter.Format(nodeData.Enemy.MaxHealth)}";
            if (enemyImage != null)
            {
                if (nodeData.Enemy.Definition.sprite != null)
                {
                    enemyImage.style.display = DisplayStyle.Flex;
                    enemyImage.style.backgroundImage = new StyleBackground(nodeData.Enemy.Definition.sprite);
                }
                else
                {
                    enemyImage.style.display = DisplayStyle.None;
                }
            }
            if (enemyHPBar != null)
            {
                float enemyHPPercent = nodeData.Enemy.CurrentHealth / nodeData.Enemy.MaxHealth * 100f;
                enemyHPBar.style.width = new Length(enemyHPPercent, LengthUnit.Percent);
            }
        }
        else
        {
            if (enemyInfo != null) enemyInfo.text = "No enemy";
            if (enemyHP != null) enemyHP.text = "HP: -/-";
            if (enemyImage != null) enemyImage.style.backgroundImage = StyleKeyword.None;
            if (enemyHPBar != null) enemyHPBar.style.width = new Length(0, LengthUnit.Percent);
        }

        if (nodeData.AssignedCreature != null && nodeData.AssignedCreature.Definition != null)
        {
            var creature = nodeData.AssignedCreature;
            if (playerInfo != null) playerInfo.text = $"{creature.Definition.creatureName} Lv. {creature.Level}";
            if (playerHP != null)
            {
                playerHP.text = $"HP: {NumberFormatter.Format(creature.CurrentHealth)}/{NumberFormatter.Format(creature.MaxHealth)}";
                playerHP.style.display = DisplayStyle.Flex;
            }
            if (playerImage != null)
            {
                if (creature.Definition.sprite != null)
                {
                    playerImage.style.display = DisplayStyle.Flex;
                    playerImage.style.backgroundImage = new StyleBackground(creature.Definition.sprite);
                }
                else
                {
                    playerImage.style.display = DisplayStyle.None;
                }
            }
            if (playerHPBar != null)
            {
                playerHPBar.style.display = DisplayStyle.Flex;
                float playerHPPercent = creature.CurrentHealth / creature.MaxHealth * 100f;
                playerHPBar.style.width = new Length(playerHPPercent, LengthUnit.Percent);
            }

            if (assignCreatureButton != null) assignCreatureButton.style.display = DisplayStyle.None;
            if (removeCreatureButton != null) removeCreatureButton.style.display = DisplayStyle.Flex;
        }
        else
        {
            if (playerInfo != null) playerInfo.text = "No creature assigned";
            if (playerHP != null) playerHP.style.display = DisplayStyle.None;
            if (playerImage != null) playerImage.style.display = DisplayStyle.None;
            if (playerHPBar != null) playerHPBar.style.display = DisplayStyle.None;

            if (assignCreatureButton != null) assignCreatureButton.style.display = DisplayStyle.Flex;
            if (removeCreatureButton != null) removeCreatureButton.style.display = DisplayStyle.None;
        }
    }

    private void UpdateProgressBars(VisualElement popupInstance, NodeData nodeData)
    {
        for (int i = 0; i < nodeData.productionProgresses.Count; i++)
        {
            var progressFill = popupInstance.Q<VisualElement>($"ProgressFill_{i}");
            if (progressFill != null)
            {
                float fillWidth = nodeData.productionProgresses[i].currentProductionProgress * 100f;
                progressFill.style.width = new Length(fillWidth, LengthUnit.Pixel);
            }
        }

        UpdateBattleUI(popupInstance, nodeData);
    }

    private void OpenCreaturePickerPopup(NodeData nodeData, VisualElement nodePopupInstance)
    {
        OpenPopup(creaturePickerPopupAsset, (popupInstance) =>
        {
            var creatureList = popupInstance.Q<VisualElement>("CreaturePickerList");
            var unassignedCreatures = gameManager.GetUnassignedCreatures();

            creatureList.Clear();

            foreach (var creature in unassignedCreatures)
            {
                var element = creatureListElementAsset.CloneTree();

                var iconElement = element.Q<VisualElement>("CreatureIcon");
                var nameLabel = element.Q<Label>("CreatureName");
                var levelLabel = element.Q<Label>("CreatureLevel");

                if (creature.Definition != null && creature.Definition.sprite != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(creature.Definition.sprite);
                }
                nameLabel.text = creature.Definition != null ? creature.Definition.creatureName : "Unknown";
                levelLabel.text = $"Lv. {creature.Level}";

                var capturedCreature = creature;
                element.RegisterCallback<ClickEvent>(evt =>
                {
                    gameManager.AssignCreatureToNode(nodeData, capturedCreature);
                    CloseCurrentPopup();
                    UpdateBattleUI(nodePopupInstance, nodeData);
                });

                creatureList.Add(element);
            }

            if (unassignedCreatures.Count == 0)
            {
                var emptyLabel = new Label("No creatures available");
                emptyLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.paddingTop = 20;
                creatureList.Add(emptyLabel);
            }
        });
    }

    private void OpenPopup(VisualTreeAsset popupContentAsset, Action<VisualElement> onPopupOpened = null)
    {
        if (popupStack.Count == 0)
        {
            popupHost.style.display = DisplayStyle.Flex;
        }

        var scrim = new VisualElement();
        scrim.name = "scrim";
        scrim.AddToClassList("popup-scrim");
        scrim.RegisterCallback<ClickEvent>(evt => CloseCurrentPopup());

        var popupContent = popupContentAsset.CloneTree();
        popupContent.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        var popupInstance = new VisualElement();
        popupInstance.name = "popup-instance";
        popupInstance.AddToClassList("popup-instance-container");

        popupInstance.Add(scrim);
        popupInstance.Add(popupContent);

        if (popupStack.Count > 0)
        {
            popupStack.Peek().style.display = DisplayStyle.None;
        }

        popupStack.Push(popupInstance);
        popupHost.Add(popupInstance);

        popupInstance.schedule.Execute(() => onPopupOpened?.Invoke(popupInstance));
    }

    private void CloseCurrentPopup()
    {
        if (popupStack.Count == 0) return;

        var popupToClose = popupStack.Pop();
        popupToClose.RemoveFromHierarchy();

        if (popupStack.Count > 0)
        {
            popupStack.Peek().style.display = DisplayStyle.Flex;
        }

        if (popupStack.Count == 0)
        {
            popupHost.style.display = DisplayStyle.None;
        }
    }

    // Flash Feedback Logic
    private void TriggerFlash(VisualElement element, Color baseColor, Color flashColor, float duration)
    {
        if (element == null) return;

        // Stop existing flash on this element if any
        if (activeFlashes.TryGetValue(element, out var existingCoroutine))
        {
            if (existingCoroutine != null) StopCoroutine(existingCoroutine);
            activeFlashes.Remove(element);
        }

        // Start new flash
        var coroutine = StartCoroutine(PerformFlash(element, baseColor, flashColor, duration));
        activeFlashes.Add(element, coroutine);
    }

    private System.Collections.IEnumerator PerformFlash(VisualElement element, Color baseColor, Color flashColor, float duration)
    {
        float elapsed = 0f;
        // Start at peak flash
        element.style.backgroundColor = flashColor;

        while (elapsed < duration)
        {
            if (element == null) yield break; // Safety check

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Lerp back to base
            element.style.backgroundColor = Color.Lerp(flashColor, baseColor, t);
            yield return null;
        }

        if (element != null)
        {
            element.style.backgroundColor = baseColor;
            activeFlashes.Remove(element);
        }
    }
}
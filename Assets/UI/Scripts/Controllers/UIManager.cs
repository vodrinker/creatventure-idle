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
    private bool isShowingItemsView;

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
            ShowItemsView();
        }

        if (creaturesListView != null && creaturesListView.parent != null)
        {
            creaturesListView.itemsSource = gameManager.Player.Creatures;
            creaturesListView.Rebuild();
        }
    }

    private void UpdateMoneyLabel(int newAmount)
    {
        if (moneyLabel == null)
        {
            moneyLabel = new Label($"Money: {newAmount}");
            moneyLabel.name = "MoneyLabel";
            topBar.Add(moneyLabel);
        }
        else
        {
            moneyLabel.text = $"Money: {newAmount}";
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

    private void ShowItemsView()
    {
        if (gameManager == null) return;

        CloseAllPopups();
        contentContainer.Clear();
        if (currentMapView != null) currentMapView.UnregisterCallbacks();
        currentMapView = null;
        isShowingItemsView = true;

        var itemsViewInstance = itemsViewAsset.CloneTree();
        var itemsTable = itemsViewInstance.Q<VisualElement>("ItemsTable");
        var sellAllValue = itemsViewInstance.Q<Label>("SellAllValue");
        var sellAllButton = itemsViewInstance.Q<Button>("SellAllButton");

        long totalValue = 0;
        var items = new List<KeyValuePair<ItemSO, int>>(gameManager.Player.Items);

        foreach (var itemData in items)
        {
            var row = new VisualElement();
            row.AddToClassList("item-row");

            var nameLabel = new Label(itemData.Key.itemName);
            nameLabel.AddToClassList("item-name");
            row.Add(nameLabel);

            var qtyLabel = new Label(itemData.Value.ToString());
            qtyLabel.AddToClassList("item-qty");
            row.Add(qtyLabel);

            long itemValue = (long)itemData.Key.value * itemData.Value;
            totalValue += itemValue;

            var valueLabel = new Label($"{itemValue}$");
            valueLabel.AddToClassList("item-value");
            row.Add(valueLabel);

            var sellButton = new Button(() =>
            {
                gameManager.SellItem(itemData.Key, itemData.Value);
                ShowItemsView();
            });
            sellButton.text = "Sell";
            sellButton.AddToClassList("item-sell-button");
            row.Add(sellButton);

            itemsTable.Add(row);
        }

        sellAllValue.text = $"{totalValue}$";
        sellAllButton.clicked += () =>
        {
            foreach (var itemData in items)
            {
                gameManager.SellItem(itemData.Key, itemData.Value);
            }
            ShowItemsView();
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

        var enemyInfo = popupInstance.Q<Label>("EnemyInfo");
        var enemyHP = popupInstance.Q<Label>("EnemyHP");
        var enemyImage = popupInstance.Q<VisualElement>("EnemyImage");
        var playerInfo = popupInstance.Q<Label>("PlayerInfo");
        var playerHP = popupInstance.Q<Label>("PlayerHP");
        var playerImage = popupInstance.Q<VisualElement>("PlayerImage");
        var assignCreatureButton = popupInstance.Q<Button>("AssignCreatureButton");
        var removeCreatureButton = popupInstance.Q<Button>("RemoveCreatureButton");

        titleLabel.text = nodeData.NodeName;
        imageElement.style.backgroundImage = new StyleBackground(nodeData.NodeSprite);

        economyTab.clicked += () =>
        {
            economyTab.AddToClassList("tab-active");
            battleTab.RemoveFromClassList("tab-active");
            economyContent.style.display = DisplayStyle.Flex;
            battleContent.style.display = DisplayStyle.None;
            imageElement.style.display = DisplayStyle.Flex; // Show image in economy
        };

        battleTab.clicked += () =>
        {
            battleTab.AddToClassList("tab-active");
            economyTab.RemoveFromClassList("tab-active");
            battleContent.style.display = DisplayStyle.Flex;
            economyContent.style.display = DisplayStyle.None;
            imageElement.style.display = DisplayStyle.None; // Hide image in battle
        };

        productionList.Clear();
        for (int i = 0; i < nodeData.productionProgresses.Count; i++)
        {
            var prod = nodeData.productionProgresses[i];
            var row = new VisualElement();
            row.AddToClassList("production-item-row");

            var nameLabel = new Label(prod.item != null ? prod.item.itemName : "Unknown");
            nameLabel.AddToClassList("production-item-name");
            row.Add(nameLabel);

            float rate = prod.baseAmount * nodeData.productionLevel;
            var rateLabel = new Label($"{rate:F2}/s");
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

        UpdateBattleUI(popupInstance, nodeData);

        assignCreatureButton.clicked += () => OpenCreaturePickerPopup(nodeData, popupInstance);
        removeCreatureButton.clicked += () =>
        {
            gameManager.RemoveCreatureFromNode(nodeData);
            UpdateBattleUI(popupInstance, nodeData);
        };

        if (nodeData.isOwned)
        {
            buyButton.style.display = DisplayStyle.None;
            economyTab.style.display = DisplayStyle.Flex;
            battleTab.style.display = DisplayStyle.Flex;
        }
        else
        {
            economyTab.style.display = DisplayStyle.None;
            battleTab.style.display = DisplayStyle.None;
            economyContent.style.display = DisplayStyle.None;
            battleContent.style.display = DisplayStyle.None;

            if (nodeData.isVisible)
            {
                buyButton.text = $"Buy ({nodeData.UpgradeCost})";
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

        if (nodeData.Enemy != null && nodeData.Enemy.Definition != null)
        {
            enemyInfo.text = $"{nodeData.Enemy.Definition.creatureName} Lv. {nodeData.Enemy.Level}";
            enemyHP.text = $"HP: {nodeData.Enemy.CurrentHealth:F0}/{nodeData.Enemy.MaxHealth:F0}";
            if (nodeData.Enemy.Definition.sprite != null)
            {
                enemyImage.style.display = DisplayStyle.Flex;
                enemyImage.style.backgroundImage = new StyleBackground(nodeData.Enemy.Definition.sprite);
            }
            else
            {
                enemyImage.style.display = DisplayStyle.None;
            }
            float enemyHPPercent = nodeData.Enemy.CurrentHealth / nodeData.Enemy.MaxHealth * 100f;
            enemyHPBar.style.width = new Length(enemyHPPercent, LengthUnit.Percent);
        }
        else
        {
            enemyInfo.text = "No enemy";
            enemyHP.text = "HP: -/-";
            enemyImage.style.backgroundImage = StyleKeyword.None;
            enemyHPBar.style.width = new Length(0, LengthUnit.Percent);
        }

        if (nodeData.AssignedCreature != null && nodeData.AssignedCreature.Definition != null)
        {
            var creature = nodeData.AssignedCreature;
            playerInfo.text = $"{creature.Definition.creatureName} Lv. {creature.Level}";
            playerHP.text = $"HP: {creature.CurrentHealth:F0}/{creature.MaxHealth:F0}";
            if (creature.Definition.sprite != null)
            {
                playerImage.style.display = DisplayStyle.Flex;
                playerImage.style.backgroundImage = new StyleBackground(creature.Definition.sprite);
            }
            else
            {
                playerImage.style.display = DisplayStyle.None;
            }
            float playerHPPercent = creature.CurrentHealth / creature.MaxHealth * 100f;
            playerHPBar.style.width = new Length(playerHPPercent, LengthUnit.Percent);
            assignCreatureButton.style.display = DisplayStyle.None;
            removeCreatureButton.style.display = DisplayStyle.Flex;
        }
        else
        {
            playerInfo.text = "No creature assigned";
            playerHP.text = "HP: -/-";
            playerImage.style.backgroundImage = StyleKeyword.None;
            playerHPBar.style.width = new Length(0, LengthUnit.Percent);
            assignCreatureButton.style.display = DisplayStyle.Flex;
            removeCreatureButton.style.display = DisplayStyle.None;
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
}
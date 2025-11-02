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

    private readonly Stack<VisualElement> popupStack = new Stack<VisualElement>();

    private VisualTreeAsset mapViewAsset;
    private VisualTreeAsset mapTilePopupAsset;

    private void Awake()
    {
        QueryVisualElements();
        LoadAssets();
    }

    private void Start()
    {
        RegisterCallbacks();
        ShowMapView();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnMoneyChanged += UpdateMoneyLabel;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoneyLabel;
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
    }

    private void RegisterCallbacks()
    {
        mapButton.RegisterCallback<ClickEvent>(evt => ShowMapView());
        techTreeButton.RegisterCallback<ClickEvent>(evt => ShowTechTreeView());
    }

    private void LoadAssets()
    {
        mapViewAsset = Resources.Load<VisualTreeAsset>("MapView");
        mapTilePopupAsset = Resources.Load<VisualTreeAsset>("MapTilePopup");
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
        var mapViewInstance = mapViewAsset.CloneTree();
        contentContainer.Add(mapViewInstance);
        new MapView(mapViewInstance, this);
    }

    private void ShowTechTreeView()
    {
        CloseAllPopups();
        contentContainer.Clear();
    }

    public void OpenMapTilePopup(NodeData nodeData)
    {
        OpenPopup(mapTilePopupAsset, (popupInstance) =>
        {
            var titleLabel = popupInstance.Q<Label>("PopupTitle");
            var imageElement = popupInstance.Q<VisualElement>("PopupImage");
            var buyButton = popupInstance.Q<Button>("BuyButton");

            titleLabel.text = nodeData.Definition.nodeName;
            buyButton.text = $"Buy ({nodeData.Cost})";

            if (nodeData.isOwned)
            {
                imageElement.style.backgroundImage = new StyleBackground(nodeData.Definition.nodeSprite);
                buyButton.style.display = DisplayStyle.None;
            }
            else if (nodeData.isVisible)
            {
                imageElement.style.backgroundImage = new StyleBackground(nodeData.Definition.nodeSprite);
                buyButton.style.display = DisplayStyle.Flex;
                buyButton.clicked += () =>
                {
                    bool success = GameManager.Instance.TryUnlockNode(nodeData);
                    if (success)
                    {
                        CloseCurrentPopup();
                    }
                };
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
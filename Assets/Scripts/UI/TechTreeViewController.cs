using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class TechTreeViewController
{
    private VisualElement root;
    private VisualElement nodesLayer;
    private VisualElement connectionsLayer;
    private VisualElement contentContainer;

    // UI Elements
    private Label techPointsLabel;
    private Button buyPointBtn;
    private Button closeBtn;
    private Button tabGameBtn;
    private Button tabMetaBtn;
    private VisualElement viewport;

    private UIManager uiManager;

    // State
    private enum Tab { Game, Meta }
    private Tab currentTab = Tab.Game;

    // Note: selectedNode is no longer needed locally as popup handles it.

    private float zoom = 1.0f;
    private float gridSize = 100f; // Visual scaling factor

    // Resources
    private List<BaseTechSO> gameTechs = new List<BaseTechSO>();
    private List<BaseTechSO> metaTechs = new List<BaseTechSO>();

    // Pan state
    private bool isPanning;
    private Vector2 panStart;
    // Track translation manually since style.translate is complex to read back in runtime
    private Vector2 currentTranslation = Vector2.zero;

    public TechTreeViewController(VisualElement rootElement, UIManager manager)
    {
        this.root = rootElement;
        this.uiManager = manager;

        // Load Stylesheet
        var styleSheet = Resources.Load<StyleSheet>("TechTreeStyles");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }

        // Query Elements
        nodesLayer = root.Q("nodes-layer");
        connectionsLayer = root.Q("connections-layer");
        contentContainer = root.Q("content-container");

        techPointsLabel = root.Q<Label>("tech-points-label");
        buyPointBtn = root.Q<Button>("buy-point-btn");

        // Bind Buttons
        closeBtn = root.Q<Button>("close-btn");
        if (closeBtn != null) closeBtn.clicked += Close;

        tabGameBtn = root.Q<Button>("tab-game-btn");
        if (tabGameBtn != null) tabGameBtn.clicked += () => SwitchTab(Tab.Game);

        tabMetaBtn = root.Q<Button>("tab-meta-btn");
        if (tabMetaBtn != null) tabMetaBtn.clicked += () => SwitchTab(Tab.Meta);

        if (buyPointBtn != null) buyPointBtn.clicked += OnBuyPointClicked;

        // Input
        root.RegisterCallback<WheelEvent>(OnScrollWheel);
        viewport = root.Q("viewport");
        if (viewport != null)
        {
            viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
            viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
            viewport.focusable = true;
        }

        LoadResources();
        SwitchTab(Tab.Game);
        UpdateUI();
    }

    public void UnregisterCallbacks()
    {
        if (closeBtn != null) closeBtn.clicked -= Close;
        // Lambdas for tabs are hard to unregister without named methods, but object destruction handles mostly.
        // Direct event handlers should be unregistered if possible to prevent leaks if the UI document persists but controller changes.
        // For simplicity in this architecture (UI recreated), it's acceptable.

        if (buyPointBtn != null) buyPointBtn.clicked -= OnBuyPointClicked;

        root.UnregisterCallback<WheelEvent>(OnScrollWheel);
        if (viewport != null)
        {
            viewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            viewport.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            viewport.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
    }

    private void Close()
    {
        // Handled by UIManager typically clearing this view
        // But if we want a self-close button, we might need a callback or just hide.
        // For now, do nothing or assume parent handles it.
    }

    private void LoadResources()
    {
        gameTechs = Resources.LoadAll<BaseTechSO>("Technologies/Game").ToList();
        metaTechs = Resources.LoadAll<BaseTechSO>("Technologies/Meta").ToList();
    }

    private void SwitchTab(Tab tab)
    {
        currentTab = tab;

        // Update Tab Styles
        if (tabGameBtn != null && tabMetaBtn != null)
        {
            tabGameBtn.SetEnabled(true);
            tabMetaBtn.SetEnabled(true);

            if (tab == Tab.Game)
            {
                tabGameBtn.AddToClassList("selected");
                tabMetaBtn.RemoveFromClassList("selected");

                if (techPointsLabel != null) techPointsLabel.style.display = DisplayStyle.Flex;
                if (buyPointBtn != null) buyPointBtn.style.display = DisplayStyle.Flex;
            }
            else
            {
                tabMetaBtn.AddToClassList("selected");
                tabGameBtn.RemoveFromClassList("selected");

                if (techPointsLabel != null) techPointsLabel.style.display = DisplayStyle.None;
                if (buyPointBtn != null) buyPointBtn.style.display = DisplayStyle.None;
            }
        }

        RefreshGraph();
        UpdateUI();
        CenterContent();
    }

    private void RefreshGraph()
    {
        if (nodesLayer == null || connectionsLayer == null) return;

        nodesLayer.Clear();
        connectionsLayer.Clear();

        var nodes = currentTab == Tab.Game ? gameTechs : metaTechs;

        // Draw Connections
        foreach (var node in nodes)
        {
            if (node.connectedNodes != null)
            {
                foreach (var child in node.connectedNodes)
                {
                    if (child == null) continue;
                    DrawConnection(node.position, child.position);
                }
            }
        }

        // Draw Nodes
        foreach (var node in nodes)
        {
            var nodeElement = new VisualElement();
            nodeElement.AddToClassList("tech-node");

            // Positioning (Invert Y for consistency with Editor grid)
            nodeElement.style.left = node.position.x * gridSize;
            nodeElement.style.top = -node.position.y * gridSize;

            // Styling based on state
            if (currentTab == Tab.Game)
            {
                bool unlocked = TechManager.Instance.IsUnlocked(node.name);
                bool unlockable = TechManager.Instance.CanUnlock(node as GameTechSO);

                if (unlocked) nodeElement.AddToClassList("tech-node-unlocked");
                else if (unlockable) nodeElement.AddToClassList("tech-node-available");
                else nodeElement.AddToClassList("tech-node-locked");
            }
            else // Meta
            {
                int level = MetaProgressionManager.Instance.GetLevel(node.name);
                var metaNode = node as MetaUpgradeSO;
                int max = metaNode != null ? metaNode.maxLevels : 1;

                if (level >= max) nodeElement.AddToClassList("tech-node-maxed");
                else if (level > 0) nodeElement.AddToClassList("tech-node-unlocked");
                else if (MetaProgressionManager.Instance.CanUpgrade(metaNode)) nodeElement.AddToClassList("tech-node-available");
                else nodeElement.AddToClassList("tech-node-locked");
            }

            // Click Handler
            nodeElement.RegisterCallback<ClickEvent>(evt => SelectNode(node));

            // Icon/Label
            // if (node.icon) nodeElement.style.backgroundImage = new StyleBackground(node.icon);
            var label = new Label(node.displayName);
            label.AddToClassList("tech-node-label");
            nodeElement.Add(label);

            nodesLayer.Add(nodeElement);
        }
    }

    private void DrawConnection(Vector2Int startGrid, Vector2Int endGrid)
    {
        // Simple straight line using VisualElement
        Vector2 startPos = new Vector2(startGrid.x * gridSize + 20, -startGrid.y * gridSize + 20); // +20 for center offset
        Vector2 endPos = new Vector2(endGrid.x * gridSize + 20, -endGrid.y * gridSize + 20);

        Vector2 diff = endPos - startPos;
        float dist = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        var line = new VisualElement();
        line.style.position = Position.Absolute;
        line.style.height = 2; // Thickness
        line.style.width = dist;
        line.style.backgroundColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));
        line.style.left = startPos.x;
        line.style.top = startPos.y;

        // Pivot at start
        line.style.transformOrigin = new TransformOrigin(0, 0, 0);
        line.style.rotate = new Rotate(angle);

        connectionsLayer.Add(line);
    }

    private void SelectNode(BaseTechSO node)
    {
        // Open UIManager Popup
        if (uiManager != null)
        {
            uiManager.OpenTechNodePopup(node, () =>
            {
                RefreshGraph();
                UpdateUI();
            });
        }
    }

    public void UpdateUI()
    {
        if (TechManager.Instance.GetSaveData() == null) return;

        int points = TechManager.Instance.GetSaveData().ownedTechPoints;
        int totalBought = TechManager.Instance.GetSaveData().totalTechPointsBought;
        long nextCost = GameBalance.CalculateTechPointCost(totalBought);
        if (techPointsLabel != null) techPointsLabel.text = $"Tech Points: {points}";
        if (buyPointBtn != null)
        {
            buyPointBtn.text = $"Buy Point (${NumberFormatter.Format(nextCost)})";
            buyPointBtn.SetEnabled(TechManager.Instance.CanBuyTechPoint());
        }
    }

    private void OnBuyPointClicked()
    {
        TechManager.Instance.BuyTechPoint();
        UpdateUI();
    }

    // --- Pan / Zoom Logic ---

    private void OnScrollWheel(WheelEvent evt)
    {
        // Simple zoom
        float newZoom = zoom - evt.delta.y * 0.1f;
        zoom = Mathf.Clamp(newZoom, 0.5f, 2.0f);
        if (contentContainer != null)
            contentContainer.style.scale = new Scale(new Vector3(zoom, zoom, 1));
        evt.StopPropagation();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        // Don't pan if we clicked a node or button
        var target = evt.target as VisualElement;
        if (target != null && (target.ClassListContains("tech-node") || target.ClassListContains("tech-node-label")))
        {
            return;
        }

        if (evt.button == 0 || evt.button == 2) // Middle or Left drag usually
        {
            isPanning = true;
            panStart = evt.position;
            // No simple way to get current translation if set via style only.
            // Rely on accumulator 'currentTranslation'.
            if (viewport != null) viewport.CapturePointer(evt.pointerId);
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isPanning && viewport != null && viewport.HasPointerCapture(evt.pointerId))
        {
            Vector2 delta = (Vector2)evt.position - panStart;
            panStart = evt.position; // Delta from last frame

            currentTranslation += delta;

            if (contentContainer != null)
                contentContainer.style.translate = new Translate(currentTranslation.x, currentTranslation.y, 0);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (isPanning)
        {
            isPanning = false;
            if (viewport != null) viewport.ReleasePointer(evt.pointerId);
        }
    }
    private void CenterContent()
    {
        if (viewport == null) return;

        if (float.IsNaN(viewport.resolvedStyle.width) || viewport.resolvedStyle.width == 0)
        {
            viewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            return;
        }

        // Center is half width/height. 
        // Assuming node (0,0) is at (0,0) locally, we just translate to center of viewport.
        float x = viewport.resolvedStyle.width / 2f;
        float y = viewport.resolvedStyle.height / 2f;
        currentTranslation = new Vector2(x, y);

        if (contentContainer != null)
            contentContainer.style.translate = new Translate(currentTranslation.x, currentTranslation.y, 0);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        viewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        CenterContent();
    }
}

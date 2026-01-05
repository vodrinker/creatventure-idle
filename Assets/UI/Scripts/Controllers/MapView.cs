using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class MapView
{
    private VisualElement gridContainer;
    private UIManager uiManager;
    private GameManager gameManager;

    // Słownik przechowuje referencje do już istniejących kafelków
    private Dictionary<Vector2Int, VisualElement> tileElements = new Dictionary<Vector2Int, VisualElement>();

    public MapView(VisualElement rootElement, UIManager uiManager, GameManager gameManager)
    {
        this.uiManager = uiManager;
        this.gameManager = gameManager;

        // 1. Znajdź główny kontener siatki (który jest w UXML)
        gridContainer = rootElement.Q<VisualElement>("GridContainer");

        // 2. Nie czyść go, nie dodawaj klas - on już istnieje i jest gotowy

        if (this.gameManager != null)
        {
            // 3. Wypełnij siatkę danymi
            BindMapData(this.gameManager.Map);

            // 4. Zarejestruj się na zmiany
            this.gameManager.OnNodeUpdated += UpdateNodeVisuals;
        }
    }

    public void UnregisterCallbacks()
    {
        if (gameManager != null)
        {
            gameManager.OnNodeUpdated -= UpdateNodeVisuals;
        }
    }

    private void BindMapData(Dictionary<Vector2Int, NodeData> mapData)
    {
        tileElements.Clear();

        int yOffset = 11 / 2;
        int xOffset = 7 / 2;

        // 1. Znajdź wszystkie rzędy (załóżmy, że mają klasę .map-row)
        // Twoja struktura ze screena to: #GridContainer > TemplateContainer > #RowRoot (.map-row)
        // Query znajdzie je wszystkie, niezależnie od zagnieżdżenia w TemplateContainer
        var allRows = gridContainer.Query<VisualElement>(className: "map-row").ToList();

        if (allRows.Count != 11)
        {
            Debug.LogError($"MapView Error: Oczekiwano 11 rzędów (.map-row), ale znaleziono {allRows.Count}.");
            return;
        }

        for (int y = yOffset; y >= -yOffset; y--)
        {
            // Mapowanie y (5..-5) na indeks listy (0..10)
            int rowIndex = yOffset - y;
            var mapRow = allRows[rowIndex];

            // 2. Znajdź wszystkie kafelki (o nazwie #TileRoot) wewnątrz tego rzędu
            var tilesInRow = mapRow.Query<VisualElement>(name: "TileRoot").ToList();

            if (tilesInRow.Count != 7)
            {
                Debug.LogError($"MapView Error: Oczekiwano 7 kafelków (#TileRoot) w rzędzie {rowIndex}, ale znaleziono {tilesInRow.Count}.");
                continue;
            }

            for (int x = -xOffset; x <= xOffset; x++)
            {
                var coordinates = new Vector2Int(x, y);
                var nodeData = gameManager.GetNodeAt(coordinates);

                // Mapowanie x (-3..3) na indeks listy (0..6)
                int tileIndex = x + xOffset;

                // 3. Pobierz kafelek, który JUŻ ISTNIEJE w UXML
                VisualElement tileElement = tilesInRow[tileIndex];

                // 4. Zapisz go w słowniku i podepnij logikę
                tileElements.Add(coordinates, tileElement);
                UpdateNodeVisuals(nodeData);
                tileElement.RegisterCallback<ClickEvent>(evt => OnTileClicked(coordinates));
            }
        }
    }

    // Reszta skryptu (UpdateNodeVisuals i OnTileClicked) jest
    // DOKŁADNIE TAKA SAMA jak w poprzedniej wersji i nie wymaga zmian.

    private void UpdateNodeVisuals(NodeData nodeData)
    {
        if (nodeData == null || !tileElements.ContainsKey(nodeData.Coordinates)) return;

        var tileElement = tileElements[nodeData.Coordinates];
        var tileIcon = tileElement.Q<VisualElement>("TileIcon");
        var enemyHPBar = tileElement.Q<VisualElement>("EnemyHPBar");
        var enemyHPFill = tileElement.Q<VisualElement>("EnemyHPFill");
        var playerHPBar = tileElement.Q<VisualElement>("PlayerHPBar");
        var playerHPFill = tileElement.Q<VisualElement>("PlayerHPFill");

        if (nodeData.isVisible)
        {
            tileIcon.style.backgroundImage = new StyleBackground(nodeData.NodeSprite);
            tileElement.pickingMode = PickingMode.Position;

            if (nodeData.isOwned)
            {
                tileElement.AddToClassList("map-tile--owned");
            }
            else
            {
                tileElement.RemoveFromClassList("map-tile--owned");
            }

            if (nodeData.isOwned && nodeData.AssignedCreature != null)
            {
                // Logic for Player HP Bar
                playerHPBar.style.display = DisplayStyle.Flex;
                float playerHPPercent = nodeData.AssignedCreature.CurrentHealth / nodeData.AssignedCreature.MaxHealth * 100f;
                playerHPFill.style.width = new Length(playerHPPercent, LengthUnit.Percent);

                // Logic for Enemy HP Bar - show only if fighting active enemy
                if (nodeData.Enemy != null && nodeData.Enemy.IsAlive && !nodeData.IsHealing)
                {
                    enemyHPBar.style.display = DisplayStyle.Flex;
                    float enemyHPPercent = nodeData.Enemy.CurrentHealth / nodeData.Enemy.MaxHealth * 100f;
                    enemyHPFill.style.width = new Length(enemyHPPercent, LengthUnit.Percent);
                }
                else
                {
                    enemyHPBar.style.display = DisplayStyle.None;
                }
            }
            else
            {
                playerHPBar.style.display = DisplayStyle.None;
                enemyHPBar.style.display = DisplayStyle.None;
            }
        }
        else
        {
            tileIcon.style.backgroundImage = new StyleBackground(gameManager.GetLockedNodeSprite());
            tileElement.pickingMode = PickingMode.Ignore;
            // Ensure bars are hidden for locked/invisible nodes
            if (playerHPBar != null) playerHPBar.style.display = DisplayStyle.None;
            if (enemyHPBar != null) enemyHPBar.style.display = DisplayStyle.None;
        }
    }

    private void OnTileClicked(Vector2Int coordinates)
    {
        if (gameManager == null) return;
        NodeData nodeData = gameManager.GetNodeAt(coordinates);
        if (nodeData != null && nodeData.isVisible)
        {
            uiManager.OpenMapTilePopup(nodeData);
        }
    }
}
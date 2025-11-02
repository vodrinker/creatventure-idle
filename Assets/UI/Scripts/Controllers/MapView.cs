using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class MapView
{
    private VisualElement gridContainer;
    private UIManager uiManager;

    // Słownik przechowuje referencje do już istniejących kafelków
    private Dictionary<Vector2Int, VisualElement> tileElements = new Dictionary<Vector2Int, VisualElement>();

    public MapView(VisualElement rootElement, UIManager uiManager)
    {
        this.uiManager = uiManager;

        // 1. Znajdź główny kontener siatki (który jest w UXML)
        gridContainer = rootElement.Q<VisualElement>("GridContainer");

        // 2. Nie czyść go, nie dodawaj klas - on już istnieje i jest gotowy

        // 3. Wypełnij siatkę danymi
        BindMapData(GameManager.Instance.Map);

        // 4. Zarejestruj się na zmiany (tak jak wcześniej)
        GameManager.Instance.OnNodeStateChanged += UpdateNodeVisuals;
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
                var nodeData = GameManager.Instance.GetNodeAt(coordinates);

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

        if (nodeData.isVisible)
        {
            tileIcon.style.backgroundImage = new StyleBackground(nodeData.Definition.nodeSprite);
            tileElement.pickingMode = PickingMode.Position;
        }
        else
        {
            tileIcon.style.backgroundImage = new StyleBackground(GameManager.Instance.GetLockedNodeSprite());
            tileElement.pickingMode = PickingMode.Ignore;
        }
    }

    private void OnTileClicked(Vector2Int coordinates)
    {
        NodeData nodeData = GameManager.Instance.GetNodeAt(coordinates);
        if (nodeData != null && nodeData.isVisible)
        {
            uiManager.OpenMapTilePopup(nodeData);
        }
    }
}
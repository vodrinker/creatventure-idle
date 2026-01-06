using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AspectRatioBox : VisualElement
{
    // === Atrybuty UXML (zastępują UxmlTraits) ===

    [UxmlAttribute("ratio-x")]
    public float RatioX
    {
        get => m_RatioX;
        set
        {
            m_RatioX = value;
            UpdateAspectRatio();
        }
    }
    private float m_RatioX = 1.0f;

    [UxmlAttribute("ratio-y")]
    public float RatioY
    {
        get => m_RatioY;
        set
        {
            m_RatioY = value;
            UpdateAspectRatio();
        }
    }
    private float m_RatioY = 1.0f;

    [UxmlAttribute("fit-to-parent")]
    public bool FitToParent { get; set; } = false;

    // === Logika Głównego Elementu ===

    public float AspectRatio { get; private set; } = 1.0f;

    public AspectRatioBox()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        UpdateAspectRatio();
    }

    private void UpdateAspectRatio()
    {
        if (m_RatioY > 0)
            AspectRatio = m_RatioX / m_RatioY;
        else
            AspectRatio = 1.0f;
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (FitToParent)
        {
            // TRYB 1: Dopasowanie do rodzica (dla #GridContainer 7:11)
            FitToParentLogic();
        }
        else
        {
            // TRYB 2: Dopasowanie do szerokości Flex (dla #MapTile 1:1)
            FlexChildLogic();
        }
    }

    /// <summary>
    /// Logika dla siatki: dopasowuje się do granic rodzica,
    /// ustawiając WŁASNĄ szerokość i wysokość.
    /// </summary>
    private void FitToParentLogic()
    {
        var parent = this.parent;
        if (parent == null) return;

        float parentWidth = parent.resolvedStyle.width;
        float parentHeight = parent.resolvedStyle.height;

        if (float.IsNaN(parentWidth) || float.IsNaN(parentHeight) || parentWidth <= 0 || parentHeight <= 0)
            return;

        // AspectRatio = szerokość / wysokość (np. 7/11)
        float heightBasedOnWidth = parentWidth / AspectRatio;
        float widthBasedOnHeight = parentHeight * AspectRatio;

        float newWidth;
        float newHeight;

        // Wybieramy mniejszy wymiar, aby się zmieścić
        if (heightBasedOnWidth <= parentHeight)
        {
            // Ograniczeniem jest szerokość rodzica (letterboxing)
            newWidth = parentWidth;
            newHeight = heightBasedOnWidth;
        }
        else
        {
            // Ograniczeniem jest wysokość rodzica (pillarboxing)
            newWidth = widthBasedOnHeight;
            newHeight = parentHeight;
        }

        if (Mathf.Approximately(resolvedStyle.width, newWidth) &&
            Mathf.Approximately(resolvedStyle.height, newHeight))
            return;

        // Ustawiamy OBA wymiary
        style.width = newWidth;
        style.height = newHeight;
    }

    /// <summary>
    /// Logika dla kafelka: odczytuje szerokość nadaną przez Flex
    /// i ustawia WŁASNĄ wysokość.
    /// </summary>
    private void FlexChildLogic()
    {
        float flexWidth = resolvedStyle.width;

        // --- POPRAWKA DLA PODGLĄDU W UI BUILDER ---
#if UNITY_EDITOR
        if (!Application.isPlaying && (float.IsNaN(flexWidth) || flexWidth <= 0))
        {
            if (parent != null && parent.name == "unity-content-viewport")
            {
                flexWidth = 100f; // Domyślny rozmiar dla podglądu kafelka
            }
        }
#endif
        // --- KONIEC POPRAWKI ---

        if (float.IsNaN(flexWidth) || flexWidth <= 0)
            return;

        float newHeight = flexWidth / AspectRatio; // AspectRatio = 1/1

        if (Mathf.Approximately(resolvedStyle.height, newHeight))
            return;

        // Ustawiamy TYLKO wysokość
        style.height = newHeight;
    }
}
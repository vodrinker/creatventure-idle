using UnityEngine;
using UnityEngine.UIElements;

public class AspectRatioBox : VisualElement
{
    // === UXML Factory & Traits (z nowym atrybutem) ===
    public new class UxmlFactory : UxmlFactory<AspectRatioBox, UxmlTraits>
    { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private UxmlFloatAttributeDescription m_ratioX =
            new UxmlFloatAttributeDescription { name = "ratio-x", defaultValue = 1.0f };

        private UxmlFloatAttributeDescription m_ratioY =
            new UxmlFloatAttributeDescription { name = "ratio-y", defaultValue = 1.0f };

        // NOWY ATRYBUT: Przełącznik trybu dopasowania
        private UxmlBoolAttributeDescription m_fitToParent =
            new UxmlBoolAttributeDescription { name = "fit-to-parent", defaultValue = false };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var box = ve as AspectRatioBox;

            float x = m_ratioX.GetValueFromBag(bag, cc);
            float y = m_ratioY.GetValueFromBag(bag, cc);

            if (y > 0)
                box.AspectRatio = x / y;
            else
                box.AspectRatio = 1.0f;

            // Odczytujemy nowy atrybut z UXML
            box.FitToParent = m_fitToParent.GetValueFromBag(bag, cc);
        }
    }

    // === Logika Głównego Elementu ===

    public float AspectRatio { get; set; } = 1.0f;

    /// <summary>
    /// Jeśli true, element dopasuje się do rodzica (dla #GridContainer).
    /// Jeśli false, dopasuje się do szerokości flex (dla #MapTile).
    /// </summary>
    public bool FitToParent { get; set; } = false;

    public AspectRatioBox()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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
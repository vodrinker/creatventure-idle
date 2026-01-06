using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TechnologyEditorWindow : EditorWindow
{
    private enum TechTab { Game, Meta }
    private TechTab currentTab = TechTab.Game;

    private Vector2 scrollPosition;
    private const float GridSize = 50f;
    private const float NodeSize = 40f;
    private float zoom = 1.0f;

    private List<TechnologySO> currentNodes = new List<TechnologySO>();

    // Interaction state
    private TechnologySO connectionStartNode = null;
    private bool isDraggingConnection = false;

    // Node Dragging
    private TechnologySO draggedNode = null;
    private bool isDraggingNode = false;
    private Vector2 nodeDragOffset; // Offset from node center to mouse click point
    private Vector2 dragStartPos;   // Screen position where mouse went down

    // Connection selection
    private TechnologySO selectedConnectionSource = null;
    private TechnologySO selectedConnectionTarget = null;

    // Creation popup state
    private bool showCreationPopup = false;
    private Vector2Int creationGridPos;
    private string newTechName = "";
    private Rect creationPopupRect;

    // Node popup state
    private TechnologySO popupNode = null;
    private Rect nodePopupRect;

    // Node value buffers
    private string tempName = "";
    private string tempDescription = "";

    [MenuItem("Tools/Technology Manager")]
    public static void ShowWindow()
    {
        GetWindow<TechnologyEditorWindow>("Technology Manager");
    }

    private void OnEnable()
    {
        LoadNodes();
    }

    private void OnFocus()
    {
        LoadNodes();
    }

    private void LoadNodes()
    {
        string path = currentTab == TechTab.Game ? "Technologies/Game" : "Technologies/Meta";
        currentNodes.Clear();
        var loaded = Resources.LoadAll<TechnologySO>(path);
        currentNodes.AddRange(loaded);
    }

    private void OnGUI()
    {
        DrawToolbar();

        // Get available space for the grid
        Rect contentRect = GUILayoutUtility.GetRect(position.width, position.height - 20, GUI.skin.box);

        Event e = Event.current;

        // Zoom (ScrollWheel)
        if (e.type == EventType.ScrollWheel && contentRect.Contains(e.mousePosition))
        {
            float zoomDelta = -e.delta.y * 0.05f;
            zoom = Mathf.Clamp(zoom + zoomDelta, 0.2f, 3.0f);
            e.Use();
        }

        // Handle Pan
        if (e.type == EventType.MouseDrag && (e.button == 2 || e.alt))
        {
            scrollPosition -= e.delta / zoom;
            Repaint();
        }

        DrawGrid(contentRect);
        DrawConnections(contentRect);
        DrawNodes(contentRect);

        // Draw Dragging Line (Line ONLY)
        if (isDraggingConnection && connectionStartNode != null)
        {
            Vector2 startPos = GridToScreen(connectionStartNode.position, contentRect) + contentRect.position;
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(Texture2D.whiteTexture, 5f * zoom, startPos, e.mousePosition);
            Repaint();
        }

        ProcessEvents(e, contentRect);

        DrawCreationPopup();
        DrawNodePopup();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();

        GUI.color = currentTab == TechTab.Game ? Color.green : Color.white;
        if (GUILayout.Button("Game Progression", EditorStyles.toolbarButton)) currentTab = TechTab.Game;

        GUI.color = currentTab == TechTab.Meta ? Color.cyan : Color.white;
        if (GUILayout.Button("Meta Progression", EditorStyles.toolbarButton)) currentTab = TechTab.Meta;

        GUI.color = Color.white;

        if (EditorGUI.EndChangeCheck())
        {
            LoadNodes();
            popupNode = null;
            showCreationPopup = false;
            selectedConnectionSource = null;
            selectedConnectionTarget = null;
            draggedNode = null; // Cancel drag
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
        {
            LoadNodes();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawGrid(Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;

        GUI.BeginClip(rect);
        GL.PushMatrix();
        GL.Clear(true, false, Color.black);

        EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.15f, 0.15f, 0.15f));

        Vector2 center = rect.size / 2f;

        Vector2 worldOriginScreen = center - scrollPosition * zoom;
        float scaledGridSize = GridSize * zoom;

        Vector2 offset = new Vector2(worldOriginScreen.x % scaledGridSize, worldOriginScreen.y % scaledGridSize);

        Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        for (float x = offset.x - scaledGridSize; x < rect.width; x += scaledGridSize)
            Handles.DrawLine(new Vector2(x, 0), new Vector2(x, rect.height));

        for (float y = offset.y - scaledGridSize; y < rect.height; y += scaledGridSize)
            Handles.DrawLine(new Vector2(0, y), new Vector2(rect.width, y));

        // Highlight (0,0) CELL
        Vector2 zeroCellCenter = GridToScreen(Vector2Int.zero, rect);
        float halfSize = scaledGridSize / 2f;
        Rect zeroCellRect = new Rect(zeroCellCenter.x - halfSize, zeroCellCenter.y - halfSize, scaledGridSize, scaledGridSize);

        Handles.color = new Color(0.5f, 0.8f, 0.5f, 0.5f);
        Handles.DrawSolidRectangleWithOutline(zeroCellRect, new Color(0, 0, 0, 0), new Color(0.5f, 0.8f, 0.5f, 1f));

        GL.PopMatrix();
        GUI.EndClip();
    }

    private void DrawNodes(Rect rect)
    {
        GUI.BeginClip(rect);
        foreach (var node in currentNodes)
        {
            Vector2 pos;
            if (isDraggingNode && node == draggedNode)
            {
                // Draw based on Mouse Position (converted to local clip space) + Offset
                Vector2 mouseInClip = Event.current.mousePosition - rect.position;
                pos = mouseInClip + nodeDragOffset;
            }
            else
            {
                pos = GridToScreen(node.position, rect);
            }

            float s = NodeSize * zoom;
            Rect nodeRect = new Rect(pos.x - s / 2, pos.y - s / 2, s, s);

            // Draw shadow
            EditorGUI.DrawRect(new Rect(nodeRect.x - 2, nodeRect.y - 2, nodeRect.width + 4, nodeRect.height + 4), Color.black);

            // Base Color
            Color baseColor = currentTab == TechTab.Game ? new Color(0.28f, 0.65f, 0.28f) : new Color(0.2f, 0.5f, 0.8f);

            // Highlight (Lighter version)
            if (node == popupNode)
            {
                // Make it lighter - significantly brighter green as requested
                baseColor = currentTab == TechTab.Game ? new Color(0.5f, 1.0f, 0.5f) : new Color(0.4f, 0.7f, 1.0f);
            }

            EditorGUI.DrawRect(nodeRect, baseColor);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = Mathf.RoundToInt(12 * zoom);
            GUI.Label(nodeRect, node.displayName, labelStyle);
        }
        GUI.EndClip();
    }

    private void DrawConnections(Rect contentRect)
    {
        Handles.BeginGUI();
        foreach (var node in currentNodes)
        {
            if (node.connectedNodes == null) continue;
            foreach (var connected in node.connectedNodes)
            {
                if (connected == null) continue;
                Vector2 startCenter = (isDraggingNode && node == draggedNode)
                    ? (Event.current.mousePosition + nodeDragOffset) + contentRect.position
                    : GridToScreen(node.position, contentRect) + contentRect.position;

                Vector2 endCenter = (isDraggingNode && connected == draggedNode)
                    ? (Event.current.mousePosition + nodeDragOffset) + contentRect.position
                    : GridToScreen(connected.position, contentRect) + contentRect.position;

                bool isSelected = (node == selectedConnectionSource && connected == selectedConnectionTarget);

                float s = NodeSize * zoom;
                Vector2 startEdge = GetPointOnRectEdge(startCenter, s, s, endCenter);
                Vector2 endEdge = GetPointOnRectEdge(endCenter, s, s, startCenter);

                DrawConnectionWedge(startEdge, endEdge, isSelected);
            }
        }
        Handles.EndGUI();
    }

    // Gets point on the edge of a rectangle (centered at rectCenter with sizes w, h)
    private Vector2 GetPointOnRectEdge(Vector2 rectCenter, float w, float h, Vector2 targetPoint)
    {
        Vector2 dir = targetPoint - rectCenter;

        float halfW = w / 2f;
        float halfH = h / 2f;

        if (dir == Vector2.zero) return rectCenter;

        float tX = (dir.x != 0) ? halfW / Mathf.Abs(dir.x) : float.PositiveInfinity;
        float tY = (dir.y != 0) ? halfH / Mathf.Abs(dir.y) : float.PositiveInfinity;

        float t = Mathf.Min(tX, tY);

        return rectCenter + dir * t;
    }

    private void DrawConnectionWedge(Vector2 start, Vector2 end, bool selected = false)
    {
        Handles.color = selected ? Color.yellow : Color.white;

        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);

        float dist = Vector2.Distance(start, end);
        if (dist < 1f) return;

        // Wedge Geometry
        float startWidth = (selected ? 12f : 8f) * zoom;

        Vector2 startLeft = start + normal * (startWidth * 0.5f);
        Vector2 startRight = start - normal * (startWidth * 0.5f);
        Vector2 endPoint = end;

        Handles.DrawAAConvexPolygon(startLeft, startRight, endPoint);
    }

    private Vector2 GridToScreen(Vector2Int gridPos, Rect rect)
    {
        Vector2 center = rect.size / 2f;
        Vector2 worldPos = new Vector2(gridPos.x * GridSize, -gridPos.y * GridSize);
        Vector2 relative = (worldPos - scrollPosition) * zoom;
        return center + relative + new Vector2(GridSize * zoom / 2f, -GridSize * zoom / 2f);
    }

    private Vector2Int ScreenToGrid(Vector2 windowMousePos, Rect rect)
    {
        Vector2 relativeMouse = windowMousePos - rect.position;
        Vector2 center = rect.size / 2f;

        Vector2 offsetScaled = new Vector2(GridSize * zoom / 2f, -GridSize * zoom / 2f);

        Vector2 fromCenter = relativeMouse - center - offsetScaled;
        Vector2 unscaled = fromCenter / zoom;
        Vector2 worldPos = unscaled + scrollPosition;

        int gx = Mathf.RoundToInt(worldPos.x / GridSize);
        int gy = Mathf.RoundToInt(-worldPos.y / GridSize);

        return new Vector2Int(gx, gy);
    }

    private void ProcessEvents(Event e, Rect contentRect)
    {
        if (e.type == EventType.MouseDown)
        {
            if (e.alt || e.button == 2) return;

            // PRE-CALCULATE HIT NODE
            TechnologySO clickedNode = null;
            float hitRadius = (NodeSize * zoom) / 2f;
            foreach (var node in currentNodes)
            {
                Vector2 nodePos = GridToScreen(node.position, contentRect) + contentRect.position;
                if (Vector2.Distance(e.mousePosition, nodePos) < hitRadius)
                {
                    clickedNode = node;
                    break;
                }
            }

            // 1. Popup Blockers
            if (showCreationPopup)
            {
                if (creationPopupRect.Contains(e.mousePosition)) return;
                showCreationPopup = false; e.Use(); Repaint(); return;
            }

            if (popupNode != null)
            {
                if (nodePopupRect.Contains(e.mousePosition)) return;

                // If we hit a node, don't return! We want to allow drag/select.
                if (clickedNode == null)
                {
                    // Clicked empty space -> Deselect and consume
                    popupNode = null;
                    Repaint();
                    e.Use();
                    return;
                }
            }

            if (!contentRect.Contains(e.mousePosition)) return;

            if (e.button == 0) // Left Click
            {
                if (clickedNode != null)
                {
                    // SELECT
                    popupNode = clickedNode;

                    // Update buffers
                    if (popupNode != null)
                    {
                        GUI.FocusControl(null); // Clear focus to prevent old value retention
                        tempName = popupNode.displayName;
                        tempDescription = popupNode.description;
                    }

                    // PREPARE DRAG (Don't start yet)
                    draggedNode = clickedNode;
                    isDraggingNode = false; // Wait for threshold
                    dragStartPos = e.mousePosition;

                    Vector2 nodeCenter = GridToScreen(clickedNode.position, contentRect) + contentRect.position;
                    nodeDragOffset = nodeCenter - e.mousePosition;

                    // Reset others
                    nodePopupRect = new Rect(e.mousePosition.x - 100, e.mousePosition.y - 100, 200, 160);
                    showCreationPopup = false;
                    selectedConnectionSource = null;
                    selectedConnectionTarget = null;
                }
                else
                {
                    // Check Connection Click
                    bool connectionClicked = false;
                    foreach (var node in currentNodes)
                    {
                        if (node.connectedNodes == null) continue;
                        foreach (var connected in node.connectedNodes)
                        {
                            Vector2 startCenter = GridToScreen(node.position, contentRect) + contentRect.position;
                            Vector2 endCenter = GridToScreen(connected.position, contentRect) + contentRect.position;
                            float dist = HandleUtility.DistancePointLine(e.mousePosition, startCenter, endCenter);
                            if (dist < 10f)
                            {
                                if (selectedConnectionSource == node && selectedConnectionTarget == connected)
                                {
                                    Undo.RecordObject(node, "Disconnect Node");
                                    node.connectedNodes.Remove(connected);
                                    EditorUtility.SetDirty(node);
                                    selectedConnectionSource = null;
                                    selectedConnectionTarget = null;
                                }
                                else
                                {
                                    selectedConnectionSource = node;
                                    selectedConnectionTarget = connected;
                                }
                                connectionClicked = true;
                                break;
                            }
                        }
                        if (connectionClicked) break;
                    }

                    if (connectionClicked)
                    {
                        Repaint();
                    }
                    else
                    {
                        selectedConnectionSource = null;
                        selectedConnectionTarget = null;

                        Vector2Int gridPos = ScreenToGrid(e.mousePosition, contentRect);
                        bool occupied = currentNodes.Any(n => n.position == gridPos);

                        if (!occupied && contentRect.Contains(e.mousePosition))
                        {
                            showCreationPopup = true;
                            creationGridPos = gridPos;
                            creationPopupRect = new Rect(e.mousePosition.x - 100, e.mousePosition.y - 50, 200, 80);
                            newTechName = "";
                            popupNode = null;
                            GUI.FocusControl("NewTechName");
                        }
                    }
                }
            }
            else if (e.button == 1) // Right Click
            {
                if (clickedNode != null)
                {
                    isDraggingConnection = true;
                    connectionStartNode = clickedNode;
                }
            }
            Repaint();
        }
        else if (e.type == EventType.MouseDrag)
        {
            if (draggedNode != null)
            {
                // Check Threshold if not yet dragging
                if (!isDraggingNode)
                {
                    if (Vector2.Distance(e.mousePosition, dragStartPos) > 5f)
                    {
                        isDraggingNode = true;
                    }
                }

                if (isDraggingNode)
                {
                    Repaint();
                }
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            if (draggedNode != null)
            {
                // Only drop if we actually dragged
                if (isDraggingNode)
                {
                    // FINISH DRAG
                    Vector2Int newGridPos = ScreenToGrid(e.mousePosition, contentRect);
                    bool occupied = currentNodes.Any(n => n.position == newGridPos && n != draggedNode);

                    if (!occupied && contentRect.Contains(e.mousePosition))
                    {
                        if (draggedNode.position != newGridPos)
                        {
                            Undo.RecordObject(draggedNode, "Move Node");
                            draggedNode.position = newGridPos;
                            EditorUtility.SetDirty(draggedNode);
                        }
                    }
                }

                // Cleanup regardless
                isDraggingNode = false;
                draggedNode = null;
                Repaint();
            }

            if (isDraggingConnection && e.button == 1)
            {
                isDraggingConnection = false;
                TechnologySO dropNode = null;
                float hitRadius = (NodeSize * zoom) / 2f;
                foreach (var node in currentNodes)
                {
                    Vector2 nodePos = GridToScreen(node.position, contentRect) + contentRect.position;
                    if (Vector2.Distance(e.mousePosition, nodePos) < hitRadius)
                    {
                        dropNode = node;
                        break;
                    }
                }

                if (dropNode != null && dropNode != connectionStartNode)
                {
                    Undo.RecordObject(connectionStartNode, "Connect Node");
                    if (!connectionStartNode.connectedNodes.Contains(dropNode))
                    {
                        connectionStartNode.connectedNodes.Add(dropNode);
                        EditorUtility.SetDirty(connectionStartNode);
                    }
                }
                connectionStartNode = null;
                Repaint();
            }
        }
        else if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                showCreationPopup = false;
                popupNode = null;
                selectedConnectionSource = null;
                selectedConnectionTarget = null;
                Repaint();
            }

            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            {
                if (selectedConnectionSource != null && selectedConnectionTarget != null)
                {
                    Undo.RecordObject(selectedConnectionSource, "Disconnect Node");
                    selectedConnectionSource.connectedNodes.Remove(selectedConnectionTarget);
                    EditorUtility.SetDirty(selectedConnectionSource);

                    selectedConnectionSource = null;
                    selectedConnectionTarget = null;
                    Repaint();
                    e.Use();
                }
            }
        }
    }

    private void DrawCreationPopup()
    {
        if (!showCreationPopup) return;

        EditorGUI.DrawRect(creationPopupRect, new Color(0.15f, 0.15f, 0.15f, 1f));
        GUI.Box(creationPopupRect, GUIContent.none);

        GUILayout.BeginArea(creationPopupRect);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginVertical();

        GUILayout.Label($"New Node at {creationGridPos}", EditorStyles.boldLabel);
        GUI.SetNextControlName("NewTechName");
        newTechName = GUILayout.TextField(newTechName);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
        {
            showCreationPopup = false;
        }
        if (GUILayout.Button("Create") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
        {
            CreateNode(newTechName, creationGridPos);
            showCreationPopup = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.EndArea();
    }

    private void DrawNodePopup()
    {
        if (popupNode == null) return;

        // Calculate Height
        float width = 200f;
        float padding = 10f;
        float baseHeight = 105f; // Header, Name, Label spacing, ID, margins

        float descHeight = EditorStyles.textArea.CalcHeight(new GUIContent(tempDescription), width - padding * 2);
        descHeight = Mathf.Max(descHeight, 40f); // Minimum height

        nodePopupRect.width = width;
        nodePopupRect.height = baseHeight + descHeight;

        EditorGUI.DrawRect(nodePopupRect, new Color(0.1f, 0.1f, 0.1f, 0.95f));
        GUI.Box(nodePopupRect, GUIContent.none);

        GUILayout.BeginArea(nodePopupRect);
        GUILayout.Space(10);

        // Header / Name
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        EditorGUI.BeginChangeCheck();

        GUILayout.Label("Display Name", EditorStyles.miniLabel);
        string newName = EditorGUILayout.TextField(tempName);

        GUILayout.Space(5);
        GUILayout.Label("Description", EditorStyles.miniLabel);
        string newDesc = EditorGUILayout.TextArea(tempDescription, GUILayout.Height(descHeight));

        if (EditorGUI.EndChangeCheck())
        {
            tempName = newName;
            tempDescription = newDesc;

            Undo.RecordObject(popupNode, "Modify Node");
            popupNode.displayName = tempName;
            popupNode.description = tempDescription;
            EditorUtility.SetDirty(popupNode);
        }

        GUILayout.Space(10);

        // ID Style
        GUIStyle idStyle = new GUIStyle(EditorStyles.label);
        idStyle.fontSize = 10;
        idStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        GUILayout.Label($"ID: {popupNode.name}", idStyle);

        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.EndArea();
    }

    private void CreateNode(string displayName, Vector2Int pos)
    {
        string folder = currentTab == TechTab.Game ? "Assets/Resources/Technologies/Game" : "Assets/Resources/Technologies/Meta";

        if (!System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        int id = 1;
        while (System.IO.File.Exists($"{folder}/technology_{id}.asset"))
        {
            id++;
        }

        TechnologySO newNode = ScriptableObject.CreateInstance<TechnologySO>();
        newNode.displayName = displayName;
        newNode.position = pos;

        AssetDatabase.CreateAsset(newNode, $"{folder}/technology_{id}.asset");
        AssetDatabase.SaveAssets();

        LoadNodes();
    }
}

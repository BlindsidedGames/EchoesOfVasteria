using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Quests;
using UnityEditor;
using UnityEngine;

/// <summary>
///     An editor window to visualize the quest flow graph automatically from QuestData assets.
///     Features a fully pannable and zoomable infinite canvas with a robust layout algorithm.
///     Controls:
///     - Pan: Hold Alt + Left-Click and Drag, OR Middle-Click and Drag
///     - Zoom: Mouse Scroll Wheel
///     - Select/Drag Node: Left-Click and Drag
/// </summary>
public class QuestFlowWindow : EditorWindow
{
    private class Node
    {
        public Rect rect; // Position and size in Canvas Space
        public readonly string title;
        public readonly GUIStyle style;
        public readonly QuestData questData;

        public Node(QuestData questData, Vector2 position, float width, float height)
        {
            this.questData = questData;
            title = questData.questName.GetLocalizedString();
            if (string.IsNullOrEmpty(title)) title = questData.name;
            rect = new Rect(position.x, position.y, width, height);

            style = new GUIStyle();
            style.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            style.border = new RectOffset(12, 12, 12, 12);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
        }
    }

    private struct Connection
    {
        public Node from;
        public Node to;
    }

    private Dictionary<QuestData, Node> questNodeMap;
    private List<Connection> connections;

    private Vector2 panOffset;
    private float zoomScale = 1.0f;
    private const float MinZoom = 0.2f;
    private const float MaxZoom = 2.0f;

    private Node selectedNode;
    private bool isPanning;

    private const float NodeWidth = 180f;
    private const float NodeHeight = 60f;
    private const float HorizontalSpacing = 250f;
    private const float VerticalSpacing = 100f;
    private const float GridSpacing = 20f;

    [MenuItem("Window/Quest Flow Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<QuestFlowWindow>();
        window.titleContent = new GUIContent("Quest Flow");
        window.wantsMouseMove = true;
    }

    private void OnEnable()
    {
        PopulateAndLayoutGraph();
    }

    private void OnGUI()
    {
        DrawGrid();
        DrawConnections();
        DrawNodes();
        DrawToolbar();
        ProcessEvents(Event.current);
        if (GUI.changed) Repaint();
    }

    private void DrawGrid()
    {
        var scaledGrid = GridSpacing * zoomScale;
        var xOffset = panOffset.x % scaledGrid;
        var yOffset = panOffset.y % scaledGrid;

        Handles.BeginGUI();
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        for (var i = xOffset; i < position.width; i += scaledGrid)
            Handles.DrawLine(new Vector3(i, 0, 0), new Vector3(i, position.height, 0));
        for (var i = yOffset; i < position.height; i += scaledGrid)
            Handles.DrawLine(new Vector3(0, i, 0), new Vector3(position.width, i, 0));

        Handles.EndGUI();
    }

    private void DrawNodes()
    {
        if (questNodeMap == null) return;
        var viewRect = new Rect(0, 0, position.width, position.height);
        foreach (var node in questNodeMap.Values)
        {
            var screenPos = node.rect.position * zoomScale + panOffset;
            var screenSize = node.rect.size * zoomScale;
            var screenRect = new Rect(screenPos, screenSize);
            if (viewRect.Overlaps(screenRect, true)) GUI.Box(screenRect, node.title, node.style);
        }
    }

    private void DrawConnections()
    {
        if (connections == null) return;
        Handles.color = Color.white;
        foreach (var c in connections)
        {
            var startPos = new Vector2(c.from.rect.x + c.from.rect.width, c.from.rect.y + c.from.rect.height / 2) *
                zoomScale + panOffset;
            var endPos = new Vector2(c.to.rect.x, c.to.rect.y + c.to.rect.height / 2) * zoomScale + panOffset;
            Handles.DrawBezier(startPos, endPos, startPos + Vector2.right * 50, endPos + Vector2.left * 50, Color.white,
                null, 2f * zoomScale);
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Reload & Layout", EditorStyles.toolbarButton)) PopulateAndLayoutGraph();
        if (GUILayout.Button("Home", EditorStyles.toolbarButton))
        {
            panOffset = Vector2.zero;
            zoomScale = 1.0f;
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Zoom: {zoomScale:F2}");
        EditorGUILayout.EndHorizontal();
    }

    private void PopulateAndLayoutGraph()
    {
        questNodeMap = new Dictionary<QuestData, Node>();
        connections = new List<Connection>();

        var guids = AssetDatabase.FindAssets("t:QuestData");
        var allQuests = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<QuestData>)
            .Where(q => q != null)
            .ToList();

        foreach (var quest in allQuests)
            questNodeMap[quest] = new Node(quest, Vector2.zero, NodeWidth, NodeHeight);

        foreach (var quest in allQuests)
        {
            var toNode = questNodeMap[quest];
            if (quest.requiredQuests != null)
                foreach (var requiredQuest in quest.requiredQuests)
                    if (requiredQuest != null && questNodeMap.ContainsKey(requiredQuest))
                        connections.Add(new Connection { from = questNodeMap[requiredQuest], to = toNode });
        }

        LayoutNodes();
        panOffset = Vector2.zero;
        zoomScale = 1.0f;
        GUI.changed = true;
    }

    /// <summary>
    ///     --- FIX: New layout algorithm to create straight "swimlane" chains ---
    ///     This method calculates node positions to keep quest chains horizontally aligned.
    /// </summary>
    private void LayoutNodes()
    {
        if (questNodeMap == null || !questNodeMap.Any()) return;

        var nodes = questNodeMap.Values.ToList();
        var nodePrerequisites = new Dictionary<Node, List<Node>>();
        var nodeSuccessors = new Dictionary<Node, List<Node>>();
        var nodeColumns = new Dictionary<Node, int>();

        // 1. Initialize data structures
        foreach (var node in nodes)
        {
            nodePrerequisites[node] = new List<Node>();
            nodeSuccessors[node] = new List<Node>();
        }

        foreach (var connection in connections)
        {
            nodePrerequisites[connection.to].Add(connection.from);
            nodeSuccessors[connection.from].Add(connection.to);
        }

        // 2. Calculate column for each node using a breadth-first traversal
        var rootNodes = nodes.Where(n => !nodePrerequisites[n].Any()).ToList();
        var processedNodes = new HashSet<Node>();
        var queue = new Queue<Node>();

        foreach (var root in rootNodes)
        {
            nodeColumns[root] = 0;
            queue.Enqueue(root);
            processedNodes.Add(root);
        }

        while (queue.Any())
        {
            var currentNode = queue.Dequeue();
            foreach (var successor in nodeSuccessors[currentNode])
            {
                // A node's column is the maximum column of its parents + 1
                nodeColumns[successor] =
                    nodePrerequisites[successor].Max(p => nodeColumns.ContainsKey(p) ? nodeColumns[p] : 0) + 1;

                if (!processedNodes.Contains(successor))
                {
                    processedNodes.Add(successor);
                    queue.Enqueue(successor);
                }
            }
        }

        // 3. Position nodes column by column, maintaining vertical alignment
        var yPositionsInUse = new Dictionary<int, List<float>>();
        var nextRootY = 50f;

        // Sort all nodes by their calculated column index to ensure parents are placed before children
        var nodesSortedByColumn =
            processedNodes.OrderBy(n => nodeColumns.ContainsKey(n) ? nodeColumns[n] : int.MaxValue).ToList();

        foreach (var node in nodesSortedByColumn)
        {
            var col = nodeColumns[node];
            float targetY;

            if (nodePrerequisites[node].Any())
            {
                // If it has parents, target the average Y of its parents
                targetY = nodePrerequisites[node].Average(p => p.rect.y);
            }
            else
            {
                // If it's a root node, assign it the next available root Y position
                targetY = nextRootY;
                nextRootY += VerticalSpacing;
            }

            // Ensure we don't collide with another node in the same column
            if (!yPositionsInUse.ContainsKey(col)) yPositionsInUse[col] = new List<float>();

            // Find an empty slot
            while (yPositionsInUse[col].Any(y => Mathf.Abs(y - targetY) < VerticalSpacing - 1))
                targetY += VerticalSpacing; // Nudge it down until a free slot is found

            node.rect.position = new Vector2(50f + col * HorizontalSpacing, targetY);
            yPositionsInUse[col].Add(targetY);
        }

        // 4. Position any leftover (cyclical/unconnected) nodes at the bottom
        var unprocessedNodes = nodes.Except(processedNodes).ToList();
        if (unprocessedNodes.Any())
        {
            Debug.LogWarning(
                $"Quest Flow: Detected {unprocessedNodes.Count} un-placeable nodes, likely due to a cyclical dependency. See the group at the bottom of the graph.");
            var maxPlacedY = nodes.Any() ? nodes.Max(n => n.rect.yMax) : 50f;
            var orphanY = maxPlacedY + VerticalSpacing * 2;
            for (var i = 0; i < unprocessedNodes.Count; i++)
                unprocessedNodes[i].rect.position = new Vector2(50f, orphanY + i * VerticalSpacing);
        }
    }


    private void ProcessEvents(Event e)
    {
        if (questNodeMap == null) return;

        var mousePosInCanvas = (e.mousePosition - panOffset) / zoomScale;

        if (e.type == EventType.MouseDown)
        {
            if ((e.alt && e.button == 0) || e.button == 2)
            {
                isPanning = true;
                selectedNode = null;
                e.Use();
            }
            else if (e.button == 0)
            {
                selectedNode = null;
                var nodesList = questNodeMap.Values.ToList();
                for (var i = nodesList.Count - 1; i >= 0; i--)
                    if (nodesList[i].rect.Contains(mousePosInCanvas))
                    {
                        selectedNode = nodesList[i];
                        questNodeMap.Remove(selectedNode.questData);
                        questNodeMap[selectedNode.questData] = selectedNode;
                        break;
                    }
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            isPanning = false;
            selectedNode = null;
        }
        else if (e.type == EventType.MouseDrag)
        {
            if (isPanning)
                panOffset += e.delta;
            else if (selectedNode != null) selectedNode.rect.position += e.delta / zoomScale;
            e.Use();
        }
        else if (e.type == EventType.ScrollWheel)
        {
            var canvasMousePos = (e.mousePosition - panOffset) / zoomScale;
            var zoomDelta = -e.delta.y * 0.05f;
            var oldZoom = zoomScale;
            zoomScale = Mathf.Clamp(zoomScale + zoomDelta, MinZoom, MaxZoom);

            panOffset += canvasMousePos * oldZoom - canvasMousePos * zoomScale;
            e.Use();
        }

        if (isPanning) EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.Pan);

        GUI.changed = true;
    }
}
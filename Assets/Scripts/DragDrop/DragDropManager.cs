using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;

    [Header("Grid")]
    [SerializeField] private Vector2 cellSize = new Vector2(100f, 100f);
    [SerializeField] private Vector2 gridOffset = Vector2.zero;

    [Header("Clone")]
    [SerializeField] private float cloneAlpha = 0.5f;

    private RectTransform canvasRect;
    private GraphicRaycaster raycaster;
    private DraggableItem currentDrag;
    private RectTransform currentClone;
    private readonly Dictionary<Vector2Int, DraggableItem> grid = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        raycaster = canvas.GetComponent<GraphicRaycaster>();
    }

    // ── Called by DraggableItem ──────────────────────────────────────────────

    public void StartDrag(DraggableItem item, PointerEventData data)
    {
        if (currentDrag != null) return;
        currentDrag = item;
        UnregisterFromGrid(item);
        SpawnClone(item, data);
    }

    public void UpdateDrag(PointerEventData data)
    {
        if (currentClone == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, data.position, data.pressEventCamera, out Vector2 local))
            currentClone.localPosition = local;
    }

    public void EndDrag(DraggableItem item, PointerEventData data)
    {
        if (currentDrag != item) return;

        DestroyClone();

        DropHandler handler = FindHandlerUnderPointer(data);

        if (handler != null && !handler.IsBackground)
        {
            // Custom drop: delegate entirely to the UnityEvent
            handler.OnDrop.Invoke(item);
        }
        else if (handler != null && handler.IsBackground)
        {
            HandleGridDrop(item, data);
        }
        else
        {
            ReturnToStart(item);
        }

        currentDrag = null;
    }

    // ── Grid registration ────────────────────────────────────────────────────

    public void RegisterOnGrid(DraggableItem item)
    {
        Vector2Int cell = LocalToCell(item.RectTransform.localPosition);
        grid[cell] = item;
        item.CurrentCell = cell;
    }

    // ── Internal grid logic ──────────────────────────────────────────────────

    private void UnregisterFromGrid(DraggableItem item)
    {
        if (grid.TryGetValue(item.CurrentCell, out var occupant) && occupant == item)
            grid.Remove(item.CurrentCell);
    }

    private void PlaceOnGrid(DraggableItem item, Vector2Int cell)
    {
        grid[cell] = item;
        item.CurrentCell = cell;
        item.MoveTo(CellToLocal(cell));
    }

    private void ReturnToStart(DraggableItem item)
    {
        grid[item.StartCell] = item;
        item.CurrentCell = item.StartCell;
        item.ReturnToStart();
    }

    private void HandleGridDrop(DraggableItem item, PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, data.position, data.pressEventCamera, out Vector2 local))
        {
            ReturnToStart(item);
            return;
        }

        local = ClampToCanvas(local, item.RectTransform.rect.size);
        Vector2Int targetCell = LocalToCell(local);

        if (grid.ContainsKey(targetCell))
        {
            Vector2Int? free = FindNearestFreeCell(targetCell, item.RectTransform.rect.size);
            if (free.HasValue)
                PlaceOnGrid(item, free.Value);
            else
                ReturnToStart(item);
        }
        else
        {
            PlaceOnGrid(item, targetCell);
        }
    }

    // ── Coordinate helpers ───────────────────────────────────────────────────

    public Vector2Int LocalToCell(Vector2 localPos)
    {
        Vector2 adjusted = localPos - gridOffset;
        return new Vector2Int(
            Mathf.RoundToInt(adjusted.x / cellSize.x),
            Mathf.RoundToInt(adjusted.y / cellSize.y)
        );
    }

    public Vector2 CellToLocal(Vector2Int cell)
    {
        return new Vector2(
            cell.x * cellSize.x + gridOffset.x,
            cell.y * cellSize.y + gridOffset.y
        );
    }

    private Vector2 ClampToCanvas(Vector2 localPos, Vector2 objSize)
    {
        Rect r = canvasRect.rect;
        float hx = objSize.x * 0.5f, hy = objSize.y * 0.5f;
        return new Vector2(
            Mathf.Clamp(localPos.x, r.xMin + hx, r.xMax - hx),
            Mathf.Clamp(localPos.y, r.yMin + hy, r.yMax - hy)
        );
    }

    private bool CellIsInCanvas(Vector2Int cell, Vector2 objSize)
    {
        Vector2 center = CellToLocal(cell);
        Rect r = canvasRect.rect;
        float hx = objSize.x * 0.5f, hy = objSize.y * 0.5f;
        return center.x - hx >= r.xMin && center.x + hx <= r.xMax &&
               center.y - hy >= r.yMin && center.y + hy <= r.yMax;
    }

    // Spiral outward from origin, returns first free cell inside canvas
    private Vector2Int? FindNearestFreeCell(Vector2Int origin, Vector2 objSize)
    {
        for (int radius = 1; radius <= 20; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                    var candidate = origin + new Vector2Int(dx, dy);
                    if (!grid.ContainsKey(candidate) && CellIsInCanvas(candidate, objSize))
                        return candidate;
                }
            }
        }
        return null;
    }

    // ── Raycast ──────────────────────────────────────────────────────────────

    private DropHandler FindHandlerUnderPointer(PointerEventData data)
    {
        var results = new List<RaycastResult>();
        raycaster.Raycast(data, results);

        DropHandler background = null;
        foreach (var result in results)
        {
            var h = result.gameObject.GetComponent<DropHandler>();
            if (h == null) continue;
            if (!h.IsBackground) return h; // specific handler has priority
            background = h;
        }
        return background;
    }

    // ── Clone ────────────────────────────────────────────────────────────────

    private void SpawnClone(DraggableItem item, PointerEventData data)
    {
        var go = Instantiate(item.gameObject, canvas.transform);
        go.transform.SetAsLastSibling();

        foreach (var d in go.GetComponentsInChildren<DraggableItem>()) Destroy(d);
        foreach (var d in go.GetComponentsInChildren<DropHandler>()) Destroy(d);

        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        cg.alpha = cloneAlpha;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        currentClone = go.GetComponent<RectTransform>();
        UpdateDrag(data);
    }

    private void DestroyClone()
    {
        if (currentClone != null) { Destroy(currentClone.gameObject); currentClone = null; }
    }
}

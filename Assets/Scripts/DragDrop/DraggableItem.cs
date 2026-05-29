using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Visual")]
    [SerializeField] private float dragAlpha = 0.5f;

    [Header("Move / Return Animation")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    public UnityEvent onDragStart;
    public UnityEvent onDragEnd;
    public UnityEvent onReturnStart;
    public UnityEvent onReturnComplete;

    public RectTransform RectTransform { get; private set; }

    // Current grid cell (managed by DragDropManager)
    public Vector2Int CurrentCell { get; set; }

    // Saved at the moment drag begins
    public Vector2Int StartCell { get; private set; }
    public Vector2 StartLocalPosition { get; private set; }

    private CanvasGroup canvasGroup;
    private Coroutine activeAnim;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (DragDropManager.Instance == null)
        {
            Debug.LogError("[DraggableItem] DragDropManager not found in scene.", this);
            return;
        }
        // Record initial position and register in grid
        StartLocalPosition = RectTransform.localPosition;
        DragDropManager.Instance.RegisterOnGrid(this);
        StartCell = CurrentCell;
    }

    // ── Drag interface ───────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        StartLocalPosition = RectTransform.localPosition;
        StartCell = CurrentCell;

        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;

        DragDropManager.Instance.StartDrag(this, eventData);
        onDragStart.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        DragDropManager.Instance.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        DragDropManager.Instance.EndDrag(this, eventData);
        onDragEnd.Invoke();
    }

    // ── Called by DragDropManager ────────────────────────────────────────────

    public void MoveTo(Vector2 targetLocalPos)
    {
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = StartCoroutine(AnimateTo(targetLocalPos, null));
    }

    public void ReturnToStart()
    {
        onReturnStart.Invoke();
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = StartCoroutine(AnimateTo(StartLocalPosition, () => onReturnComplete.Invoke()));
    }

    // ── Animation ────────────────────────────────────────────────────────────

    private IEnumerator AnimateTo(Vector2 target, System.Action onComplete)
    {
        Vector2 from = RectTransform.localPosition;
        float t = 0f;

        while (t < 1f)
        {
            t = Mathf.Min(t + Time.deltaTime / animDuration, 1f);
            RectTransform.localPosition = Vector2.Lerp(from, target, animCurve.Evaluate(t));
            yield return null;
        }

        RectTransform.localPosition = target;
        onComplete?.Invoke();
    }
}

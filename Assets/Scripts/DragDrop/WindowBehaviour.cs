using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to the root of a window GameObject alongside a Canvas component.
/// Handles bring-to-front sorting and exposes open/close events.
/// Requires: Canvas + GraphicRaycaster on the same GameObject.
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class WindowBehaviour : MonoBehaviour, IPointerDownHandler
{
    [Header("Events")]
    public UnityEvent onFocus;
    public UnityEvent onOpen;
    public UnityEvent onClose;

    // Shared counter across all windows — incremented on every focus change
    private static int topOrder = 10;

    private Canvas windowCanvas;

    private void Awake()
    {
        windowCanvas = GetComponent<Canvas>();
        windowCanvas.overrideSorting = true;
    }

    private void OnEnable()
    {
        BringToFront();
        onOpen.Invoke();
    }

    private void OnDisable()
    {
        onClose.Invoke();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void BringToFront()
    {
        windowCanvas.sortingOrder = ++topOrder;
        onFocus.Invoke();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ── Click anywhere on window → bring to front ────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        BringToFront();
    }
}

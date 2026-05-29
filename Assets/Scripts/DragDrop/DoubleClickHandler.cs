using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DoubleClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Maximum time in seconds between two clicks to count as double click.")]
    [SerializeField] private float doubleClickThreshold = 0.3f;

    public UnityEvent onDoubleClick;

    private float lastClickTime = -1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.unscaledTime;

        if (now - lastClickTime <= doubleClickThreshold)
        {
            lastClickTime = -1f; // reset so a third click doesn't re-trigger
            onDoubleClick.Invoke();
        }
        else
        {
            lastClickTime = now;
        }
    }
}

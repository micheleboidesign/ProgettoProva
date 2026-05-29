using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DraggableItemEvent : UnityEvent<DraggableItem> { }

public class DropHandler : MonoBehaviour
{
    [Tooltip("True for the canvas background; false for specific drop targets.")]
    [SerializeField] private bool isBackground;
    public bool IsBackground => isBackground;

    [Tooltip("If true, the dropped item is disabled after the OnDrop event fires.")]
    [SerializeField] private bool disableOnDrop;

    [SerializeField] private DraggableItemEvent onDrop = new();
    public DraggableItemEvent OnDrop => onDrop;

    public void HandleDrop(DraggableItem item)
    {
        onDrop.Invoke(item);
        if (disableOnDrop)
            item.gameObject.SetActive(false);
    }
}

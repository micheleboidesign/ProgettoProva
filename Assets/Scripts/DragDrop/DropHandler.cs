using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DraggableItemEvent : UnityEvent<DraggableItem> { }

public class DropHandler : MonoBehaviour
{
    [Tooltip("True for the canvas background; false for specific drop targets.")]
    [SerializeField] private bool isBackground;
    public bool IsBackground => isBackground;

    [SerializeField] private DraggableItemEvent onDrop = new();
    public DraggableItemEvent OnDrop => onDrop;
}

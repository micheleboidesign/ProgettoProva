using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Folder : MonoBehaviour
{
    [Header("Initial Contents")]
    [Tooltip("Prefabs instantiated into the folder at startup.")]
    [SerializeField] private List<GameObject> initialPrefabs = new();

    [Header("References")]
    [Tooltip("Transform with a GridLayoutGroup — items are parented here.")]
    [SerializeField] private Transform contentContainer;
    [Tooltip("The window GameObject to show/hide (disabled at start).")]
    [SerializeField] private GameObject windowObject;

    [Header("Events")]
    public UnityEvent onOpen;
    public UnityEvent onClose;
    public UnityEvent<DraggableItem> onItemAdded;

    private readonly List<GameObject> items = new();
    public IReadOnlyList<GameObject> Items => items;

    private void Start()
    {
        foreach (var prefab in initialPrefabs)
            SpawnPrefab(prefab);
    }

    // ── Called from DropHandler's OnDrop event ───────────────────────────────

    public void AddItem(DraggableItem item)
    {
        // Re-parent into the content container (GridLayout takes over positioning).
        // worldPositionStays = false lets the layout manage the transform.
        item.transform.SetParent(contentContainer, false);
        items.Add(item.gameObject);
        onItemAdded.Invoke(item);
    }

    // ── Window control ───────────────────────────────────────────────────────

    public void OpenWindow()
    {
        windowObject.SetActive(true);
        onOpen.Invoke();
    }

    public void CloseWindow()
    {
        windowObject.SetActive(false);
        onClose.Invoke();
    }

    public void ToggleWindow()
    {
        if (windowObject.activeSelf)
            CloseWindow();
        else
            OpenWindow();
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void SpawnPrefab(GameObject prefab)
    {
        var go = Instantiate(prefab, contentContainer);
        items.Add(go);
    }
}

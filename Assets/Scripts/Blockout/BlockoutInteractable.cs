using UnityEngine;
using UnityEngine.Events;

public sealed class BlockoutInteractable : MonoBehaviour
{
    [Header("Blockout interaction")]
    public string displayName = "Inspect";
    [TextArea(2, 5)] public string observation;
    public Transform approachPoint;

    [Header("Puzzle hook")]
    [SerializeField] private UnityEvent onInteracted = new UnityEvent();

    public void Interact()
    {
        onInteracted?.Invoke();
    }
}

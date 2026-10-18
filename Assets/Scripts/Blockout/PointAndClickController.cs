using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Prototype point-and-click movement and inspection for the factory blockout.
/// Left-click a walkable surface to move; click an interactable to approach it
/// and inspect it. Right-click cancels the current command.
/// </summary>
public sealed class PointAndClickController : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LayerMask clickMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(10f)] private float maxRayDistance = 250f;
    [SerializeField, Min(0.1f)] private float interactionRange = 1.35f;
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 2.5f;
    [SerializeField] private bool showPrototypeFeedback = true;

    private BlockoutInteractable pendingInteraction;
    private bool pendingMove;
    private bool manualMotion;
    private Vector3 manualDestination;
    private Vector3 moveDestination;
    private string currentFeedback = string.Empty;
    private float feedbackUntil;
    private GUIStyle feedbackStyle;

    private void Awake()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        rightClick |= Input.GetMouseButtonDown(1);
        leftClick |= Input.GetMouseButtonDown(0);
#endif

        if (rightClick)
        {
            CancelCommand();
        }
        else if (leftClick)
        {
            HandleClick();
        }

        if (manualMotion && (pendingMove || pendingInteraction != null))
            AdvanceManualMotion();

        if (pendingInteraction != null)
            CompleteInteractionIfClose();
        else if (pendingMove)
            CompleteMoveIfClose();
    }

    private void HandleClick()
    {
        if (sceneCamera == null || agent == null)
        {
            ShowFeedback("Kamera veya NavMesh Agent bağlı değil.", 4f);
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

#if ENABLE_LEGACY_INPUT_MANAGER
        Vector2 pointerPosition = Input.mousePosition;
#else
        Vector2 pointerPosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
#endif
        Ray ray = sceneCamera.ScreenPointToRay(pointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxRayDistance,
            clickMask,
            QueryTriggerInteraction.Collide);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null) continue;

            BlockoutInteractable interactable = hitCollider.GetComponentInParent<BlockoutInteractable>();
            if (interactable != null)
            {
                MoveToInteractable(interactable);
                return;
            }

            if (hitCollider.GetComponentInParent<BlockoutWalkable>() != null)
            {
                MoveToPoint(hits[i].point);
                return;
            }

            // Ignore unrelated trigger volumes, but don't click through solid props or walls.
            if (hitCollider.isTrigger) continue;

            // If a floor collider lost its BlockoutWalkable marker, resolve the hit
            // back onto the room's ground plane instead of silently rejecting it.
            if (TryResolveGroundPoint(hits[i].point, out Vector3 fallbackPoint))
            {
                MoveToPoint(fallbackPoint);
                return;
            }

            ShowFeedback("Orası yürünebilir bir alan değil.", 2.5f);
            return;
        }

        // Illustrated floor art may not have a collider. Project that click onto
        // the physical ground plane so the painted floor remains clickable.
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance) &&
            TryResolveGroundPoint(ray.GetPoint(distance), out Vector3 groundPoint))
        {
            MoveToPoint(groundPoint);
            return;
        }

        ShowFeedback("Yürümek için zemine, incelemek için bir nesneye tıkla.", 2.5f);
    }

    private bool TryResolveGroundPoint(Vector3 candidate, out Vector3 point)
    {
        point = candidate;
        if (candidate.x < -9.4f || candidate.x > 9.4f ||
            candidate.z < -8.6f || candidate.z > 8.6f)
            return false;

        Vector3 groundCandidate = new Vector3(candidate.x, 0.02f, candidate.z);
        if (agent != null && NavMesh.SamplePosition(
                groundCandidate, out NavMeshHit navHit, navMeshSampleDistance, agent.areaMask))
        {
            point = navHit.position;
            return true;
        }

        point = groundCandidate;
        return true;
    }

    private void MoveToPoint(Vector3 point)
    {
        pendingInteraction = null;
        pendingMove = false;

        if (!TrySetDestination(point, out Vector3 destination)) return;

        moveDestination = destination;
        pendingMove = true;
        ShowFeedback("Hedefe yürüyor…", 60f);
    }

    private void MoveToInteractable(BlockoutInteractable interactable)
    {
        if (interactable == null) return;

        Vector3 requestedPoint = interactable.approachPoint != null
            ? interactable.approachPoint.position
            : interactable.transform.position;

        pendingInteraction = null;
        pendingMove = false;

        if (!TrySetDestination(requestedPoint, out _)) return;

        pendingInteraction = interactable;
        ShowFeedback("Yaklaşıyor: " + interactable.displayName, 60f);
    }

    private bool TrySetDestination(Vector3 requestedPoint, out Vector3 destination)
    {
        destination = requestedPoint;

        if (agent != null && agent.isActiveAndEnabled && !agent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out NavMeshHit startHit,
                navMeshSampleDistance, agent.areaMask))
        {
            agent.Warp(startHit.position);
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh &&
            NavMesh.SamplePosition(requestedPoint, out NavMeshHit navHit,
                navMeshSampleDistance, agent.areaMask))
        {
            agent.isStopped = false;
            if (agent.SetDestination(navHit.position))
            {
                manualMotion = false;
                destination = navHit.position;
                return true;
            }
        }

        // Keep the prototype playable if the scene's NavMesh is missing or stale.
        // The blockout is open, so a direct ground-plane move is a useful fallback.
        BeginManualMotion(requestedPoint);
        destination = manualDestination;
        return true;
    }

    private void BeginManualMotion(Vector3 destination)
    {
        manualMotion = true;
        manualDestination = destination;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void AdvanceManualMotion()
    {
        float speed = agent != null ? agent.speed : 3.2f;
        transform.position = Vector3.MoveTowards(
            transform.position, manualDestination, speed * Time.deltaTime);

        Vector3 direction = manualDestination - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, 540f * Time.deltaTime);
        }
    }

    private void CompleteInteractionIfClose()
    {
        if (pendingInteraction == null) return;

        Vector3 requestedPoint = pendingInteraction.approachPoint != null
            ? pendingInteraction.approachPoint.position
            : pendingInteraction.transform.position;

        if (manualMotion)
        {
            if (Vector3.Distance(transform.position, requestedPoint) > interactionRange)
                return;
        }
        else
        {
            if (agent == null || !agent.isOnNavMesh || agent.pathPending)
                return;

            if (agent.hasPath)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    FailPendingInteraction("Nesneye giden yol geçersiz.");
                    return;
                }

                if (agent.pathStatus == NavMeshPathStatus.PathPartial &&
                    Vector3.Distance(agent.pathEndPosition, requestedPoint) > interactionRange)
                {
                    FailPendingInteraction("Nesneye ulaşmak için yol kapalı.");
                    return;
                }

                float stopRange = Mathf.Max(interactionRange, agent.stoppingDistance);
                if (agent.remainingDistance > stopRange) return;
            }
            else if (Vector3.Distance(transform.position, requestedPoint) > interactionRange)
            {
                FailPendingInteraction("Nesneye ulaşmak için yol bulunamadı.");
                return;
            }
        }

        BlockoutInteractable completedInteraction = pendingInteraction;
        pendingInteraction = null;
        pendingMove = false;
        manualMotion = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        completedInteraction.Interact();
        string observation = string.IsNullOrWhiteSpace(completedInteraction.observation)
            ? "İncelendi."
            : completedInteraction.observation;
        ShowFeedback(completedInteraction.displayName + "\n" + observation, 6f);
    }

    private void CompleteMoveIfClose()
    {
        if (manualMotion)
        {
            if (Vector3.Distance(transform.position, moveDestination) > 0.15f) return;
            pendingMove = false;
            ShowFeedback("Ulaştın.", 1.5f);
            return;
        }

        if (agent == null || !agent.isOnNavMesh || agent.pathPending) return;

        if (agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            agent.isStopped = true;
            agent.ResetPath();
            BeginManualMotion(moveDestination);
            return;
        }

        float stopRange = Mathf.Max(agent.stoppingDistance, 0.15f);
        float distance = agent.hasPath
            ? agent.remainingDistance
            : Vector3.Distance(transform.position, moveDestination);

        if (distance > stopRange) return;

        pendingMove = false;
        ShowFeedback("Ulaştın.", 1.5f);
    }

    private void FailPendingInteraction(string message)
    {
        pendingInteraction = null;
        pendingMove = false;
        manualMotion = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        ShowFeedback(message, 3f);
    }

    private void CancelCommand()
    {
        pendingInteraction = null;
        pendingMove = false;
        manualMotion = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        ShowFeedback("Hareket iptal edildi.", 1.5f);
    }

    private void ShowFeedback(string message, float duration)
    {
        currentFeedback = message;
        feedbackUntil = Time.unscaledTime + duration;
    }

    private void OnGUI()
    {
        if (!showPrototypeFeedback || string.IsNullOrEmpty(currentFeedback) ||
            Time.unscaledTime > feedbackUntil)
            return;

        string message = currentFeedback;
        const float width = 620f;
        const float height = 82f;
        Rect panel = new Rect(20f, Screen.height - height - 20f, width, height);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.055f, 0.07f, 0.07f, 0.92f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = new Color(0.94f, 0.91f, 0.82f, 1f);

        if (feedbackStyle == null)
        {
            feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.94f, 0.91f, 0.82f, 1f) }
            };
        }
        GUI.Label(new Rect(panel.x + 16f, panel.y + 8f, panel.width - 32f, panel.height - 16f), message, feedbackStyle);
        GUI.color = previousColor;
    }
}

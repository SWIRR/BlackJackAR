using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BlackjackTableTracker : MonoBehaviour
{
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    [Header("Table Prefab")]
    public GameObject blackjackTablePrefab;

    [Header("Plane Visuals")]
    public GameObject wrongPlanePrefab;
    public GameObject correctPlanePrefab;

    [Header("Plane Detection")]
    [Tooltip("How far below the camera a plane must be to not be considered ceiling")]
    public float cameraMargin = 0.3f;

    public static event Action<GameObject> OnTableSpawned;

    private ARPlane currentBestPlane;
    private GameObject spawnedTable;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void OnEnable()
    {
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (spawnedTable != null) return;
        if (currentBestPlane == null) return;

        Vector2 screenPos = Vector2.zero;
        bool didTap = false;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            didTap = true;
        }
#else
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0 && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            screenPos = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition;
            didTap = true;
        }
#endif


        if (!didTap) return;

        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinBounds)) return;

        foreach (ARRaycastHit hit in hits)
        {
            if (hit.trackableId != currentBestPlane.trackableId) continue;

            Vector3 spawnPos = currentBestPlane.transform.position;
            Quaternion spawnRot = currentBestPlane.transform.rotation;

            spawnedTable = Instantiate(blackjackTablePrefab, spawnPos, spawnRot);

            foreach (ARPlane plane in planeManager.trackables)
                plane.gameObject.SetActive(false);

            planeManager.enabled = false;

            OnTableSpawned?.Invoke(spawnedTable);
            Debug.Log($"[Blackjack] Table spawned at: {spawnPos}");
            break;
        }
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {
        if (spawnedTable != null) return; // ignore plane updates after table is placed
        RefreshPlaneVisuals();
    }

    private void RefreshPlaneVisuals()
    {
        ARPlane best = FindBestPlane();

        foreach (ARPlane plane in planeManager.trackables)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
            ApplyVisual(plane, plane == best);
        }

        if (best != null && best != currentBestPlane)
        {
            currentBestPlane = best;
            Debug.Log($"[Blackjack] Best plane updated @ Y: {best.transform.position.y}");
        }
    }

    private void ApplyVisual(ARPlane plane, bool isCorrect)
    {
        MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
        GameObject prefabToUse = isCorrect ? correctPlanePrefab : wrongPlanePrefab;

        if (prefabToUse != null && meshRenderer != null)
        {
            MeshRenderer prefabRenderer = prefabToUse.GetComponent<MeshRenderer>();
            if (prefabRenderer != null)
                meshRenderer.material = prefabRenderer.sharedMaterial;
        }
    }

    private ARPlane FindBestPlane()
    {
        float cameraY = Camera.main.transform.position.y;
        float maxAllowedY = cameraY - cameraMargin;
        float minAllowedY = cameraY - 2.0f;

        ARPlane lowest = null;
        float lowestY = float.MaxValue;

        List<ARPlane> candidates = new List<ARPlane>();
        foreach (ARPlane plane in planeManager.trackables)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;

            float planeY = plane.transform.position.y;
            if (planeY > maxAllowedY) continue;
            if (planeY < minAllowedY) continue;

            candidates.Add(plane);
            if (planeY < lowestY) { lowestY = planeY; lowest = plane; }
        }

        if (candidates.Count == 0) return null;

        // only one plane — don't guess, return nothing
        // user needs to scan more so we can distinguish floor from table
        if (candidates.Count == 1) return null;

        ARPlane best = null;
        float highestY = float.MinValue;

        foreach (ARPlane plane in candidates)
        {
            float planeY = plane.transform.position.y;

            // must be at least 25cm above the lowest plane to not be floor
            if (planeY - lowestY < 0.25f) continue;

            if (planeY > highestY)
            {
                highestY = planeY;
                best = plane;
            }
        }

        return best; // null if no plane clears the height gap — all shown as wrong
    }
}
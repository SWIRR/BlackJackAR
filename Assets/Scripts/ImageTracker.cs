using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BlackjackTableTracker : MonoBehaviour
{
    [Header("AR Components")]
    public ARTrackedImageManager trackedImageManager;
    public ARPlaneManager planeManager;

    [Header("Blackjack Table")]
    public GameObject blackjackTablePrefab;
    public string targetImageName = "blackjack_marker";

    public static event Action<GameObject> OnTableSpawned;

    private GameObject spawnedTable;
    private ARPlane anchorPlane;

    void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnImagesChanged);
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnImagesChanged);
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var img in eventArgs.added) HandleTrackedImage(img);
        foreach (var img in eventArgs.updated) HandleTrackedImage(img);
    }

    void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        if (trackedImage.referenceImage == null) return;

        string imageName = trackedImage.referenceImage.name;
        if (string.IsNullOrEmpty(imageName) || string.IsNullOrEmpty(targetImageName)) return;
        if (!imageName.Trim().Equals(targetImageName.Trim(), StringComparison.OrdinalIgnoreCase)) return;

        // Stół już stoi — NIGDY go nie ruszamy
        if (spawnedTable != null) return;

        // Czekamy na pewny tracking
        if (trackedImage.trackingState != TrackingState.Tracking) return;

        ARPlane planeUnder = FindPlaneUnder(trackedImage.transform.position);
        if (planeUnder != null) anchorPlane = planeUnder;

        if (blackjackTablePrefab == null) return;

        Vector3 spawnPos = trackedImage.transform.position;
        Quaternion spawnRot = BuildTableRotation(trackedImage.transform, anchorPlane);

        // KLUCZOWE: brak SetParent — stół jest wolnym obiektem, nic go nie rusza
        spawnedTable = Instantiate(blackjackTablePrefab, spawnPos, spawnRot);
        spawnedTable.transform.localScale = Vector3.one * 0.4f;

        Debug.Log($"[Blackjack] Stół zespawnowany @ {spawnPos}");
        OnTableSpawned?.Invoke(spawnedTable);
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {
        // Plane jest używany TYLKO do rotacji przy spawnie. Reszta nas nie obchodzi.
    }

    private Quaternion BuildTableRotation(Transform imageTransform, ARPlane plane)
    {
        Vector3 surfaceUp = (plane != null) ? plane.transform.up : Vector3.up;
        Vector3 tableForward = Vector3.ProjectOnPlane(imageTransform.forward, surfaceUp).normalized;

        if (tableForward.sqrMagnitude < 0.001f)
            tableForward = Vector3.ProjectOnPlane(imageTransform.right, surfaceUp).normalized;

        return Quaternion.LookRotation(tableForward, surfaceUp);
    }

    private ARPlane FindPlaneUnder(Vector3 worldPos)
    {
        ARPlane closest = null;
        float bestDist = float.MaxValue;

        foreach (var plane in planeManager.trackables)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp &&
                plane.alignment != PlaneAlignment.HorizontalDown)
                continue;

            float dist = Vector3.Distance(plane.transform.position, worldPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = plane;
            }
        }

        return closest;
    }
}
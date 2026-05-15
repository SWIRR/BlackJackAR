using System.Collections.Generic;
using UnityEngine;

public class BetChipSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ChipPrefabEntry
    {
        public int value;
        public GameObject prefab;
    }

    [Header("Chip prefabs")]
    [SerializeField] private List<ChipPrefabEntry> chipPrefabs = new List<ChipPrefabEntry>();

    [Header("Spawn settings")]
    [SerializeField] private Transform dropPoint;
    [SerializeField] private Transform chipsRoot;

    [Tooltip("Wysokoœæ, z której spada ¿eton nad punktem DropPoint.")]
    [SerializeField] private float spawnHeight = 1.0f;

    [Tooltip("Losowe rozrzucenie ¿etonów na boki.")]
    [SerializeField] private float randomRadius = 0.05f;

    [Tooltip("Losowa rotacja ¿etonu przy spawnie.")]
    [SerializeField] private float rotationRandomness = 180f;

    [Tooltip("Delikatny impuls na boki po spawnie.")]
    [SerializeField] private float horizontalImpulse = 0.01f;

    [Tooltip("Delikatny obrót ¿etonu podczas spadania.")]
    [SerializeField] private float torqueImpulse = 0.05f;

    [Header("Physics settings")]
    [SerializeField] private float chipMass = 0.05f;
    [SerializeField] private float linearDamping = 0.1f;
    [SerializeField] private float angularDamping = 0.2f;

    private readonly List<GameObject> spawnedChips = new List<GameObject>();

    public void SpawnChip(int value)
    {
        GameObject chipPrefab = GetChipPrefab(value);

        if (chipPrefab == null)
        {
            Debug.LogWarning($"[BetChipSpawner] Brak prefabu ¿etonu dla wartoœci: {value}");
            return;
        }

        if (dropPoint == null)
        {
            Debug.LogWarning("[BetChipSpawner] Brak przypisanego Drop Point.");
            return;
        }

        Vector2 randomCircle = Random.insideUnitCircle * randomRadius;

        Vector3 spawnPosition =
            dropPoint.position
            + transform.up * spawnHeight
            + transform.right * randomCircle.x
            + transform.forward * randomCircle.y;

        Quaternion spawnRotation = Quaternion.Euler(
            Random.Range(-10f, 10f),
            Random.Range(0f, rotationRandomness),
            Random.Range(-10f, 10f)
        );

        Transform parent = chipsRoot != null ? chipsRoot : transform;

        GameObject spawnedChip = Instantiate(chipPrefab, spawnPosition, spawnRotation, parent);
        spawnedChips.Add(spawnedChip);

        Rigidbody rb = spawnedChip.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = spawnedChip.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = chipMass;

        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 sideImpulse =
            transform.right * Random.Range(-horizontalImpulse, horizontalImpulse) +
            transform.forward * Random.Range(-horizontalImpulse, horizontalImpulse);

        rb.AddForce(sideImpulse, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueImpulse, ForceMode.Impulse);
    }

    public void ClearChips()
    {
        for (int i = spawnedChips.Count - 1; i >= 0; i--)
        {
            if (spawnedChips[i] != null)
            {
                Destroy(spawnedChips[i]);
            }
        }

        spawnedChips.Clear();
    }

    private GameObject GetChipPrefab(int value)
    {
        foreach (ChipPrefabEntry entry in chipPrefabs)
        {
            if (entry.value == value)
            {
                return entry.prefab;
            }
        }

        return null;
    }
}
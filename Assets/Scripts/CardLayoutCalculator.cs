using UnityEngine;

/// <summary>
/// Oblicza docelowe pozycje i rotacje kart na stole oraz ustawia rooty rąk
/// w zależności od tego, z której strony stoi gracz (AR-aware).
///
/// WAŻNE: playerHandRoot i dealerHandRoot muszą być dziećmi CardTable.
/// Skrypt operuje wyłącznie na localPosition/localRotation rootów,
/// dzięki czemu pochylenie stołu jest automatycznie uwzględnione.
/// </summary>
public class CardLayoutCalculator : MonoBehaviour
{
    [Header("Rooty rąk (dzieci CardTable)")]
    public Transform playerHandRoot;
    public Transform dealerHandRoot;

    [Header("Dealer (dziecko CardTable)")]
    [Tooltip("Transform postaci krupiera — musi być dzieckiem CardTable.")]
    public Transform dealerTransform;
    [Tooltip("Odległość krupiera od środka stołu (lokalna przestrzeń stołu).")]
    public float dealerStandDistance = 1.5f;
    [Tooltip("Lokalna wysokość krupiera nad stołem (Y w przestrzeni stołu).")]
    public float dealerHeightOffset = -1.5f;

    [Header("Geometria stołu")]
    [Tooltip("Promień stołu od środka do narożnika.")]
    public float tableRadius = 0.45f;
    [Tooltip("Wcięcie od krawędzi ku środkowi — karty na suknie, nie na drewnie.")]
    public float handInset = 0.08f;

    [Header("Ustawienia kart")]
    public float cardSpacing = 0.08f;
    public float cardDepthOffset = 0.001f;

    public void UpdateHandPositions()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[CardLayout] Brak Camera.main.");
            return;
        }

        // Kierunek do kamery w lokalnej przestrzeni stołu, tylko XZ (ignorujemy Y stołu)
        Vector3 toCameraLocal = transform.InverseTransformDirection(
            cam.transform.position - transform.position
        );
        toCameraLocal.y = 0f;
        if (toCameraLocal.sqrMagnitude < 0.0001f) return;
        toCameraLocal.Normalize();

        float angle = Mathf.Atan2(toCameraLocal.x, toCameraLocal.z) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;

        Vector3 playerLocalDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 dealerLocalDir = -playerLocalDir;

        float edgeDist = tableRadius * Mathf.Cos(Mathf.PI / 8f) - handInset;

        SetRootLocalPosition(playerHandRoot, playerLocalDir, edgeDist);
        SetRootLocalPosition(dealerHandRoot, dealerLocalDir, edgeDist);
        SetDealerLocalTransform(dealerLocalDir);

        Debug.Log($"[CardLayout] Gracz od strony {snapped:F0}°");
    }

    private void SetDealerLocalTransform(Vector3 dealerLocalDir)
    {
        if (dealerTransform == null) return;

        dealerTransform.localPosition = new Vector3(
            dealerLocalDir.x * dealerStandDistance,
            dealerHeightOffset,
            dealerLocalDir.z * dealerStandDistance
        );

        // Rotacja: krupier patrzy w kierunku gracza (-dealerLocalDir = w stronę środka stołu)
        // Upewnij się, że -dealerLocalDir nie jest zerowym wektorem przed LookRotation
        Vector3 facingDir = -dealerLocalDir;
        if (facingDir.sqrMagnitude > 0.0001f)
            dealerTransform.localRotation = Quaternion.LookRotation(facingDir, Vector3.up);
    }

    private void SetRootLocalPosition(Transform root, Vector3 localDir, float edgeDist)
    {
        // Zachowaj lokalny Y ustawiony w Inspektorze — to wysokość nad powierzchnią stołu
        float localY = root.localPosition.y;

        // Ustaw XZ w lokalnej przestrzeni stołu, Y zostaje
        root.localPosition = new Vector3(
            localDir.x * edgeDist,
            localY,
            localDir.z * edgeDist
        );

        // Rotacja: root patrzy wzdłuż lokalnego kierunku, oś "góra" = lokalna Y stołu (0,1,0 lokalnie)
        // transform.InverseTransformDirection nie jest potrzebne — lokalnie Y stołu to zawsze Vector3.up
        root.localRotation = Quaternion.LookRotation(localDir, Vector3.up);
    }

    public Vector3 ComputeCardWorldPos(bool isPlayer, int index, int totalInHand)
    {
        Transform root = isPlayer ? playerHandRoot : dealerHandRoot;

        float offset = (index - (totalInHand - 1) / 2f) * cardSpacing;
        if (isPlayer) offset = -offset;

        // transform.up = normalna powierzchni stołu w przestrzeni świata
        // — separacja kart prostopadle do stołu, nie pionowo
        return root.position
             + root.right * offset
             + transform.up * (index * cardDepthOffset);
    }

    public Quaternion ComputeCardFlatRot(bool isPlayer)
    {
        Transform root = isPlayer ? playerHandRoot : dealerHandRoot;

        // root.rotation zawiera już pełną rotację stołu (bo root jest dzieckiem stołu).
        // Euler(-90, 0, 0) kładzie kartę płasko na powierzchni stołu.
        // Dla gracza dodatkowy obrót 180° wokół Z (nie Y!) żeby karta była czytelna od jego strony.
        float zFlip = isPlayer ? 180f : 0f;
        return root.rotation * Quaternion.Euler(-90f, 0f, zFlip);
    }
}
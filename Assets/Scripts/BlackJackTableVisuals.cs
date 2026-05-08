using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CardLayoutCalculator))]
[RequireComponent(typeof(CardAnimator))]
public class BlackjackTableVisuals : MonoBehaviour
{
    [Header("Punkty kontrolne")]
    public Transform deckTransform;

    // ─── zależności ───────────────────────────────────────────────────────────
    private CardLayoutCalculator layout;
    private CardAnimator animator;

    // ─── stan wewnętrzny ──────────────────────────────────────────────────────
    private List<GameObject> playerCardObjects = new List<GameObject>();
    private List<GameObject> dealerCardObjects = new List<GameObject>();
    private GameObject hiddenDealerCard;

    void Awake()
    {
        layout = GetComponent<CardLayoutCalculator>();
        animator = GetComponent<CardAnimator>();
    }

    public void ClearTable()
    {
        animator.StopAll();

        foreach (var c in playerCardObjects) if (c) Destroy(c);
        foreach (var c in dealerCardObjects) if (c) Destroy(c);
        playerCardObjects.Clear();
        dealerCardObjects.Clear();

        if (hiddenDealerCard != null) { Destroy(hiddenDealerCard); hiddenDealerCard = null; }
    }

    public void AddCard(string cardData, bool isPlayer)
    {
        if (playerCardObjects.Count == 0 && dealerCardObjects.Count == 0)
            layout.UpdateHandPositions();

        bool isHiddenDealerCard = !isPlayer && dealerCardObjects.Count == 1;
        if (isHiddenDealerCard)
        {
            dealerCardObjects.Add(null); // placeholder
            return;
        }

        GameObject newCard = SpawnCard(cardData);
        if (newCard == null) return;

        List<GameObject> hand = isPlayer ? playerCardObjects : dealerCardObjects;
        hand.Add(newCard);

        RearrangeHand(isPlayer, excludeLast: true);
        AnimateCardToSlot(newCard, isPlayer, hand.Count - 1, hand.Count);
    }

    public void RevealHiddenDealerCard(string cardData)
    {
        if (dealerCardObjects.Count < 2 || dealerCardObjects[1] != null) return;

        if (hiddenDealerCard != null) { Destroy(hiddenDealerCard); hiddenDealerCard = null; }

        GameObject newCard = SpawnCard(cardData);
        if (newCard == null) return;

        dealerCardObjects[1] = newCard;
        AnimateCardToSlot(newCard, isPlayer: false, index: 1, total: dealerCardObjects.Count);
    }

    private GameObject SpawnCard(string cardData)
    {
        GameObject prefab = FindPrefabInResources(cardData);
        if (prefab == null)
        {
            Debug.LogWarning($"[TableVisuals] Brak prefaba dla karty: {cardData}");
            return null;
        }

        GameObject card = Instantiate(prefab, deckTransform.position, deckTransform.rotation);
        card.transform.SetParent(transform);
        DisablePhysics(card);

        return card;
    }

    // ─── layout i animacje ────────────────────────────────────────────────────

    private void RearrangeHand(bool isPlayer, bool excludeLast = false)
    {
        List<GameObject> hand = isPlayer ? playerCardObjects : dealerCardObjects;
        int count = excludeLast ? hand.Count - 1 : hand.Count;

        for (int i = 0; i < count; i++)
        {
            if (hand[i] == null) continue;
            Vector3 pos = layout.ComputeCardWorldPos(isPlayer, i, hand.Count);
            Quaternion rot = layout.ComputeCardFlatRot(isPlayer);
            animator.AnimateTo(hand[i], pos, rot);
        }
    }

    private void AnimateCardToSlot(GameObject card, bool isPlayer, int index, int total)
    {
        Vector3 pos = layout.ComputeCardWorldPos(isPlayer, index, total);
        Quaternion rot = layout.ComputeCardFlatRot(isPlayer);
        animator.AnimateTo(card, pos, rot);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private GameObject FindPrefabInResources(string cardData)
    {
        string rank = cardData.Length == 3 ? cardData.Substring(0, 2) : cardData.Substring(0, 1);
        string suitIcon = cardData.Substring(cardData.Length - 1);

        string baseSuit = suitIcon switch
        {
            "♠" => "Spade",
            "♥" => "Heart",
            "♦" => "Diamond",
            "♣" => "Club",
            _ => ""
        };

        string rankName = rank switch
        {
            "A" => "Ace",
            "J" => "Jack",
            "Q" => "Queen",
            "K" => "King",
            _ => rank
        };

        return Resources.Load<GameObject>($"Cards/{baseSuit}s/Card_{baseSuit}{rankName}");
    }

    private void DisablePhysics(GameObject obj)
    {
        if (obj.TryGetComponent<Rigidbody>(out var rb)) { rb.useGravity = false; rb.isKinematic = true; }
        if (obj.TryGetComponent<Collider>(out var col)) col.enabled = false;
    }
}
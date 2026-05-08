using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackJackGameManager : MonoBehaviour
{
    // ===================== EVENTY DLA UI =====================

    public event Action<int> OnBalanceChanged;
    public event Action<int> OnBetChanged;
    public event Action OnBetPanelRequested;
    public event Action<bool> OnGameUIUpdateRequested;   // bool = hideSecondDealerCard
    public event Action<string, int> OnGameResolved;     // (resultText, payout)
    public event Action<bool> OnPlayerButtonsChanged;    // bool = active
    public event Action<string, bool> OnCardPhysicalDraw;

    // ===================== PUBLICZNY STAN (do odczytu przez UI) =====================

    public List<string> PlayerHand => playerHand;
    public List<string> DealerHand => dealerHand;
    public int Balance => balance;
    public int CurrentBet => currentBet;
    public bool PlayerTurn => playerTurn;

    // ===================== PRYWATNY STAN =====================

    private List<string> deck = new List<string>();
    private List<string> playerHand = new List<string>();
    private List<string> dealerHand = new List<string>();

    private int balance = 1000;
    private int currentBet = 0;
    private bool playerTurn = false;

    private readonly string[] suits = { "♠", "♥", "♦", "♣" };
    private readonly string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

    void Start()
    {
        RequestBetPanel();
    }

    // ===================== BETTING =====================

    public void RequestBetPanel()
    {
        currentBet = 0;
        OnBetChanged?.Invoke(currentBet);
        OnBetPanelRequested?.Invoke();
    }

    public void PlaceBet(int amount)
    {
        if (currentBet + amount > balance) return;
        currentBet += amount;
        OnBetChanged?.Invoke(currentBet);
    }

    public void ClearBet()
    {
        currentBet = 0;
        OnBetChanged?.Invoke(currentBet);
    }

    // ===================== DECK =====================

    private void BuildDeck()
    {
        deck.Clear();
        foreach (var suit in suits)
            foreach (var rank in ranks)
                deck.Add($"{rank}{suit}");

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    private string DrawCard()
    {
        string card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    public int CardValue(string card)
    {
        string rank = card.Length == 3 ? card.Substring(0, 2) : card.Substring(0, 1);
        if (rank == "A") return 11;
        if (rank == "J" || rank == "Q" || rank == "K" || rank == "10") return 10;
        return int.Parse(rank);
    }

    public int HandValue(List<string> hand)
    {
        int total = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            int val = CardValue(card);
            if (val == 11) aces++;
            total += val;
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    // ===================== GAME FLOW =====================

    public void Deal()
    {
        if (currentBet <= 0) return;

        balance -= currentBet;
        OnBalanceChanged?.Invoke(balance);

        BuildDeck();
        playerHand.Clear();
        dealerHand.Clear();

        string pCard1 = DrawCard();
        playerHand.Add(pCard1);
        OnCardPhysicalDraw?.Invoke(pCard1, true);

        pCard1 = DrawCard();
        dealerHand.Add(pCard1);
        OnCardPhysicalDraw?.Invoke(pCard1, false);

        pCard1 = DrawCard();
        playerHand.Add(pCard1);
        OnCardPhysicalDraw?.Invoke(pCard1, true);

        pCard1 = DrawCard();
        dealerHand.Add(pCard1);
        OnCardPhysicalDraw?.Invoke(pCard1, false);

        playerTurn = true;
        OnGameUIUpdateRequested?.Invoke(true);
        OnPlayerButtonsChanged?.Invoke(true);

        if (HandValue(playerHand) == 21)
            StartCoroutine(DealerTurnCoroutine());
    }

    public void Hit()
    {
        if (!playerTurn) return;

        string card = DrawCard();
        playerHand.Add(card);
        OnCardPhysicalDraw?.Invoke(card, true);
        OnGameUIUpdateRequested?.Invoke(true);
        OnPlayerButtonsChanged?.Invoke(true);

        if (HandValue(playerHand) >= 21)
            StartCoroutine(DealerTurnCoroutine());
    }

    public void Stand()
    {
        if (!playerTurn) return;
        playerTurn = false;
        StartCoroutine(DealerTurnCoroutine());
    }

    public void DoubleDown()
    {
        if (!playerTurn || balance < currentBet) return;

        balance -= currentBet;
        currentBet *= 2;
        OnBalanceChanged?.Invoke(balance);

        string card = DrawCard();
        playerHand.Add(card);
        OnCardPhysicalDraw?.Invoke(card, true); 

        OnGameUIUpdateRequested?.Invoke(true);
        playerTurn = false;
        StartCoroutine(DealerTurnCoroutine());
    }

    private IEnumerator DealerTurnCoroutine()
    {
        playerTurn = false;
        OnPlayerButtonsChanged?.Invoke(false);
        OnGameUIUpdateRequested?.Invoke(false);
        yield return new WaitForSeconds(0.8f);

        while (HandValue(dealerHand) < 17)
        {
            string card = DrawCard();
            dealerHand.Add(card);
            OnCardPhysicalDraw?.Invoke(card, false);
            OnGameUIUpdateRequested?.Invoke(false);
            yield return new WaitForSeconds(0.8f);
        }

        ResolveGame();
    }

    private void ResolveGame()
    {
        int playerScore = HandValue(playerHand);
        int dealerScore = HandValue(dealerHand);

        string result;
        int payout = 0;

        bool playerBust = playerScore > 21;
        bool dealerBust = dealerScore > 21;
        bool playerBlackjack = playerScore == 21 && playerHand.Count == 2;

        if (playerBust)
        {
            result = "PRZEGRANA\nPrzebicie!";
        }
        else if (dealerBust)
        {
            result = "WYGRANA!\nKrupier się przebił!";
            payout = currentBet * 2;
        }
        else if (playerBlackjack && dealerScore != 21)
        {
            result = "BLACKJACK!\n🃏";
            payout = Mathf.RoundToInt(currentBet * 2.5f);
        }
        else if (playerScore > dealerScore)
        {
            result = "WYGRANA!";
            payout = currentBet * 2;
        }
        else if (playerScore < dealerScore)
        {
            result = "PRZEGRANA";
        }
        else
        {
            result = "REMIS";
            payout = currentBet;
        }

        balance += payout;
        OnBalanceChanged?.Invoke(balance);
        OnGameUIUpdateRequested?.Invoke(false);

        if (balance <= 0)
        {
            result += "\n\nBrak środków!\nRestart";
            balance = 1000;
            OnBalanceChanged?.Invoke(balance);
        }

        OnGameResolved?.Invoke(result, payout);
    }
}
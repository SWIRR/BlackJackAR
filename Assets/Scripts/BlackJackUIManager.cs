using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BlackJackUIManager : MonoBehaviour
{
    [Header("Referencja do logiki gry")]
    public BlackJackGameManager gameManager;

    [Header("Panele")]
    public GameObject panelInstructions;
    public GameObject panelGame;
    public GameObject panelResult;
    public GameObject panelBet;

    [Header("Tekst")]
    public TextMeshProUGUI textPlayerCards;
    public TextMeshProUGUI textPlayerScore;
    public TextMeshProUGUI textDealerCards;
    public TextMeshProUGUI textDealerScore;
    public TextMeshProUGUI textResult;
    public TextMeshProUGUI textBalance;
    public TextMeshProUGUI textCurrentBet;

    [Header("Przyciski")]
    public Button btnHit;
    public Button btnStand;
    public Button btnDouble;
    public Button btnDeal;
    public Button btnRestart;
    public Button btnBet10;
    public Button btnBet25;
    public Button btnBet50;
    public Button btnBet100;
    public Button btnClearBet;

    [Header("Obrazki Wyników")]
    public Image imageResult;
    public Sprite spriteWin;
    public Sprite spriteLoss;

    // ===================== INIT =====================

    private GameObject tableObject;
    private BlackjackTableVisuals tableVisuals;
    private DealerAnimationController dealerAnimationController;
    private BetChipSpawner betChipSpawner;

    void Start()
    {
        if (panelInstructions != null) panelInstructions.SetActive(true);
        if (panelBet != null) panelBet.SetActive(false);
        if (panelGame != null) panelGame.SetActive(false);
        if (panelResult != null) panelResult.SetActive(false);

        BlackjackTableTracker.OnTableSpawned += InitializeGame;
    }

    // Zmieniono sygnaturę tak, by pasowała do Action<GameObject>
    void InitializeGame(GameObject spawnedTable)
    {
        BlackjackTableTracker.OnTableSpawned -= InitializeGame;

        tableObject = spawnedTable;
        tableVisuals = spawnedTable.GetComponent<BlackjackTableVisuals>();
        dealerAnimationController = spawnedTable.GetComponentInChildren<DealerAnimationController>();
        betChipSpawner = spawnedTable.GetComponentInChildren<BetChipSpawner>();

        if (panelInstructions != null) panelInstructions.SetActive(false);

        panelBet.SetActive(true);
        panelGame.SetActive(false);
        panelResult.SetActive(false);

        // Podpięcie przycisków
        btnHit.onClick.AddListener(gameManager.Hit);
        btnStand.onClick.AddListener(gameManager.Stand);
        btnDouble.onClick.AddListener(gameManager.DoubleDown);
        btnDeal.onClick.AddListener(() =>
        {
            gameManager.Deal();
            dealerAnimationController?.PlayWave();
        });
        btnRestart.onClick.AddListener(gameManager.RequestBetPanel);
        btnBet10.onClick.AddListener(() => PlaceBetWithChip(10));
        btnBet25.onClick.AddListener(() => PlaceBetWithChip(25));
        btnBet50.onClick.AddListener(() => PlaceBetWithChip(50));
        btnBet100.onClick.AddListener(() => PlaceBetWithChip(100));
        btnClearBet.onClick.AddListener(() =>
        {
            gameManager.ClearBet();
            betChipSpawner?.ClearChips();
        });

        // Subskrypcja eventów z GameManagera
        gameManager.OnBalanceChanged += UpdateBalanceUI;
        gameManager.OnBetChanged += UpdateBetUI;
        gameManager.OnBetPanelRequested += ShowBetPanel;
        gameManager.OnGameUIUpdateRequested += UpdateGameUI;
        gameManager.OnGameResolved += ShowResult;
        gameManager.OnPlayerButtonsChanged += SetPlayerButtons;
        gameManager.OnCardPhysicalDraw += (card, isPlayer) => tableVisuals.AddCard(card, isPlayer);
        gameManager.OnBetPanelRequested += tableVisuals.ClearTable;

        // Stan początkowy UI
        UpdateBalanceUI(gameManager.Balance);
    }

    private void PlaceBetWithChip(int value)
    {
        int betBefore = gameManager.CurrentBet;

        gameManager.PlaceBet(value);

        int betAfter = gameManager.CurrentBet;

        if (betAfter > betBefore)
        {
            betChipSpawner?.SpawnChip(value);
        }
    }

    void OnDestroy()
    {
        // Odpięcie eventów dla bezpieczeństwa
        BlackjackTableTracker.OnTableSpawned -= InitializeGame;

        if (gameManager != null)
        {
            gameManager.OnBalanceChanged -= UpdateBalanceUI;
            gameManager.OnBetChanged -= UpdateBetUI;
            gameManager.OnBetPanelRequested -= ShowBetPanel;
            gameManager.OnGameUIUpdateRequested -= UpdateGameUI;
            gameManager.OnGameResolved -= ShowResult;
            gameManager.OnPlayerButtonsChanged -= SetPlayerButtons;
        }
    }

    // ===================== PANELE =====================

    void ShowBetPanel()
    {
        panelBet.SetActive(true);
        panelGame.SetActive(false);
        panelResult.SetActive(false);
        UpdateBetUI(gameManager.CurrentBet);
        betChipSpawner?.ClearChips();
    }

    void ShowResult(string resultText, int payout)
    {
        panelResult.SetActive(true);
        textResult.text = payout > 0 ? $"\n+{payout}$" : "";

        if (IsPlayerWin(resultText))
        {
            dealerAnimationController?.PlayThumbsUp();
            if (imageResult != null && spriteWin != null)
                imageResult.sprite = spriteWin;
        }
        else if (IsPlayerLoss(resultText))
        {
            dealerAnimationController?.PlayHeadGrab();
            if (imageResult != null && spriteLoss != null)
                imageResult.sprite = spriteLoss;
        }
    }

    // ===================== AKTUALIZACJE UI =====================

    bool IsPlayerWin(string resultText)
    {
        return resultText.Contains("WYGRANA") || resultText.Contains("BLACKJACK");
    }

    bool IsPlayerLoss(string resultText)
    {
        return resultText.Contains("PRZEGRANA");
    }

    void UpdateBalanceUI(int balance)
    {
        textBalance.text = $"Kasa: {balance}$";
    }

    void UpdateBetUI(int currentBet)
    {
        textCurrentBet.text = $"Zakład: {currentBet}$";
        btnDeal.interactable = currentBet > 0;
    }

    void UpdateGameUI(bool hideSecondDealerCard)
    {
        List<string> playerHand = gameManager.PlayerHand;
        List<string> dealerHand = gameManager.DealerHand;

        textPlayerCards.text = string.Join(" ", playerHand);
        textPlayerScore.text = $"Wynik: {gameManager.HandValue(playerHand)}";

        if (hideSecondDealerCard && dealerHand.Count >= 2)
        {
            textDealerCards.text = dealerHand[0] + " ?";
            textDealerScore.text = $"Wynik: {gameManager.CardValue(dealerHand[0])}";
        }
        else
        {
            textDealerCards.text = string.Join(" ", dealerHand);
            textDealerScore.text = $"Wynik: {gameManager.HandValue(dealerHand)}";

            if (dealerHand.Count >= 2)
                tableVisuals?.RevealHiddenDealerCard(dealerHand[1]);
        }

        panelGame.SetActive(true);
        panelBet.SetActive(false);

        // Double aktywny tylko na start tury (2 karty) i jeśli stać gracza
        SetPlayerButtons(gameManager.PlayerTurn);
    }

    void SetPlayerButtons(bool active)
    {
        btnHit.interactable = active;
        btnStand.interactable = active;
        btnDouble.interactable = active
            && gameManager.Balance >= gameManager.CurrentBet
            && gameManager.PlayerHand.Count == 2;
    }
}
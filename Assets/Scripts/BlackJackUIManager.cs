using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BlackJackUIManager : MonoBehaviour
{
    [Header("Referencja do logiki gry")]
    public BlackJackGameManager gameManager;

    [Header("Panele")]
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

    // ===================== INIT =====================

    private GameObject tableObject;
    private BlackjackTableVisuals tableVisuals;

    void Start()
    {
        BlackjackTableTracker.OnTableSpawned += InitializeGame;
    }

    // Zmieniono sygnaturę tak, by pasowała do Action<GameObject>
    void InitializeGame(GameObject spawnedTable)
    {
        BlackjackTableTracker.OnTableSpawned -= InitializeGame;

        tableObject = spawnedTable;
        tableVisuals = spawnedTable.GetComponent<BlackjackTableVisuals>();

        panelBet.SetActive(true);
        panelGame.SetActive(false);
        panelResult.SetActive(false);

        // Podpięcie przycisków
        btnHit.onClick.AddListener(gameManager.Hit);
        btnStand.onClick.AddListener(gameManager.Stand);
        btnDouble.onClick.AddListener(gameManager.DoubleDown);
        btnDeal.onClick.AddListener(gameManager.Deal);
        btnRestart.onClick.AddListener(gameManager.RequestBetPanel);
        btnBet10.onClick.AddListener(() => gameManager.PlaceBet(10));
        btnBet25.onClick.AddListener(() => gameManager.PlaceBet(25));
        btnBet50.onClick.AddListener(() => gameManager.PlaceBet(50));
        btnBet100.onClick.AddListener(() => gameManager.PlaceBet(100));
        btnClearBet.onClick.AddListener(gameManager.ClearBet);

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
    }

    void ShowResult(string resultText, int payout)
    {
        panelResult.SetActive(true);
        textResult.text = resultText + (payout > 0 ? $"\n+{payout}$" : "");
    }

    // ===================== AKTUALIZACJE UI =====================

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
            textDealerCards.text = dealerHand[0] + " 🂠";
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
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Core
{
    public class PurchaseDialog : MonoBehaviour
    {
        public static PurchaseDialog Instance { get; private set; }

        [Header("Dialog Panel")]
        public GameObject dialogPanel;
        
        [Header("UI Elements")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        
        [Header("Buttons")]
        public Button coinButton;
        public Button adButton;
        public Button closeButton;

        [Header("Button Texts")]
        public TextMeshProUGUI coinButtonText;
        public TextMeshProUGUI adButtonText;

        private Action onCoinPurchase;
        private Action onAdPurchase;
        private int currentCoinCost;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Hide dialog initially
            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            // Setup button listeners
            if (coinButton != null)
                coinButton.onClick.AddListener(OnCoinButtonClicked);
            
            if (adButton != null)
                adButton.onClick.AddListener(OnAdButtonClicked);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseDialog);
        }

        /// <summary>
        /// Shows the purchase dialog with both coin and ad options
        /// </summary>
        /// <param name="title">Dialog title (e.g., "Get Hints")</param>
        /// <param name="description">Description text (e.g., "Choose how to get 3 hints")</param>
        /// <param name="coinCost">Cost in coins</param>
        /// <param name="onCoinPurchaseCallback">Callback when coin purchase is successful</param>
        /// <param name="onAdPurchaseCallback">Callback when ad is watched successfully</param>
        public void ShowDialog(string title, string description, int coinCost, Action onCoinPurchaseCallback, Action onAdPurchaseCallback)
        {
            if (dialogPanel == null) return;

            // Set text content
            if (titleText != null)
                titleText.text = title;
            
            if (descriptionText != null)
                descriptionText.text = description;

            // Store callbacks and cost
            onCoinPurchase = onCoinPurchaseCallback;
            onAdPurchase = onAdPurchaseCallback;
            currentCoinCost = coinCost;

            // Update button texts
            if (coinButtonText != null)
                coinButtonText.text = $"{coinCost}";
            
            if (adButtonText != null)
                adButtonText.text = "Ad";

            // Update button interactability
            UpdateButtonStates();

            // Show dialog
            dialogPanel.SetActive(true);
            
            // Play sound
            AudioManager.Instance?.PlayButtonSound();
        }

        private void UpdateButtonStates()
        {
            // Check if player has enough coins
            bool hasEnoughCoins = CurrencyManager.Instance != null && 
                                  CurrencyManager.Instance.Coins >= currentCoinCost;
            
            if (coinButton != null)
                coinButton.interactable = hasEnoughCoins;

            // Check if ad is available
            bool adAvailable = AdsManager.Instance != null && 
                              AdsManager.Instance.enableAds && 
                              AdsManager.Instance.IsRewardedAdReady();
            
            if (adButton != null)
                adButton.interactable = adAvailable;
        }

        private void OnCoinButtonClicked()
        {
            if (CurrencyManager.Instance == null) return;

            // Try to spend coins
            if (CurrencyManager.Instance.SpendCoins(currentCoinCost))
            {
                Debug.Log($"Purchased with {currentCoinCost} coins!");
                AudioManager.Instance?.PlayButtonSound();
                
                // Execute callback
                onCoinPurchase?.Invoke();
                
                // Close dialog
                CloseDialog();
            }
            else
            {
                Debug.Log("Not enough coins!");
                // Optional: Show "Not Enough Coins" message
            }
        }

        private void OnAdButtonClicked()
        {
            if (AdsManager.Instance == null) return;

            AudioManager.Instance?.PlayButtonSound();

            // Show rewarded ad
            AdsManager.Instance.ShowRewardedAd(
                onReward: () => {
                    Debug.Log("Ad watched successfully!");
                    
                    // Execute callback
                    onAdPurchase?.Invoke();
                    
                    // Close dialog
                    CloseDialog();
                },
                onFail: () => {
                    Debug.Log("Ad failed or was cancelled.");
                    // Dialog stays open so user can try again or use coins
                }
            );
        }

        public void CloseDialog()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
            
            AudioManager.Instance?.PlayButtonSound();
        }
    }
}

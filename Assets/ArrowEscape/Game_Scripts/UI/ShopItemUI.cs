using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;

namespace UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI priceText;
        public Button actionButton;
        public TextMeshProUGUI actionButtonText;
        public GameObject selectedIndicator; // e.g. a checkmark or "Selected" text overlay

        private ArrowTheme myTheme;
        private ShopUI shopUI;

        public void Setup(ArrowTheme theme, ShopUI ui)
        {
            myTheme = theme;
            shopUI = ui;

            if (iconImage != null) iconImage.sprite = theme.icon;
            if (nameText != null) nameText.text = theme.themeName;
            
            RefreshState();
            
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        void Update()
        {
            if (myTheme == null) return;
            
            // If locked:
            // - If Ads Enabled: Check Ad Ready
            // - If Ads Disabled: Check Coin Balance
            if (!ThemeManager.Instance.IsThemeUnlocked(myTheme.id))
            {
                bool useAds = AdsManager.Instance != null && AdsManager.Instance.enableAds;
                
                if (useAds)
                {
                    bool adReady = AdsManager.Instance != null && AdsManager.Instance.IsRewardedAdReady();
                    if (actionButton != null) actionButton.interactable = adReady;
                }
                else
                {
                    // Use Coins
                    int price = myTheme.price > 0 ? myTheme.price : 500; // Default price if not set
                    bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= price;
                    if (actionButton != null) actionButton.interactable = canAfford;
                }
            }
        }

        public void RefreshState()
        {
            if (myTheme == null) return;

            bool isUnlocked = ThemeManager.Instance.IsThemeUnlocked(myTheme.id);
            bool isSelected = ThemeManager.Instance.IsThemeSelected(myTheme.id);

            if (selectedIndicator != null) selectedIndicator.SetActive(isSelected);

            if (isSelected)
            {
                // Already selected
                if (actionButtonText != null) actionButtonText.text = "Selected";
                actionButton.interactable = false;
                if (priceText != null) priceText.text = "";
            }
            else if (isUnlocked)
            {
                // Unlocked but not selected
                if (actionButtonText != null) actionButtonText.text = "Select";
                actionButton.interactable = true;
                if (priceText != null) priceText.text = "Owned";
            }
            else
            {
                // Locked, need to buy
                bool useAds = AdsManager.Instance != null && AdsManager.Instance.enableAds;
                
                if (useAds)
                {
                    if (actionButtonText != null) actionButtonText.text = "Watch Ad";
                    if (priceText != null) priceText.text = "Free";
                    // Interactable handled in Update()
                }
                else
                {
                    int price = myTheme.price > 0 ? myTheme.price : 500;
                    if (actionButtonText != null) actionButtonText.text = "Buy";
                    if (priceText != null) priceText.text = $"{price}";
                    // Interactable handled in Update()
                }
            }
        }

        private void OnActionButtonClicked()
        {
            if (myTheme == null) return;

            bool isUnlocked = ThemeManager.Instance.IsThemeUnlocked(myTheme.id);

            if (isUnlocked)
            {
                // Select it
                ThemeManager.Instance.SelectTheme(myTheme.id);
                shopUI.RefreshAllItems();
                AudioManager.Instance?.PlayButtonSound();
            }
            else
            {
                // Unlock Logic
                bool useAds = AdsManager.Instance != null && AdsManager.Instance.enableAds;

                if (useAds)
                {
                    // Try to unlock via Ad
                    AdsManager.Instance?.ShowRewardedAd(() => {
                        UnlockAndSelect();
                        Debug.Log($"Theme {myTheme.id} unlocked via Ad!");
                    }, () => {
                        Debug.Log("Ad failed or cancelled.");
                        AudioManager.Instance?.PlayBlockedSound();
                    });
                }
                else
                {
                    // Try to unlock via Coins
                    int price = myTheme.price > 0 ? myTheme.price : 500;
                    if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCoins(price))
                    {
                        UnlockAndSelect();
                        Debug.Log($"Theme {myTheme.id} unlocked via Coins!");
                    }
                    else
                    {
                        Debug.Log("Not enough coins!");
                        AudioManager.Instance?.PlayBlockedSound();
                    }
                }
            }
        }

        private void UnlockAndSelect()
        {
            ThemeManager.Instance.UnlockTheme(myTheme.id);
            // Optionally auto-select? The previous code didn't, but usually shops do. 
            // The previous code REFRESHED, so it would show as "Select". 
            // Let's just Unlock it for now, user can then click Select.
            shopUI.RefreshAllItems();
            AudioManager.Instance?.PlayCoinSpendSound(); 
        }
    }
}

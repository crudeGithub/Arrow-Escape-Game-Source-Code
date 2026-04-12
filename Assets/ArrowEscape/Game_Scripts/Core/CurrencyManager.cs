using UnityEngine;
using System;

namespace Core
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public event Action<int> OnCoinsChanged;

        private const string PREF_COINS = "PlayerCoins";
        public int Coins { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadCoins();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadCoins()
        {
            Coins = PlayerPrefs.GetInt(PREF_COINS, 0); // Default 0 coins
        }

        public void AddCoins(int amount)
        {
            if (amount < 0) return;
            Coins += amount;
            SaveCoins();
            OnCoinsChanged?.Invoke(Coins);
            Debug.Log($"Added {amount} coins. Total: {Coins}");
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;
            if (Coins >= amount)
            {
                Coins -= amount;
                SaveCoins();
                OnCoinsChanged?.Invoke(Coins);
                AudioManager.Instance?.PlayCoinSpendSound();
                Debug.Log($"Spent {amount} coins. Remaining: {Coins}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Not enough coins! Need {amount}, have {Coins}.");
                return false;
            }
        }

        private void SaveCoins()
        {
            PlayerPrefs.SetInt(PREF_COINS, Coins);
            PlayerPrefs.Save();
        }

        [ContextMenu("Add 500 Coins")]
        public void DebugAddCoins()
        {
            AddCoins(500);
        }
        
        [ContextMenu("Reset Coins")]
        public void DebugResetCoins()
        {
            Coins = 0;
            SaveCoins();
            OnCoinsChanged?.Invoke(Coins);
        }
    }
}

using UnityEngine;
using GoogleMobileAds.Api;
using System;

namespace Core
{
    public class AdsManager : MonoBehaviour
    {
        public bool enableAds = true;

        public static AdsManager Instance { get; private set; }

        // Test Ad Unit IDs
#if UNITY_ANDROID
        public string _rewardedAdUnitId = "ca-app-pub-3055395982997327/5311400294";
#elif UNITY_IPHONE
        private string _bannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
        private string _rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
        private string _bannerAdUnitId = "unused";
        private string _rewardedAdUnitId = "unused";
#endif

        private BannerView _bannerView;
        private RewardedAd _rewardedAd;
        private Action _onRewardEarned;
        private Action _onAdFailed;

        void Awake()
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

        void Start()
        {
            if (!enableAds) return;

            // Initialize the Google Mobile Ads SDK.
            MobileAds.Initialize(initStatus => {
                Debug.Log("AdMob Initialized");
                LoadRewardedAd();
/*                CreateBannerView();
*/            });
        }

      /*  public void CreateBannerView()
        {
            if (!enableAds) return;

            Debug.Log("Creating banner view");

            // If we already have a banner, destroy the old one.
            if (_bannerView != null)
            {
                DestroyBannerView();
            }

            // Create a 320x50 banner at top of the screen
            _bannerView = new BannerView(_bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        }
*/
       
        public void DestroyBannerView()
        {
            if (_bannerView != null)
            {
                Debug.Log("Destroying banner view.");
                _bannerView.Destroy();
                _bannerView = null;
            }
        }

        public void LoadRewardedAd()
        {
            if (!enableAds) return;

            // Clean up the old ad before loading a new one.
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            Debug.Log("Loading the rewarded ad.");

            // create our request used to load the ad.
            var adRequest = new AdRequest();

            // send the request to load the ad.
            RewardedAd.Load(_rewardedAdUnitId, adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        Debug.LogError("Rewarded ad failed to load an ad " +
                                       "with error : " + error);
                        return;
                    }

                    Debug.Log("Rewarded ad loaded with response : " +
                              ad.GetResponseInfo());

                    _rewardedAd = ad;
                    RegisterEventHandlers(_rewardedAd);
                });
        }

        public void ShowRewardedAd(Action onReward, Action onFail = null)
        {
            if (!enableAds)
            {
                onReward?.Invoke();
                return;
            }

            _onRewardEarned = onReward;
            _onAdFailed = onFail;

            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show((Reward reward) =>
                {
                    // TODO: Reward the user.
                    Debug.Log(String.Format("Rewarded ad rewarded the user. Type: {0}, amount: {1}",
                        reward.Type, reward.Amount));
                    
                    _onRewardEarned?.Invoke();
                    _onRewardEarned = null;
                    _onAdFailed = null;
                });
            }
            else
            {
                Debug.Log("Rewarded ad is not ready yet.");
                _onAdFailed?.Invoke();
                _onAdFailed = null;
                _onRewardEarned = null;
                
                // Try to load again for next time
                LoadRewardedAd();
            }
        }
        
        public bool IsRewardedAdReady()
        {
            if (!enableAds) return true;
            return _rewardedAd != null && _rewardedAd.CanShowAd();
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Rewarded ad recorded an impression.");
            };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                Debug.Log("Rewarded ad was clicked.");
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Rewarded ad full screen content opened.");
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Rewarded ad full screen content closed.");
                // Reload the ad so that we can show another one as soon as possible.
                LoadRewardedAd();
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("Rewarded ad failed to open full screen content " +
                               "with error : " + error);
                // Reload the ad so that we can show another one as soon as possible.
                LoadRewardedAd();
                _onAdFailed?.Invoke();
            };
        }
    }
}

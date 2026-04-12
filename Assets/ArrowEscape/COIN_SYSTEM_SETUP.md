# Coin System Implementation Guide

A robust coin currency system has been added to the game. This system handles:
1.  **Persistence**: Saving/Loading coin balance.
2.  **Rewards**: Giving coins on Level Win.
3.  **Ad Logic**: If Ads are disabled, powerups can be bought with coins.
4.  **Animation**: Visual feedback when coins are earned.

## 1. Scene Setup
You need to add two new Managers to your scene (or ensure they exist).

### A. CurrencyManager
1.  Create an Empty GameObject in your Initialization Scene (or the Main Scene).
2.  Name it `CurrencyManager`.
3.  Add the `CurrencyManager` script to it.
4.  *Note: It will persist across scenes automatically.*

### B. CoinFeedbackManager
1.  Create an Empty GameObject.
2.  Name it `CoinFeedbackManager`.
3.  Add the `CoinFeedbackManager` script to it.
4.  **Important**: Assign the `Coin Prefab` field in the inspector.
    -   *Creating a Coin Prefab*:
        1.  Create a UI Image (Right Click > UI > Image).
        2.  Set its sprite to your Coin Icon.
        3.  Set size (e.g., 50x50).
        4.  Add the `CoinAnimation` script (for rotation/pulse).
        5.  Drag this object into your Project folder to make it a Prefab.
        6.  Delete it from the scene.
        7.  Assign this prefab to `CoinFeedbackManager`.

## 2. UI Setup
The `UIManager` script has been updated to display coins.

1.  Select your `UIManager` GameObject.
2.  In the inspector, look for the **Coin UI** section (or the new fields).
3.  **Coin Text**: Create a TextMeshProUGUI element in your HUD (e.g., top right corner) and assign it here.
4.  **Coin Icon Transform**: Assign the Coin Icon image next to the text. This is where the animated coins will fly to.
5.  *Tip*: Ensure these UI elements are NOT children of the `InGameUI` object if you want them to be visible during the Win Screen (since `InGameUI` is hidden on win). If they are part of `InGameUI`, the animation target might disappear when the level ends.

## 3. Audio Setup
1.  Select `AudioManager`.
2.  Assign `Coin Receive Sound` and `Coin Spend Sound` if not already done.

## 4. How it Works
-   **Winning**: `LevelManager` automatically awards **50 Coins** on level win and calls `CoinFeedbackManager` to play the animation.
-   **Spending**:
    -   If `AdsManager.enableAds` is **ON**: Hints/Grid/Skip buttons show Rewarded Ads.
    -   If `AdsManager.enableAds` is **OFF**:
        -   Hints/Grid cost **100 Coins**.
        -   Skip Level costs **200 Coins**.
        -   If the player has enough coins, the action succeeds immediately.
        -   If not, a debug log "Not enough coins" is shown (you may want to add a UI tooltip).

## 5. Testing
1.  Play the game.
2.  Win a level -> See coins increase and animation play.
3.  Go to `AdsManager` and uncheck `Enable Ads`.
4.  Try to use a Hint (ensure you have used up your free hints).
5.  It should deduct coins instead of showing an ad.

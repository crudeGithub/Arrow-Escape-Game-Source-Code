# Purchase Dialog Setup Guide

This guide shows you how to set up the Purchase Dialog UI in Unity that allows players to choose between buying powerups with coins or watching ads.

## 1. Create the Dialog UI

### A. Create the Main Panel
1. In your Canvas, **Right Click → UI → Panel**
2. Rename it to `PurchaseDialogPanel`
3. Set **Anchor** to stretch (full screen)
4. Set **Color** to semi-transparent black (e.g., `#00000080`)

### B. Create the Dialog Container
1. **Right Click on PurchaseDialogPanel → UI → Image**
2. Rename to `DialogContainer`
3. Set **Anchor** to center
4. Set **Width**: 600, **Height**: 400
5. Set **Color** to white or your preferred panel color
6. Optional: Add rounded corners using a sprite

### C. Add Title Text
1. **Right Click on DialogContainer → UI → Text - TextMeshPro**
2. Rename to `TitleText`
3. Position at top of dialog (e.g., Y: 120)
4. Set **Font Size**: 36
5. Set **Alignment**: Center
6. Set **Text**: "Get Hints" (placeholder)

### D. Add Description Text
1. **Right Click on DialogContainer → UI → Text - TextMeshPro**
2. Rename to `DescriptionText`
3. Position below title (e.g., Y: 60)
4. Set **Font Size**: 24
5. Set **Alignment**: Center
6. Set **Text**: "Choose how to get powerups" (placeholder)

### E. Add Coin Button
1. **Right Click on DialogContainer → UI → Button - TextMeshPro**
2. Rename to `CoinButton`
3. Position on left side (e.g., X: -100, Y: -40)
4. Set **Width**: 220, **Height**: 80
5. Set button **Color** to gold/yellow (e.g., `#FFD700`)
6. Select the child **Text (TMP)** object:
   - Rename to `CoinButtonText`
   - Set **Text**: "Buy (100 Coins)"
   - Set **Font Size**: 20
   - Set **Color**: Black or dark color for contrast

### F. Add Ad Button
1. **Right Click on DialogContainer → UI → Button - TextMeshPro**
2. Rename to `AdButton`
3. Position on right side (e.g., X: 100, Y: -40)
4. Set **Width**: 220, **Height**: 80
5. Set button **Color** to green (e.g., `#00FF00`)
6. Select the child **Text (TMP)** object:
   - Rename to `AdButtonText`
   - Set **Text**: "Watch Ad"
   - Set **Font Size**: 20

### G. Add Close Button
1. **Right Click on DialogContainer → UI → Button - TextMeshPro**
2. Rename to `CloseButton`
3. Position at top-right corner (e.g., X: 250, Y: 150)
4. Set **Width**: 60, **Height**: 60
5. Set button **Text**: "X"
6. Set **Font Size**: 30

## 2. Create the PurchaseDialog GameObject

1. In your scene hierarchy, **Right Click → Create Empty**
2. Rename to `PurchaseDialog`
3. Add the `PurchaseDialog` script component to it
4. **Important**: This should be in a scene that persists (or the initialization scene)

## 3. Assign References in Inspector

1. Select the `PurchaseDialog` GameObject
2. In the Inspector, assign the following references:

   **Dialog Panel:**
   - `Dialog Panel` → Drag `PurchaseDialogPanel`

   **UI Elements:**
   - `Title Text` → Drag `TitleText`
   - `Description Text` → Drag `DescriptionText`

   **Buttons:**
   - `Coin Button` → Drag `CoinButton`
   - `Ad Button` → Drag `AdButton`
   - `Close Button` → Drag `CloseButton`

   **Button Texts:**
   - `Coin Button Text` → Drag `CoinButtonText`
   - `Ad Button Text` → Drag `AdButtonText`

## 4. Initial State

1. Select `PurchaseDialogPanel` in the hierarchy
2. **Uncheck** the checkbox at the top of the Inspector to disable it
   - The dialog should be hidden by default
   - The script will show it when needed

## 5. Testing

1. Play the game
2. Use up your free hints
3. Click the Hint button
4. The dialog should appear with both options:
   - **Buy (100 Coins)** - enabled if you have enough coins
   - **Watch Ad** - enabled if ads are available

## 6. Customization

### Change Costs
Edit the `ShowDialog()` calls in `UIManager.cs`:
```csharp
coinCost: 100,  // Change this value
```

### Change Styling
- Modify colors, fonts, and sizes in the Unity Inspector
- Add icons to buttons for better visual appeal
- Add animations (scale/fade) when dialog appears

### Add Sound Effects
The dialog already plays button sounds via `AudioManager`. You can customize these in the `AudioManager` settings.

## Troubleshooting

**Dialog doesn't appear:**
- Check that `PurchaseDialogPanel` is initially disabled
- Verify all references are assigned in the Inspector
- Check console for null reference errors

**Buttons don't work:**
- Ensure `EventSystem` exists in your scene
- Check that buttons have the `Button` component
- Verify `PurchaseDialog.cs` script is attached and references are set

**Coin button always disabled:**
- Check that `CurrencyManager` is in the scene and working
- Verify you have enough coins (check `coinText` display)
- Look for "Not enough coins" debug logs

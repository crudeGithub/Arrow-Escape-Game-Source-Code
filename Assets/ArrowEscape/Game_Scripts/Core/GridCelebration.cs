using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles visual dot background and win celebration animation.
    /// Dots start with low opacity during gameplay, then zoom up/down with fade effect on win.
    /// </summary>
    public class GridCelebration : MonoBehaviour
    {
        [Header("Dot Appearance")]
        [Tooltip("Opacity of dots during normal gameplay (0-1)")]
        [Range(0f, 1f)]
        public float normalAlpha = 0.2f;

        [Header("Celebration Settings")]
        [Tooltip("Maximum scale multiplier during zoom")]
        public float maxScale = 1.5f;
        
        [Tooltip("Maximum opacity during celebration (0-1)")]
        [Range(0f, 1f)]
        public float celebrationAlpha = 1f;

        [Tooltip("Duration of the zoom up + fade in (seconds)")]
        public float zoomUpDuration = 0.3f;
        
        [Tooltip("Duration of the zoom down + fade out (seconds)")]
        public float zoomDownDuration = 0.3f;
        
        [Tooltip("Optional delay before starting the animation")]
        public float startDelay = 0.1f;

        private SpriteRenderer[] dotRenderers;

        void Start()
        {
            // Cache all sprite renderers and set initial low opacity
            StartCoroutine(InitializeDots());
        }

        void Update()
        {
            SyncDotColors();
        }

        private void SyncDotColors()
        {
            if (dotRenderers == null) return;
            if (CameraBackgroundToggler.Instance == null) return;

            Color dotColor = CameraBackgroundToggler.Instance.CurrentDotColor;
            foreach (var renderer in dotRenderers)
            {
                if (renderer != null)
                {
                    // Preserve the alpha because it might be animating during celebration
                    float alpha = renderer.color.a;
                    renderer.color = new Color(dotColor.r, dotColor.g, dotColor.b, alpha);
                }
            }
        }

        IEnumerator InitializeDots()
        {
            // Wait a frame to ensure dots are spawned
            yield return null;
            
            dotRenderers = GetComponentsInChildren<SpriteRenderer>();
            SyncDotColors(); // Initial sync
            
            foreach (var renderer in dotRenderers)
            {
                if (renderer != null)
                {
                    Color c = renderer.color;
                    c.a = normalAlpha;
                    renderer.color = c;
                }
            }
        }

        /// <summary>
        /// Starts the zoom up/down + fade animation for all child dot objects.
        /// Calls onComplete when the animation finishes.
        /// </summary>
        public void Celebrate(Action onComplete)
        {
            StartCoroutine(CelebrateRoutine(onComplete));
        }

        private IEnumerator CelebrateRoutine(Action onComplete)
        {
            // Small delay so the player can see the win state first
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            // Refresh renderers in case dots were just created
            dotRenderers = GetComponentsInChildren<SpriteRenderer>();
            
            // Store original scales
            Vector3[] originalScales = new Vector3[dotRenderers.Length];
            for (int i = 0; i < dotRenderers.Length; i++)
            {
                if (dotRenderers[i] != null)
                    originalScales[i] = dotRenderers[i].transform.localScale;
            }

            AudioManager.Instance?.PlayWinSound();

            // ZOOM UP + FADE IN
            float elapsed = 0f;
            while (elapsed < zoomUpDuration)
            {
                float t = elapsed / zoomUpDuration;
                float easedT = EaseOutQuad(t); // Smooth easing
                
                for (int i = 0; i < dotRenderers.Length; i++)
                {
                    if (dotRenderers[i] != null)
                    {
                        // Scale up
                        dotRenderers[i].transform.localScale = Vector3.Lerp(originalScales[i], originalScales[i] * maxScale, easedT);
                        
                        // Fade in logic is now handled in Update for color, 
                        // but we need to update alpha here for animation.
                        Color c = dotRenderers[i].color;
                        c.a = Mathf.Lerp(normalAlpha, celebrationAlpha, easedT);
                        dotRenderers[i].color = c;
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure max values reached
            for (int i = 0; i < dotRenderers.Length; i++)
            {
                if (dotRenderers[i] != null)
                {
                    dotRenderers[i].transform.localScale = originalScales[i] * maxScale;
                    Color c = dotRenderers[i].color;
                    c.a = celebrationAlpha;
                    dotRenderers[i].color = c;
                }
            }

            // ZOOM DOWN + FADE BACK
            elapsed = 0f;
            while (elapsed < zoomDownDuration)
            {
                float t = elapsed / zoomDownDuration;
                float easedT = EaseInQuad(t); // Smooth easing
                
                for (int i = 0; i < dotRenderers.Length; i++)
                {
                    if (dotRenderers[i] != null)
                    {
                        // Scale down
                        dotRenderers[i].transform.localScale = Vector3.Lerp(originalScales[i] * maxScale, originalScales[i], easedT);
                        
                        // Fade back to normal
                        Color c = dotRenderers[i].color;
                        c.a = Mathf.Lerp(celebrationAlpha, normalAlpha, easedT);
                        dotRenderers[i].color = c;
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Return to original state
            for (int i = 0; i < dotRenderers.Length; i++)
            {
                if (dotRenderers[i] != null)
                {
                    dotRenderers[i].transform.localScale = originalScales[i];
                    Color c = dotRenderers[i].color;
                    c.a = normalAlpha;
                    dotRenderers[i].color = c;
                }
            }

            // Invoke callback
            onComplete?.Invoke();
        }

        // Easing functions for smooth animation
        float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        float EaseInQuad(float t) => t * t;

    }
}

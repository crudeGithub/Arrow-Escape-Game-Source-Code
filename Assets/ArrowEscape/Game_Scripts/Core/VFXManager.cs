using UnityEngine;

namespace Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("VFX Settings")]
        public Color blockedFlashColor = Color.red;
        public float flashDuration = 0.3f;

        [Header("Particle Effects")]
        public GameObject winParticlePrefab;
        public GameObject loseParticlePrefab;

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

        public void PlayBlockedAnimation(ArrowUnit arrow)
        {
            StartCoroutine(FlashRoutine(arrow));
        }

        public void PlayWinEffect()
        {
            if (winParticlePrefab != null)
            {
                Vector3 centerPos = GetCameraCenter();
                Instantiate(winParticlePrefab, centerPos, Quaternion.identity);
            }
        }

        public void PlayLoseEffect()
        {
            if (loseParticlePrefab != null)
            {
                Vector3 centerPos = GetCameraCenter();
                Instantiate(loseParticlePrefab, centerPos, Quaternion.identity);
            }
        }

        private Vector3 GetCameraCenter()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;
            
            // Get the center of the camera's viewport in world space
            // Keep Z at 0 for 2D particles
            Vector3 centerPos = cam.transform.position;
            centerPos.z = 0;
            return centerPos;
        }

        private System.Collections.IEnumerator FlashRoutine(ArrowUnit arrow)
        {
            if (arrow == null) yield break;

            // Get all renderers in the arrow (lines and head/body sprites)
            var lineRenderers = arrow.GetComponentsInChildren<LineRenderer>();
            var spriteRenderers = arrow.GetComponentsInChildren<SpriteRenderer>();

            // Store original colors and states
            var originalLineColors = new System.Collections.Generic.Dictionary<LineRenderer, (Color, Color)>();
            foreach (var lr in lineRenderers)
            {
                originalLineColors[lr] = (lr.startColor, lr.endColor);
            }

            var originalSpriteColors = new System.Collections.Generic.Dictionary<SpriteRenderer, Color>();
            foreach (var sr in spriteRenderers)
            {
                originalSpriteColors[sr] = sr.color;
            }

            // Apply Flash Color
            foreach (var lr in lineRenderers)
            {
                lr.startColor = blockedFlashColor;
                lr.endColor = blockedFlashColor;
            }
            foreach (var sr in spriteRenderers)
            {
                sr.color = blockedFlashColor;
            }

            // Shake Effect
            Vector3 originalLocalPos = arrow.transform.localPosition;
            float elapsed = 0;
            float shakeMagnitude = 0.15f;

            while (elapsed < flashDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                
                if (arrow != null)
                    arrow.transform.localPosition = originalLocalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore
            if (arrow != null)
            {
                arrow.transform.localPosition = originalLocalPos;
                
                foreach (var lr in lineRenderers)
                {
                    if (lr != null && originalLineColors.ContainsKey(lr))
                    {
                        lr.startColor = originalLineColors[lr].Item1;
                        lr.endColor = originalLineColors[lr].Item2;
                    }
                }
                foreach (var sr in spriteRenderers)
                {
                    if (sr != null && originalSpriteColors.ContainsKey(sr))
                    {
                        sr.color = originalSpriteColors[sr];
                    }
                }
            }
        }
    }
}

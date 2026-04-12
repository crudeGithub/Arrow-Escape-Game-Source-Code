using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>
    /// Simple component for animated coin prefabs.
    /// Attach this to a UI Image with a coin sprite for better visual effects.
    /// </summary>
    public class CoinAnimation : MonoBehaviour
    {
        [Header("Visual Settings")]
        public bool rotateOnSpawn = true;
        public float rotationSpeed = 360f;
        public bool pulseScale = true;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.1f;

        private Image image;
        private float initialScale = 1f;
        private float pulseTimer = 0f;

        private void Awake()
        {
            image = GetComponent<Image>();
            if (image != null)
            {
                // Set default gold color if no sprite is assigned
                if (image.sprite == null)
                {
                    image.color = new Color(1f, 0.84f, 0f); // Gold
                }
            }
        }

        private void Update()
        {
            if (rotateOnSpawn)
            {
                transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }

            if (pulseScale)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                float scale = initialScale + Mathf.Sin(pulseTimer) * pulseAmount;
                transform.localScale = Vector3.one * scale;
            }
        }
    }
}

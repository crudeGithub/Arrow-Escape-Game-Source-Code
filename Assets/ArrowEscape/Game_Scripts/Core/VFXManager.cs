using UnityEngine;

namespace Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("VFX Settings")]
        public Color blockedFlashColor = Color.white;
        public float flashDuration = 0.2f;

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
            LineRenderer lr = arrow.GetComponent<LineRenderer>();
            if (lr == null) yield break;

            Color originalStart = lr.startColor;
            Color originalEnd = lr.endColor;

            // Also get the arrow head sprite renderer
            SpriteRenderer headSr = null;
            Transform headTransform = arrow.transform.Find("HeadVisual");
            if (headTransform != null)
            {
                headSr = headTransform.GetComponent<SpriteRenderer>();
            }
            Color originalHeadColor = headSr != null ? headSr.color : Color.white;

            // Flash both line and head
            lr.startColor = blockedFlashColor;
            lr.endColor = blockedFlashColor;
            if (headSr != null) headSr.color = blockedFlashColor;

            yield return new WaitForSeconds(flashDuration);

            // Restore original colors
            if (arrow != null && lr != null)
            {
                lr.startColor = originalStart;
                lr.endColor = originalEnd;
            }
            
            if (headSr != null)
            {
                headSr.color = originalHeadColor;
            }
        }
    }
}

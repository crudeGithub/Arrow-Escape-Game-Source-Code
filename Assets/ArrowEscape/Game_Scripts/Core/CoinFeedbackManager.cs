using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Core
{
    public class CoinFeedbackManager : MonoBehaviour
    {
        public static CoinFeedbackManager Instance { get; private set; }

        [Header("Settings")]
        public GameObject coinPrefab;
        public int numberOfCoinsToSpawn = 10;
        public float delayBetweenCoins = 0.05f;
        public float moveDuration = 1.0f;
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Target")]
        // If not set, will try to find the coin icon in UIManager
        public Transform targetTransform; 

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void PlayCoinAnimation(Vector3 startPosition, Action onComplete = null)
        {
            if (coinPrefab == null)
            {
                Debug.LogWarning("CoinFeedbackManager: Coin Prefab is missing!");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(SpawnCoinsRoutine(startPosition, onComplete));
        }

        private IEnumerator SpawnCoinsRoutine(Vector3 startPos, Action onComplete)
        {
            AudioManager.Instance?.PlayCoinReceiveSound();

            // Find target if missing
            if (targetTransform == null && UIManager.Instance != null && UIManager.Instance.coinIconTransform != null)
            {
                targetTransform = UIManager.Instance.coinIconTransform;
            }

            Vector3 endPos = targetTransform != null ? targetTransform.position : Vector3.zero;

            // Heuristic to determine if we are in World Space (Camera) or Screen Space (Overlay)
            // If the target is within typical screen bounds (e.g. > 200 units), it's likely Screen Space pixels.
            // If it's small (e.g. < 50 units), it's likely World Space.
            bool isWorldSpace = true;
            if (Mathf.Abs(endPos.x) > 200 || Mathf.Abs(endPos.y) > 200)
            {
                isWorldSpace = false;
            }

            // Calculate Center Start Position based on Space
            if (startPos == Vector3.zero)
            {
                if (isWorldSpace)
                {
                    // World Space: Convert Viewport Center (0.5, 0.5) to World Point
                    // Use the Z-depth of the target to ensure we are on the same plane
                    float zDepth = targetTransform != null ? Mathf.Abs(Camera.main.transform.position.z - targetTransform.position.z) : 10f;
                    startPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, zDepth));
                }
                else
                {
                    // Screen Space: Just use screen center
                    startPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
                }
            }

            // Parent to the same parent as Coin Icon to ensure similar scaling/sorting, or UIManager transform
            Transform parent = targetTransform != null ? targetTransform.parent : UIManager.Instance.transform;

            for (int i = 0; i < numberOfCoinsToSpawn; i++)
            {
                GameObject coin = Instantiate(coinPrefab, parent);
                
                // Ensure scale/rotation is clean (important for UI)
                coin.transform.localScale = Vector3.one;
                coin.transform.localRotation = Quaternion.identity;

                // Random burst at start
                float burstRadius = isWorldSpace ? 2.0f : 100f; // Adjust radius based on space
                Vector3 burstOffset = (Vector3)UnityEngine.Random.insideUnitCircle * burstRadius;
                coin.transform.position = startPos + burstOffset;
                
                // Create a random control point for the curve
                Vector3 midPoint = (startPos + endPos) / 2f;
                float controlRadius = isWorldSpace ? 5.0f : 300f;
                Vector3 randomOffset = (Vector3)UnityEngine.Random.insideUnitCircle * controlRadius;
                Vector3 controlPoint = midPoint + randomOffset;

                StartCoroutine(MoveCoinRoutine(coin.transform, coin.transform.position, endPos, controlPoint));

                yield return new WaitForSeconds(delayBetweenCoins);
            }

            // Wait for the last coin to likely finish (+ buffer)
            yield return new WaitForSeconds(moveDuration + 0.5f);
            
            onComplete?.Invoke();
        }

        private IEnumerator MoveCoinRoutine(Transform coin, Vector3 startPos, Vector3 endPos, Vector3 controlPoint)
        {
            float elapsed = 0;

            while (elapsed < moveDuration)
            {
                if (coin == null) yield break;

                // Update target pos dynamic check
                if (targetTransform != null) endPos = targetTransform.position;

                float t = elapsed / moveDuration;
                float curvedT = moveCurve.Evaluate(t);

                // Quadratic Bezier: B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2
                Vector3 p0 = startPos;
                Vector3 p1 = controlPoint;
                Vector3 p2 = endPos;

                Vector3 position = Mathf.Pow(1 - curvedT, 2) * p0 + 
                                   2 * (1 - curvedT) * curvedT * p1 + 
                                   Mathf.Pow(curvedT, 2) * p2;

                coin.position = position;
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (coin != null) Destroy(coin.gameObject);
        }
    }
}


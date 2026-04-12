using UnityEngine;

namespace Core
{
    public class CameraBackgroundToggler : MonoBehaviour
    {
        public static CameraBackgroundToggler Instance { get; private set; }

        [Header("Background Colors")]
        public Color color1 = new Color(0.1f, 0.1f, 0.15f); // Dark Navy
        public Color color2 = new Color(0.15f, 0.1f, 0.1f); // Dark Red/Brown
        
        [Header("Dot Colors")]
        public Color dotColor1 = new Color(0.2f, 0.2f, 0.3f); 
        public Color dotColor2 = new Color(0.3f, 0.2f, 0.2f);

        public float transitionDuration = 0.5f;

        private Camera mainCamera;
        private bool isColor1 = true;
        private Coroutine transitionCoroutine;

        public Color CurrentDotColor { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = color1;
                CurrentDotColor = dotColor1;
            }
        }

        public void ToggleBackground()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            isColor1 = !isColor1;
            Color targetBgColor = isColor1 ? color1 : color2;
            Color targetDotColor = isColor1 ? dotColor1 : dotColor2;

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionRoutine(targetBgColor, targetDotColor));
        }

        private System.Collections.IEnumerator TransitionRoutine(Color targetBgColor, Color targetDotColor)
        {
            Color startBgColor = mainCamera.backgroundColor;
            Color startDotColor = CurrentDotColor;
            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                float t = elapsed / transitionDuration;
                mainCamera.backgroundColor = Color.Lerp(startBgColor, targetBgColor, t);
                CurrentDotColor = Color.Lerp(startDotColor, targetDotColor, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.backgroundColor = targetBgColor;
            CurrentDotColor = targetDotColor;
            transitionCoroutine = null;
        }
    }
}

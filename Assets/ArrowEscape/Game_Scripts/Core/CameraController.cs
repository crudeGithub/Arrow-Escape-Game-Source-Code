using UnityEngine;

namespace Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        public float minZoom = 2f;
        public float maxZoom = 20f;
        public float zoomSensitivity = 1f;

        [Header("Pan Settings")]
        public float panSpeed = 1f;

        private Vector3 lastMousePosition;
        private bool isDragging = false;
        private Camera cam;
        
        private Vector2 minBounds;
        private Vector2 maxBounds;
        private bool hasBounds = false;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        public void SetBounds(Vector2Int gridDimensions)
        {
            // Allow some padding around the grid
            float padding = 2f;
            // Assuming grid starts at 0,0
            minBounds = new Vector2(-padding, -padding);
            maxBounds = new Vector2(gridDimensions.x + padding, gridDimensions.y + padding);
            hasBounds = true;
            
            // Set max zoom to cover the whole grid with some margin
            float screenRatio = (float)Screen.width / Screen.height;
            float sizeY = gridDimensions.y / 2f + 1f;
            float sizeX = (gridDimensions.x / 2f + 1f) / screenRatio;
            maxZoom = Mathf.Max(sizeY, sizeX) + 2f;
        }

        public void ResetCamera(Vector3 targetPosition, float targetSize, float duration)
        {
            if (cam == null) cam = GetComponent<Camera>();
            StartCoroutine(ResetCameraRoutine(targetPosition, targetSize, duration));
        }

        private bool isResetting = false;

        private System.Collections.IEnumerator ResetCameraRoutine(Vector3 targetPosition, float targetSize, float duration)
        {
            isResetting = true;
            Vector3 startPos = transform.position;
            float startSize = cam.orthographicSize;
            float elapsed = 0;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t); // Easing

                transform.position = Vector3.Lerp(startPos, targetPosition, t);
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
            cam.orthographicSize = targetSize;
            isResetting = false;
        }

        void Update()
        {
            if (cam == null || isResetting) return;

            HandleZoom();
            HandlePan();
        }

        void HandleZoom()
        {
            // Mouse Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                float newSize = cam.orthographicSize - scroll * zoomSensitivity * 5f;
                cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }

            // Touch Zoom (Pinch)
            if (Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                // Adjust orthographic size
                // 0.01f is a scaling factor for touch sensitivity
                float newSize = cam.orthographicSize + deltaMagnitudeDiff * zoomSensitivity * 0.01f;
                cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }

        void HandlePan()
        {
            // If pinching, don't pan
            if (Input.touchCount >= 2) 
            {
                isDragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // Check if clicking on UI
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                    
                    // Check for touch UI interaction
                    if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                        return;
                }

                lastMousePosition = Input.mousePosition;
                isDragging = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                
                // Convert screen delta to world delta
                // Units per pixel = (orthoSize * 2) / Screen.height
                float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                
                Vector3 move = new Vector3(delta.x * unitsPerPixel, delta.y * unitsPerPixel, 0);
                
                // Move camera opposite to drag
                transform.position -= move;
                
                lastMousePosition = Input.mousePosition;
                
                ClampPosition();
            }
        }

        void ClampPosition()
        {
            if (!hasBounds) return;
            
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            transform.position = pos;
        }
    }
}

using UnityEngine;

public class UIRotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f; // Degrees per second

    void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}

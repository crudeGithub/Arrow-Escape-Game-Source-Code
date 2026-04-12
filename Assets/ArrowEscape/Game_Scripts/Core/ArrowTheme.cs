using UnityEngine;
using System.Collections.Generic;

namespace Core
{
    public enum ArrowVisualType
    {
        LineRenderer,
        SpriteBody
    }

    [System.Serializable]
    public struct LineLayer
    {
        public string layerName;
        [Tooltip("Width of the line at the start (Head).")]
        public float width;
        [Tooltip("Width of the line at the end (Tail). If negative, uses start width.")]
        public float endWidth; 
        [Tooltip("Material for the line renderer.")]
        public Material material;
        [Tooltip("Color of the line. If useArrowColor is true, this multiplies with the arrow's color.")]
        public Color color;
        [Tooltip("If true, multiplies this layer's color with the arrow's assigned color.")]
        public bool useArrowColor;
        [Tooltip("Texture mode for the line.")]
        public LineTextureMode textureMode;
        [Tooltip("Sorting order relative to the arrow.")]
        public int sortingOrderOffset;
        [Tooltip("Offset from the start (Head) in grid units. Positive values shorten the line from the head.")]
        public float startOffset;
        [Tooltip("Offset from the end (Tail) in grid units. Positive values shorten the line from the tail.")]
        public float endOffset;
        [Tooltip("Speed at which the texture scrolls along the line.")]
        public float scrollSpeed;
    }

    [CreateAssetMenu(fileName = "NewArrowTheme", menuName = "Arrow Escape/Arrow Theme")]
    public class ArrowTheme : ScriptableObject
    {
        [Header("Shop Settings")]
        public string id;
        public string themeName;
        public int price;
        public Sprite icon;
        public bool isUnlockedByDefault;

        [Header("General Settings")]
        public ArrowVisualType visualType = ArrowVisualType.LineRenderer;
        
        [Header("Head Settings")]
        [Tooltip("Optional: Assign a Prefab to use as the head. Overrides Sprite settings if assigned.")]
        public GameObject headPrefab;
        
        [Tooltip("Sprite for the arrow head (used if no Prefab assigned).")]
        public Sprite headSprite;
        [Tooltip("Scale of the head.")]
        public float headScale = 1.0f;
        [Tooltip("Rotation offset for the head.")]
        public float headRotationOffset = 0f;
        [Tooltip("Sorting Order for the head.")]
        public int headSortingOrder = 2;
        
        [Header("Head Outline (Sprite Only)")]
        public bool showHeadOutline = false;
        public Color headOutlineColor = Color.black;
        public float headOutlineScale = 1.2f;

        [Tooltip("If true, overrides the arrow's color with the theme's color.")]
        public bool overrideColor = false;
        public Color themeColor = Color.white;

        [Header("Line Renderer Settings")]
        [Tooltip("Smoothness of the line curve (Number of corner vertices). Higher is smoother.")]
        public int lineSmoothness = 5;
        [Tooltip("Define multiple layers for the line (e.g. Border, Glow, Core).")]
        public List<LineLayer> lineLayers = new List<LineLayer>();

        [Header("Sprite Body Settings")]
        [Tooltip("Sprite to use for body segments.")]
        public Sprite bodySprite;
        [Tooltip("Scale of the body sprites.")]
        public float bodyScale = 1.0f;
        [Tooltip("If true, rotates body sprites to follow the path direction.")]
        public bool rotateBodySprites = false;
    }
}

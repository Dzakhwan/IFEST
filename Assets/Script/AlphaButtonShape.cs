using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures UI Raycast / Hover / Click events only trigger on non-transparent sprite pixels
/// for custom-shaped or rotated UI Buttons.
/// 
/// IMPORTANT: The Sprite Texture used by the Image component MUST have 'Read/Write Enabled'
/// checked in its Texture Import Settings in the Unity Inspector.
/// </summary>
[RequireComponent(typeof(Image))]
public class AlphaButtonShape : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("Minimum alpha value required to trigger hit/hover (0.0 to 1.0). 0.1 is recommended.")]
    [SerializeField] private float alphaThreshold = 0.1f;

    private void Awake()
    {
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.alphaHitTestMinimumThreshold = alphaThreshold;
        }
    }
}

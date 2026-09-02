using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float animationSpeed = 15f;

    [Header("Color Highlight (Optional)")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private bool useColorHighlight = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f, 1f); // Warm gold highlight

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color targetColor;
    private bool isHovered = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
        {
            normalColor = targetGraphic.color;
            targetColor = normalColor;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        // Reset scale and color if component gets disabled
        transform.localScale = originalScale;
        if (targetGraphic != null)
            targetGraphic.color = normalColor;
        isHovered = false;
    }

    private void Update()
    {
        // Smoothly interpolate scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        // Smoothly interpolate graphic color if enabled
        if (useColorHighlight && targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, Time.deltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScale;
        if (useColorHighlight) targetColor = hoverColor;

        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = originalScale;
        if (useColorHighlight) targetColor = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovered ? originalScale * hoverScale : originalScale;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}

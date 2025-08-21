#if SerializedCollections && DOTween
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using DG.Tweening;
using EditorAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BetterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Hover Effect")] 
    [SerializeField] private bool doScaleEffect;
    [SerializeField, EnableField(nameof(doScaleEffect))] private float hoverScale;
    [SerializeField, EnableField(nameof(doScaleEffect))] private float scaleTransitionDuration;
    
    private Vector3 originalScale;

    [Header("Elements Color Effect")]
    [SerializeField] private bool doColorEffect;
    [SerializeField, EnableField(nameof(doScaleEffect))] private float colorFadeTransitionDuration;
    
    [SerializeField, EnableField(nameof(doColorEffect))]
    [SerializedDictionary("UI Element", "Colors (default, hover)")] private SerializedDictionary<Graphic, Color[]> elementsColorsDictionary;
    
    private Dictionary<Graphic, Color> defaultElementsColorsDictionary;
    private Dictionary<Graphic, Color> hoverElementsColorsDictionary;

    private void Awake()
    {
        // Cache the original scale of the button
        originalScale = transform.localScale;
        
        // Cache the dictionaries
        defaultElementsColorsDictionary = elementsColorsDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]);
        hoverElementsColorsDictionary = elementsColorsDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[1]);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doScaleEffect) ScaleEffect(true);
        if (doColorEffect) ColorElementsEffect(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doScaleEffect) ScaleEffect(false);
        if (doColorEffect) ColorElementsEffect(false);
    }

    private void ColorElementsEffect(bool isHovering)
    {
        foreach (var elementColorsKvp in isHovering ? hoverElementsColorsDictionary : defaultElementsColorsDictionary)
        {
            elementColorsKvp.Key.DOKill();
            elementColorsKvp.Key.DOColor(elementColorsKvp.Value, colorFadeTransitionDuration);
        }
    }
    
    private void ScaleEffect(bool isHovering)
    {
        transform.DOKill();
        transform.DOScale(isHovering ? hoverScale * Vector3.one : originalScale, scaleTransitionDuration);
    }
}
#endif
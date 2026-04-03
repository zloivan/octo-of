using System;
using OnlyFarms.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OnlyFarms.Locations.UI
{
    public class HotSpotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        public event Action OnClicked;
        public event Action OnHovered;
        public event Action OnHoverExited;

        [SerializeField] private string _id;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Material _shimmerMaterial;

        private MaterialPropertyBlock _mpg;

        private void Start()
        {
            if (_outlineMaterial != null)
                _spriteRenderer.material = _outlineMaterial;
            
            SetBrightness(.5f);
        }

        public void SetBrightness(float brightness)
        {
            _mpg ??= new MaterialPropertyBlock();

            _spriteRenderer.GetPropertyBlock(_mpg);
            _mpg.SetFloat(BrightnessId, brightness);
            _spriteRenderer.SetPropertyBlock(_mpg);
        }

        public string GetId() =>
            _id;

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetBrightness(5f);
            OnHovered?.Invoke();
            OFLogger.Log("Pointer Enter");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetBrightness(.5f);
            OnHoverExited?.Invoke();
            OFLogger.Log("Pointer Exit");
        }

        public void OnPointerClick(PointerEventData eventData) =>
            OnClicked?.Invoke();

        public void SetInteractable(bool isEnabled) =>
            _collider.enabled = isEnabled;

        public void SetShimmer(bool isEnabled) =>
            _spriteRenderer.material = isEnabled ? _shimmerMaterial : _outlineMaterial;
    }
}
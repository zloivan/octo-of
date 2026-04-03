using System;
using OnlyFarms.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OnlyFarms.Locations.UI
{
    public class HotSpotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int DashEnabledId = Shader.PropertyToID("_DashEnabled");
        private static readonly int DashSpeedId = Shader.PropertyToID("_DashSpeed");
        private static readonly int PulseEnabledId = Shader.PropertyToID("_PulseEnabled");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        public event Action OnClicked;
        public event Action OnHovered;
        public event Action OnHoverExited;

        [SerializeField] private string _id;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Material _shimmerMaterial;

        private MaterialPropertyBlock _mpb;

        private void Start()
        {
            if (_outlineMaterial != null)
                _spriteRenderer.material = _outlineMaterial;

            SetBrightness(.5f);
            SetDash(false);
        }

        public void SetBrightness(float brightness)
        {
            _mpb ??= new MaterialPropertyBlock();

            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(BrightnessId, brightness);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }


        public void SetPulse(bool enabled, float speed = 1f)
        {
            _mpb ??= new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(PulseEnabledId, enabled ? 1f : 0f);
            _mpb.SetFloat(PulseSpeedId, speed);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }

        public void SetDash(bool enabled, float speed = -1f)
        {
            _mpb ??= new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DashEnabledId, enabled ? 1f : 0f);
            if (speed > 0f)
            {
                _mpb.SetFloat(DashSpeedId, speed);
            }
            _spriteRenderer.SetPropertyBlock(_mpb);
        }

        public string GetId() =>
            _id;

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetBrightness(5f);
            SetDash(true);
            OnHovered?.Invoke();
            OFLogger.Log("Pointer Enter");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetBrightness(.5f);
            SetDash(false);
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
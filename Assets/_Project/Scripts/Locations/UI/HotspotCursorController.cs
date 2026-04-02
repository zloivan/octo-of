using System;
using OnlyFarms.Locations.Input;
using UnityEngine;

namespace OnlyFarms.Locations.UI
{
    public class HotspotCursorController : MonoBehaviour
    {
        [SerializeField] private Texture2D _cursorTexture;
        [SerializeField] private Vector2 _cursorHotspot = Vector2.zero;

        private IHotspotInput _hotspotInput;

        public void Initialize(IHotspotInput input) =>
            _hotspotInput = input ?? throw new NullReferenceException("Hotspot input cannot be null");

        private void Start()
        {
            _hotspotInput.OnHotspotHovered += UpdateCursorOnHover;
            _hotspotInput.OnHotspotHoverExited += ResetCursorToDefault;
        }

        private void OnDestroy()
        {
            _hotspotInput.OnHotspotHovered -= UpdateCursorOnHover;
            _hotspotInput.OnHotspotHoverExited -= ResetCursorToDefault;
        }

        private static void ResetCursorToDefault(string hotspotId) =>
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        private void UpdateCursorOnHover(string hotspotId) =>
            Cursor.SetCursor(_cursorTexture, _cursorHotspot, CursorMode.Auto);
    }
}
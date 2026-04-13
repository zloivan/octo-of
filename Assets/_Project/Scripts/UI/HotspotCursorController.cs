using System;
using OnlyFarms.Infrastructure.Input;
using UnityEngine;

namespace OnlyFarms.UI
{
    public class HotspotCursorController : IDisposable
    {
        private readonly Texture2D _cursorTexture;
        private readonly Vector2 _cursorHotspot;
        private readonly IHotspotInput _hotspotInput;

        public HotspotCursorController(IHotspotInput input, Texture2D cursorTexture, Vector2 cursorHotspot)
        {
            _hotspotInput = input ?? throw new NullReferenceException("Hotspot input cannot be null");

            _cursorTexture = cursorTexture;
            _cursorHotspot = cursorHotspot;
            
            _hotspotInput.OnHotspotHovered += UpdateCursorOnHover;
            _hotspotInput.OnHotspotHoverExited += ResetCursorToDefault;
        }

        private void ResetCursorToDefault(string hotspotId) =>
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        private void UpdateCursorOnHover(string hotspotId) =>
            Cursor.SetCursor(_cursorTexture, _cursorHotspot, CursorMode.Auto);
        
        public void ResetCursor() =>
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        public void Dispose()
        {
            _hotspotInput.OnHotspotHovered -= UpdateCursorOnHover;
            _hotspotInput.OnHotspotHoverExited -= ResetCursorToDefault;
        }
    }
}
using System;
using OnlyFarms.Locations.UI;
using UnityEngine;

namespace OnlyFarms.Locations.Input
{
    public class MouseHotspotInput : MonoBehaviour, IHotspotInput
    {
        public event Action<string> OnHotspotClicked;
        public event Action<string> OnHotspotHovered;
        public event Action<string> OnHotspotHoverExited;

        public void Register(HotSpotView view)
        {
            view.OnClicked += () => InvokeHotspotClick(view);
            view.OnHovered += () => InvokeHotspotHovered(view);
            view.OnHoverExited += () => InvokeHotspotHoverExited(view);
        }

        private void InvokeHotspotHoverExited(HotSpotView view)
        {
            OnHotspotHoverExited?.Invoke(view.GetId());
        }

        private void InvokeHotspotHovered(HotSpotView view)
        {
            OnHotspotHovered?.Invoke(view.GetId());
        }

        private void InvokeHotspotClick(HotSpotView view)
        {
            OnHotspotClicked?.Invoke(view.GetId());
        }

        public void Clear()
        {
            // OnHotspotClicked = null;
            // OnHotspotHovered = null;
            // OnHotspotHoverExited = null;
        }
    }
}
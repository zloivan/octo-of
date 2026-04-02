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
            view.OnClicked += () => OnHotspotClicked?.Invoke(view.GetId());
            view.OnHovered += () => OnHotspotHovered?.Invoke(view.GetId());
            view.OnHoverExited += () => OnHotspotHoverExited?.Invoke(view.GetId());
        }

        public void Clear()
        {
            OnHotspotClicked = null;
            OnHotspotHovered = null;
            OnHotspotHoverExited = null;
        }
    }
}
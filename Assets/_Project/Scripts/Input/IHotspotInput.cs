using System;

namespace OnlyFarms.Input
{
    public interface IHotspotInput
    {
        event Action<string> OnHotspotClicked;
        event Action<string> OnHotspotHovered;
        event Action<string> OnHotspotHoverExited;
    }
}
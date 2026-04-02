using System;

namespace OnlyFarms.Locations.Input
{
    public interface IHotspotInput
    {
        event Action<string> OnHotspotClicked;
        event Action<string> OnHotspotHovered;
        event Action<string> OnHotspotHoverExited;
    }
}
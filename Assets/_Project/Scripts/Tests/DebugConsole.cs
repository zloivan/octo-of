using Locations;
using Naninovel;

namespace Tests
{
    public class DebugConsole
    {
        [ConsoleCommand("goBack")]
        public static void GoBackConsole()
        {
            Engine.GetService<LocationService>()?.GoBack();
        }

        [ConsoleCommand("enter")]
        public static void EnterConsole(string locationId)
        {
            Engine.GetService<LocationService>()?.Enter(locationId);
        }
        
        [ConsoleCommand("reset")]
        public static void ResetConsole()
        {
            Engine.GetService<LocationService>()?.ResetService();
        }
        
        [ConsoleCommand("consume")]
        public static void ConsumeConsole(string itemId)
        {
            Engine.GetService<LocationService>()?.OnItemClicked(itemId);
        }
    }
    
}
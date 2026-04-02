using Naninovel;
using OnlyFarms.Locations;
using OnlyFarms.Utilities;

namespace OnlyFarms.Tests
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
        
        [ConsoleCommand("save")]
        public static void SaveConsole()
        {
            Engine.GetService<IStateManager>()?.QuickSave();
        }
        
        [ConsoleCommand("load")]
        public static void LoadConsole()
        {
            Engine.GetService<IStateManager>()?.QuickLoad();
        }
        
        [ConsoleCommand("currentLocation")]
        public static void CurrentLocationConsole()
        {
            var location = Engine.GetService<LocationService>()?.GetCurrentLocationId();
            OFLogger.Log($"Current Location: {location}");
        }
    }
    
}
using System.Threading;
using Naninovel;
using OnlyFarms.Locations;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Utilities;

namespace OnlyFarms.Tests
{
    public class DebugConsole
    {
        [ConsoleCommand("goBack")]
        public static void GoBackConsole()
        {
            Engine.GetService<LocationService>()?.GoBack(CancellationToken.None);
        }

        [ConsoleCommand("enter")]
        public static void EnterConsole(string locationId)
        {
            Engine.GetService<LocationService>()?.Enter(locationId, CancellationToken.None);
        }
        
        [ConsoleCommand("reset")]
        public static void ResetConsole()
        {
            Engine.GetService<LocationService>()?.ResetService();
        }
        
        [ConsoleCommand("consume")]
        public static void ConsumeConsole(string itemId)
        {
            Engine.GetService<LocationService>()?.OnHotspotClicked(itemId);
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

        [ConsoleCommand("allQuestsCompleted")]
        public static void AllQuestsCompleted()
        {
            var questService = Engine.GetService<QuestService>();
            if (questService == null)
            {
                OFLogger.Log("Quest service not found");
                return;
            }

            questService.OnAllQuestsCompleted().Forget();
            OFLogger.Log("All quests completed: " + questService.IsQuestCompleted("test_quest"));
        }
    }
    
}
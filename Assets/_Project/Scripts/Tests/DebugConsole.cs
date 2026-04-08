using System.Threading;
using Naninovel;
using OnlyFarms.Locations;
using OnlyFarms.Locations.Commands;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Locations.Services;
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

        [ConsoleCommand("printLocation")]
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

            questService.ForceComplete();
        }

        [ConsoleCommand("exitNarrative")]
        public static void ExitNarrativeConsole(string locationId)
        {
            var command = new ExitNarrativeCommand
            {
                Id = locationId,
            };
            command.Execute().Forget();
        }

        [ConsoleCommand("printHistory")]
        public static void PrintLocationHistory()
        {
            OFLogger.Log("Location History:");
            var locationService = Engine.GetService<LocationService>();
            if (locationService == null)
            {
                OFLogger.Log("Location service not found");
                return;
            }

            OFLogger.Log(locationService.PrintLocationHistory());
        }
        
        [ConsoleCommand("setOnEnter")]
        public static void SetOnEnterScript(string locationId, string script)
        {
            OFLogger.Log("TEST");
            Engine.GetService<GameFlowService>()?.SetLocationNarrativeSource(
                new HardcodedNarrativeSource(locationId, script));
        }
    }
}
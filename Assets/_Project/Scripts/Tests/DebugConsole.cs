using System;
using System.Threading;
using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;
using OnlyFarms.Infrastructure;
using OnlyFarms.Infrastructure.Commands;
using OnlyFarms.Infrastructure.Services;
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
            Engine.GetService<GameFlowService>()?.SetLocationNarrativeSource(
                new HardcodedNarrativeSource(locationId, script));
        }

        [ConsoleCommand("setOnClick")]
        public static void SetOnClickHotspotScript(string hotspotId, string scriptName, string labelName = null)
        {
            Engine.GetService<GameFlowService>()?.SetItemNarrativeSource(
                new HardcodedItemNarrativeSource(hotspotId, scriptName, labelName));
        }

        [ConsoleCommand("secretLivingRoom")]
        public static void SetOnClickHotspotScript()
        {
            Engine.GetService<GameFlowService>()?.SetItemNarrativeSource(
                new HardcodedItemNarrativeSourceLivingRoom());
        }

        [ConsoleCommand("setReturnPoint")]
        public static void SetReturnPoint(string scriptName, string label = null) =>
            Engine.GetService<GameFlowService>()?.SetSessionSource(
                new HardcodedSessionsSource(scriptName, label));

        // 1. TryReport → false если objective уже завершён
        [ConsoleCommand("testQuestObjectiveCompleted")]
        public static void TestObjectiveAlreadyCompleted()
        {
            var def = new QuestObjectiveDefinition { EventId = "barn", RequiredCount = 1 };
            var obj = new QuestObjectiveInstance(def);
            obj.TryReport("barn"); // завершаем
            var result = obj.TryReport("barn"); // повторный
            OFLogger.Log($"[EXPECT false] TryReport after completed: {result}");
        }

        // 2. TryReport → false если eventId не совпадает
        [ConsoleCommand("testQuestObjectiveWrongId")]
        public static void TestObjectiveWrongEventId()
        {
            var def = new QuestObjectiveDefinition { EventId = "barn", RequiredCount = 1 };
            var obj = new QuestObjectiveInstance(def);
            var result = obj.TryReport("field");
            OFLogger.Log($"[EXPECT false] TryReport with wrong eventId: {result}");
        }

        // 3. Счётчик растёт ровно на 1
        [ConsoleCommand("testQuestObjectiveCounter")]
        public static void TestObjectiveCounter()
        {
            var def = new QuestObjectiveDefinition { EventId = "item", RequiredCount = 3 };
            var obj = new QuestObjectiveInstance(def);
            obj.TryReport("item");
            OFLogger.Log($"[EXPECT 1] Count after 1st report: {obj.GetCurrentCount}");
            obj.TryReport("item");
            OFLogger.Log($"[EXPECT 2] Count after 2nd report: {obj.GetCurrentCount}");
        }

        // 4. QuestInstance.IsCompleted() — только если все objectives завершены
        [ConsoleCommand("testQuestInstanceCompleted")]
        public static void TestQuestInstanceIsCompleted()
        {
            var def = new QuestDefinition("Test Quest", new[]
            {
                new QuestObjectiveDefinition { EventId = "event_a", RequiredCount = 1 },
                new QuestObjectiveDefinition { EventId = "event_b", RequiredCount = 1 }
            }, false);

            var qi = new QuestInstance(def);
            qi.TryReport("event_a");
            OFLogger.Log($"[EXPECT false] IsCompleted after 1/2 objectives: {qi.IsCompleted()}");
            qi.TryReport("event_b");
            OFLogger.Log($"[EXPECT true]  IsCompleted after 2/2 objectives: {qi.IsCompleted()}");
        }

        // 5. RequiredCount < 1 → исключение
        [ConsoleCommand("testQuestObjectiveInvalidDef")]
        public static void TestObjectiveInvalidDefinition()
        {
            try
            {
                var def = new QuestObjectiveDefinition { EventId = "x", RequiredCount = 0 };
                var obj = new QuestObjectiveInstance(def);
                OFLogger.Log("[EXPECT exception] No exception thrown — FAIL");
            }
            catch (ArgumentException e)
            {
                OFLogger.Log($"[EXPECT exception] Exception caught — PASS: {e.Message}");
            }
        }
    }
}
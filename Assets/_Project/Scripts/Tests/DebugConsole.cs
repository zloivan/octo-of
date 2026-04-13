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
            OFLogger.Log($"[EXPECT 1] Count after 1st report: {obj.GetCurrentCount()}");
            obj.TryReport("item");
            OFLogger.Log($"[EXPECT 2] Count after 2nd report: {obj.GetCurrentCount()}");
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


        [ConsoleCommand("testSequentialInitialVisibility")]
        public static void TestSequentialInitialVisibility()
        {
            var quests = new[]
            {
                new QuestDefinition("Quest 1",
                    new[] { new QuestObjectiveDefinition { EventId = "e1", RequiredCount = 1 } }, true),
                new QuestDefinition("Quest 2",
                    new[] { new QuestObjectiveDefinition { EventId = "e2", RequiredCount = 1 } }, true),
                new QuestDefinition("Quest 3",
                    new[] { new QuestObjectiveDefinition { EventId = "e3", RequiredCount = 1 } }, true),
            };

            var session = new QuestSession(quests);
            var visible = session.GetVisibleQuests();

            OFLogger.Log($"[EXPECT 1] Initial visible count (IsSequential=true): {visible.Count}");
            OFLogger.Log($"[EXPECT 3] Total quests count: {session.GetAllQuestsList().Count}");
            OFLogger.Log($"[EXPECT Quest 1] First visible quest: {visible[0].GetDefinition().GetDisplayName()}");
        }

        [ConsoleCommand("testSequentialVisibilityAdvance")]
        public static void TestSequentialVisibilityAdvance()
        {
            var quests = new[]
            {
                new QuestDefinition("Quest 1",
                    new[] { new QuestObjectiveDefinition { EventId = "e1", RequiredCount = 1 } }, true),
                new QuestDefinition("Quest 2",
                    new[] { new QuestObjectiveDefinition { EventId = "e2", RequiredCount = 1 } }, true),
                new QuestDefinition("Quest 3",
                    new[] { new QuestObjectiveDefinition { EventId = "e3", RequiredCount = 1 } }, true),
            };

            var session = new QuestSession(quests);

            OFLogger.Log($"[EXPECT 1] Before completion: {session.GetVisibleQuests().Count}");

            // Complete first quest
            session.ReportEvent("e1");

            var visibleAfterFirst = session.GetVisibleQuests().Count;
            OFLogger.Log($"[EXPECT 2] After completing quest 1: {visibleAfterFirst}");

            // Complete second quest
            session.ReportEvent("e2");

            var visibleAfterSecond = session.GetVisibleQuests().Count;
            OFLogger.Log($"[EXPECT 3] After completing quest 2: {visibleAfterSecond}");
        }

        [ConsoleCommand("testReportEventInvisibleQuest")]
        public static void TestReportEventInvisibleQuest()
        {
            var quests = new[]
            {
                new QuestDefinition("Quest 1",
                    new[] { new QuestObjectiveDefinition { EventId = "e1", RequiredCount = 1 } }, true),
                new QuestDefinition("Quest 2",
                    new[] { new QuestObjectiveDefinition { EventId = "e2", RequiredCount = 1 } }, true),
            };

            var session = new QuestSession(quests);

            // Try to report event for invisible quest (Quest 2)
            var result = session.ReportEvent("e2");
            OFLogger.Log($"[EXPECT true] ReportEvent for invisible quest returns IsEmpty: {result.IsEmpty}");

            // Report event for visible quest (Quest 1)
            var resultVisible = session.ReportEvent("e1");
            OFLogger.Log($"[EXPECT false] ReportEvent for visible quest returns IsEmpty: {resultVisible.IsEmpty}");
        }

        [ConsoleCommand("testSnapshotRestoresState")]
        public static void TestSnapshotRestoresState()
        {
            var quests = new[]
            {
                new QuestDefinition("Quest 1", new[]
                {
                    new QuestObjectiveDefinition { EventId = "e1", RequiredCount = 3 }
                }, true),
                new QuestDefinition("Quest 2", new[]
                {
                    new QuestObjectiveDefinition { EventId = "e2", RequiredCount = 2 }
                }, true),
            };

            var session = new QuestSession(quests);

            // Report some events
            session.ReportEvent("e1");
            session.ReportEvent("e1");
            session.ReportEvent("e1"); // Quest 1 completed, visibility should advance
            session.ReportEvent("e2");

            var originalCount = session.GetAllQuestsList()[0].GetObjectives()[0].GetCurrentCount();
            var originalVisibleCount = session.GetVisibleQuests().Count;
            var snapshot = session.GetSnapshot();

            OFLogger.Log($"Original CurrentCount for Quest1/Obj0: {originalCount}");
            OFLogger.Log($"Original VisibleCount: {originalVisibleCount}");
            OFLogger.Log($"Snapshot VisibleCount: {snapshot.VisibleCount}");

            // Create new session and load snapshot
            var newSession = new QuestSession(quests);
            newSession.LoadSnapshot(snapshot);

            var restoredCount = newSession.GetAllQuestsList()[0].GetObjectives()[0].GetCurrentCount();
            var restoredVisibleCount = newSession.GetVisibleQuests().Count;

            OFLogger.Log($"[EXPECT {originalCount}] Restored CurrentCount for Quest1/Obj0: {restoredCount}");
            OFLogger.Log($"[EXPECT {originalVisibleCount}] Restored VisibleCount: {restoredVisibleCount}");
        }

        [ConsoleCommand("testSnapshotInvalidIndices")]
        public static void TestSnapshotInvalidIndices()
        {
            var quests = new[]
            {
                new QuestDefinition("Quest 1", new[]
                {
                    new QuestObjectiveDefinition { EventId = "e1", RequiredCount = 1 }
                }, false),
            };

            var session = new QuestSession(quests);

            var invalidSnapshot = new QuestSessionSnapshot
            {
                VisibleCount = 1,
                ObjectiveProgress = new[]
                {
                    new QuestObjectiveProgress
                        { QuestIndex = 999, ObjectiveIndex = 0, CurrentCount = 1 }, // Invalid QuestIndex
                    new QuestObjectiveProgress
                        { QuestIndex = 0, ObjectiveIndex = 999, CurrentCount = 1 }, // Invalid ObjectiveIndex
                    new QuestObjectiveProgress { QuestIndex = 0, ObjectiveIndex = 0, CurrentCount = 1 }, // Valid
                }
            };

            try
            {
                session.LoadSnapshot(invalidSnapshot);
                var count = session.GetAllQuestsList()[0].GetObjectives()[0].GetCurrentCount();
                OFLogger.Log($"[EXPECT 1] No exception, valid progress applied. CurrentCount: {count}");
                OFLogger.Log("[PASS] Invalid indices were skipped without exception");
            }
            catch (Exception e)
            {
                OFLogger.Log($"[FAIL] Exception thrown: {e.Message}");
            }
        }

        [ConsoleCommand("testDoubleRoundTrip")]
        public static void TestDoubleRoundTrip()
        {
            var quests = new[]
            {
                new QuestDefinition("Quest 1", new[]
                {
                    new QuestObjectiveDefinition { EventId = "e1", RequiredCount = 3 },
                    new QuestObjectiveDefinition { EventId = "e1b", RequiredCount = 2 },
                }, true),
                new QuestDefinition("Quest 2", new[]
                {
                    new QuestObjectiveDefinition { EventId = "e2", RequiredCount = 1 }
                }, true),
            };

            var session1 = new QuestSession(quests);
            session1.ReportEvent("e1");
            session1.ReportEvent("e1");
            session1.ReportEvent("e1b");

            // First save
            var snapshot1 = session1.GetSnapshot();

            // Load into new session
            var session2 = new QuestSession(quests);
            session2.LoadSnapshot(snapshot1);

            // Second save
            var snapshot2 = session2.GetSnapshot();

            // Compare snapshots
            var sameVisibleCount = snapshot1.VisibleCount == snapshot2.VisibleCount;
            var sameProgressLength = snapshot1.ObjectiveProgress.Length == snapshot2.ObjectiveProgress.Length;

            var sameProgressContent = true;
            if (sameProgressLength)
            {
                for (var i = 0; i < snapshot1.ObjectiveProgress.Length; i++)
                {
                    var p1 = snapshot1.ObjectiveProgress[i];
                    var p2 = snapshot2.ObjectiveProgress[i];
                    if (p1.QuestIndex != p2.QuestIndex ||
                        p1.ObjectiveIndex != p2.ObjectiveIndex ||
                        p1.CurrentCount != p2.CurrentCount)
                    {
                        sameProgressContent = false;
                        break;
                    }
                }
            }

            OFLogger.Log(
                $"[EXPECT true] VisibleCount identical: {sameVisibleCount} ({snapshot1.VisibleCount} vs {snapshot2.VisibleCount})");
            OFLogger.Log(
                $"[EXPECT true] Progress array length identical: {sameProgressLength} ({snapshot1.ObjectiveProgress.Length} vs {snapshot2.ObjectiveProgress.Length})");
            OFLogger.Log($"[EXPECT true] Progress content identical: {sameProgressContent}");
            OFLogger.Log(sameVisibleCount && sameProgressLength && sameProgressContent
                ? "[PASS] Double round-trip produces identical snapshot"
                : "[FAIL] Snapshots differ");
        }
        
        [ConsoleCommand("testDayIdMismatchNote")]
        public static void TestDayIdMismatchNote()
        {
            OFLogger.Log("[INFO] DayId mismatch handling is done in QuestService.LoadServiceState");
            OFLogger.Log("[INFO] When loading a snapshot with different DayId, it should be ignored");
            OFLogger.Log("[INFO] This requires integration testing with QuestService");
        }
    }
}
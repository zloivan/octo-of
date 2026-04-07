using System;
using System.Threading.Tasks;
using Naninovel;
using OnlyFarms.Utilities;

namespace OnlyFarms.Locations.Domain
{
    [InitializeAtRuntime]
    public class QuestService : IStatefulService<GameStateMap>
    {
        private readonly IScriptPlayer _scriptPlayer;
        private readonly LocationService _locationService;

        private string _returnScriptName;
        private string _returnLabel;

        public QuestService(IScriptPlayer scriptPlayer, LocationService locationService)
        {
            _scriptPlayer = scriptPlayer ?? throw new NullReferenceException("Script player service not found");
            _locationService = locationService ?? throw new NullReferenceException("Location service not found");
        }

        public UniTask InitializeService()
        {
            OFLogger.Log("QuestService initialized");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            OFLogger.Log("QuestService reset");
        }

        public void DestroyService()
        {
            OFLogger.Log("QuestService destroyed");
        }

        public void SaveServiceState(GameStateMap stateMap)
        {
            OFLogger.Log("QuestService state saved");
        }

        public UniTask LoadServiceState(GameStateMap stateMap)
        {
            OFLogger.Log("QuestService state loaded");

            return UniTask.CompletedTask;
        }

        public bool IsQuestCompleted(string conditionValue)
        {
            //TODO: TEMP TEST
            OFLogger.Log($"Checking if quest is completed: {conditionValue}");
            if (conditionValue == "test_quest")
            {
                return true;
            }

            return false;
        }

        public void SetReturnPoint(string scriptName, string label)
        {
            _returnScriptName = scriptName;
            _returnLabel = label;
        }

        public async UniTask OnAllQuestsCompleted()
        {
            _locationService.SetFreeRoamMode(false).Forget();

            if (string.IsNullOrEmpty(_returnScriptName))
            {
                OFLogger.LogWarning("Return point is not set. Cannot return to the narrative.");
                return;
            }

            if (!string.IsNullOrEmpty(_returnLabel))
                await _scriptPlayer.LoadAndPlayAtLabel(_returnScriptName, _returnLabel);
            else
                await _scriptPlayer.LoadAndPlay(_returnScriptName);
        }
    }
}
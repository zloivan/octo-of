using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class DaySessionOrchestratorService : IEngineService
    {
        private readonly QuestService _questService;
        private readonly GameFlowService _gameFlowService;
        private readonly IQuestRepository _questRepository;


        public DaySessionOrchestratorService(QuestService questService, GameFlowService gameFlowService,
            GameConfig gameConfig)
        {
            _questService = questService;
            _gameFlowService = gameFlowService;
            _questRepository = gameConfig.QuestConfig;
        }

        public UniTask InitializeService()
        {
            OFLogger.Log("<color=blue>Initialized</color>");
            return UniTask.CompletedTask;
        }

        public void ResetService() =>
            OFLogger.Log("<color=yellow>Reset Called...</color>");

        public void DestroyService() =>
            OFLogger.Log("<color=red>Destroyed</color>");

        public void StartDay(string dayId) =>
            OFLogger.Log($"Start day: {dayId} requested...");
    }
}
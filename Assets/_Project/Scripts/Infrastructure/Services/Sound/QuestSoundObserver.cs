using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain.Quests;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services.Sound
{
    [InitializeAtRuntime]
    public class QuestSoundObserver : IEngineService
    {
        private readonly QuestService _questService;
        private readonly IAudioManager _audioManager;
        private readonly GameSoundConfigSO _gameConfig;

        public QuestSoundObserver(QuestService questService, IAudioManager audioManager, GameConfig gameConfig)
        {
            _questService = questService;
            _audioManager = audioManager;
            _gameConfig = gameConfig.SoundConfig;
        }

        public UniTask InitializeService()
        {
            _questService.OnQuestObjectiveTicked += QuestService_OnQuestObjectiveTicked;
            _questService.OnQuestCompleted += QuestService_OnQuestCompleted;
            _questService.OnAllQuestsCompleted += QuestService_OnAllQuestsCompleted;
            
            OFLogger.Log("<color=blue>Initialized...</color>");
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _questService.OnQuestObjectiveTicked -= QuestService_OnQuestObjectiveTicked;
            _questService.OnQuestCompleted -= QuestService_OnQuestCompleted;
            _questService.OnAllQuestsCompleted -= QuestService_OnAllQuestsCompleted;
            
            OFLogger.Log("<color=red>Destroyed...</color>");
        }

        public void ResetService()
        {
        }

        private UniTask QuestService_OnAllQuestsCompleted()
        {
            _audioManager.PlaySfx(_gameConfig.QuestCompleted);

            return UniTask.CompletedTask;
        }

        private void QuestService_OnQuestCompleted(QuestInstance obj) =>
            _audioManager.PlaySfx(_gameConfig.QuestCrossed);

        private void QuestService_OnQuestObjectiveTicked(QuestInstance arg1, QuestObjectiveInstance arg2) =>
            _audioManager.PlaySfx(_gameConfig.QuestTicked);
    }
}
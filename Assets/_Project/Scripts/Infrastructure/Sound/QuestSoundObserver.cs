using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;
using OnlyFarms.Infrastructure.Services;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Sound
{
    [InitializeAtRuntime]
    public class QuestSoundObserver : IEngineService
    {
        private readonly QuestService _questService;
        private readonly IAudioManager _audioManager;
        private readonly GameSoundConfigSO _soundConfig;

        public QuestSoundObserver(QuestService questService, IAudioManager audioManager, GameSoundConfigSO soundConfig)
        {
            _questService = questService;
            _audioManager = audioManager;
            _soundConfig = soundConfig;
        }

        public UniTask InitializeService()
        {
            OFLogger.Log("<color=blue>Initilized...</color>");

            _questService.OnQuestObjectiveTicked += QuestService_OnQuestObjectiveTicked;
            _questService.OnQuestCompleted += QuestService_OnQuestCompleted;
            _questService.OnAllQuestsCompleted += QuestService_OnAllQuestsCompleted;
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _questService.OnQuestObjectiveTicked -= QuestService_OnQuestObjectiveTicked;
            _questService.OnQuestCompleted -= QuestService_OnQuestCompleted;
            _questService.OnAllQuestsCompleted -= QuestService_OnAllQuestsCompleted;
        }

        public void ResetService()
        {
        }

        private UniTask QuestService_OnAllQuestsCompleted()
        {
            _audioManager.PlaySfx(_soundConfig.QuestTicked);

            return UniTask.CompletedTask;
        }

        private void QuestService_OnQuestCompleted(QuestInstance obj) =>
            _audioManager.PlaySfx(_soundConfig.QuestCrossed);

        private void QuestService_OnQuestObjectiveTicked(QuestInstance arg1, QuestObjectiveInstance arg2) =>
            _audioManager.PlaySfx(_soundConfig.QuestCompleted);
    }
}
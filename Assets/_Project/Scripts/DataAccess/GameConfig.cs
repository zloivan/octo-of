using Naninovel;

namespace OnlyFarms.DataAccess
{
    [EditInProjectSettings]
    public class GameConfig : Configuration
    {
        public LocationConfigSO LocationConfig;
        public GameSoundConfigSO SoundConfig;
        public QuestConfigSO QuestConfig;
    }
}
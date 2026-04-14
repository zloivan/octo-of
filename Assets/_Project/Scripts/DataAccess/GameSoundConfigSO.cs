using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "New Game Sound", menuName = "Configs/GameSoundConfig", order = 0)]
    public class GameSoundConfigSO : ScriptableObject
    {
        [Header("Locations / Point and click")]
        public string TransitionForwardAudioId = "click_movement_forward";
        public string TransitionBackAudioId = "click_movement_back";
        public string ItemPickupAudioId = "click_object";

        [Header("Quests")]
        public string QuestCompleted = "quest_completed";
        public string QuestCrossed = "quest_crossed";
        public string QuestTicked = "quest_ticked";
    }
}
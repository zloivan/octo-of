using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "Configs/Locations/Location Config", order = 0)]
    public class InteractableItemConfigSO : ScriptableObject
    {
        [SerializeField] private ItemType Type;
        [SerializeField] private string OnClickScript;
        [SerializeField] private string ObjectiveTag;
    }

    public enum ItemType
    {
        QuestItem,
        Secret
    }
}
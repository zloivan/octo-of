using System;
using Naninovel;

namespace OnlyFarms.Locations.Domain
{
    public interface IQuestStatusSource
    {
        event Func<UniTask> OnAllQuestsCompleted;
    }
}
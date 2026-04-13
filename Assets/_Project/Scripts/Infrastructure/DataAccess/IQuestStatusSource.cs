using System;
using Naninovel;

namespace OnlyFarms.Infrastructure.DataAccess
{
    public interface IQuestStatusSource
    {
        event Func<UniTask> OnAllQuestsCompleted;
    }
}
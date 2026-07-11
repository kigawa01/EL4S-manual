using System;

namespace EL4S.Realtime
{
    [Serializable]
    public class AlchemyResult
    {
        public string recipeId;
        public bool success;
        public bool result;
        public CheckType checkType;
    }
}

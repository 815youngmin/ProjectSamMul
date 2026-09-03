#nullable enable
using System;

namespace Shared.StaticDatas
{
    public class StaticDataValidationError : Exception
    {
        public StaticDataValidationError(string message) : base(message) { }
    }
}

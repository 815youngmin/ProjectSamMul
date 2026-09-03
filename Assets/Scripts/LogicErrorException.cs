using System;

namespace Z
{

    public class LogicErrorException : Exception
    {
        public LogicErrorException(string message) : base(message)
        {

        }
    }

}
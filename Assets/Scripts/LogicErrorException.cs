using System;

namespace SamMul
{

    public class LogicErrorException : Exception
    {
        public LogicErrorException(string message) : base(message)
        {

        }
    }

}
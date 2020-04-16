using System;
using System.Runtime.Serialization;

namespace Recruiter
{
    [Serializable]
    internal class LINQException : Exception
    {
        public LINQException()
        {
        }

        public LINQException(string message) : base(message)
        {
        }

        public LINQException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LINQException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
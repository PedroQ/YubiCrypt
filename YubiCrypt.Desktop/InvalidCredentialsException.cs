using System;

namespace YubiCrypt.Desktop
{
    [Serializable]
    public class InvalidCredentialsException : YubiCryptEngineException
    {
        public InvalidCredentialsException() { }
        public InvalidCredentialsException(string message) : base(message) { }
        public InvalidCredentialsException(string message, Exception inner) : base(message, inner) { }
        protected InvalidCredentialsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class YubiCryptEngineException : Exception
    {
        public YubiCryptEngineException() { }
        public YubiCryptEngineException(string message) : base(message) { }
        public YubiCryptEngineException(string message, Exception inner) : base(message, inner) { }
        protected YubiCryptEngineException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

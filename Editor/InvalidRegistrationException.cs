using System;

namespace vz777.VContainers.Editor
{
    public class InvalidRegistrationException : Exception
    {
        protected string message;

        protected InvalidRegistrationException(TimeSpan elapsed)
        {
            ElapsedTime = elapsed.TotalSeconds;
        }
        
        protected double ElapsedTime { get; }

        public InvalidRegistrationException(string message, TimeSpan elapsed)
        {
            ElapsedTime = elapsed.TotalSeconds;
            this.message = $"({ElapsedTime:N} seconds)\n{message}";
        }

        public override string Message => message;
    }
}
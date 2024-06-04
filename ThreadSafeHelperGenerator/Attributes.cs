using System;
using System.Collections.Generic;
using System.Text;


namespace ThreadSafeHelperGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ThreadSafeAttribute : Attribute
    {
        public int MaxConcurrentThreads { get; }
        public bool WaitForAvailability { get; }

        public ThreadSafeAttribute(int maxConcurrentThreads = 1, bool waitForAvailability = true)
        {
            MaxConcurrentThreads = maxConcurrentThreads;
            WaitForAvailability = waitForAvailability;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SingleExecutionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DebounceAttribute : Attribute
    {
        public int Milliseconds { get; }

        public DebounceAttribute(int milliseconds)
        {
            Milliseconds = milliseconds;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ReadWriteLockAttribute : Attribute
    {
        public bool IsReadLock { get; }

        public ReadWriteLockAttribute(bool isReadLock)
        {
            IsReadLock = isReadLock;
        }
    }
}

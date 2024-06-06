using System;


namespace ThreadSafeHelperGenerator.Attributes
{
    /// <summary>
    /// Decorate a method with this attribute to execute it in a thread-safe manner.
    /// </summary>
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

    /// <summary>
    /// Decorate a method with this attribute to execute it only once.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SingleExecutionAttribute : Attribute
    {
    }

    /// <summary>
    /// Decorate a method with this attribute to execute it and ignore all other calls until it completes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DebounceAttribute : Attribute
    {
        public int Milliseconds { get; }

        public DebounceAttribute(int milliseconds)
        {
            Milliseconds = milliseconds;
        }
    }

    /// <summary>
    /// Decorate a method with this attribute to execute it in a read or write lock.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReadWriteLockAttribute : Attribute
    {
        public bool IsReadLock { get; }

        public ReadWriteLockAttribute(bool isReadLock)
        {
            IsReadLock = isReadLock;
        }
    }

    /// <summary>
    /// Decorate a method with this attribute to execute it at a specified interval.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TimedExecutionAttribute : Attribute
    {
        public int IntervalMilliseconds { get; }
        public bool RunInBackground { get; }

        public TimedExecutionAttribute(int intervalMilliseconds, bool runInBackground = true)
        {
            IntervalMilliseconds = intervalMilliseconds;
            RunInBackground = runInBackground;
        }
    }

    /// <summary>
    /// Decorate a method with this attribute to cache its result for a specified duration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public int DurationInSeconds { get; }

        public CacheAttribute(int durationInSeconds)
        {
            DurationInSeconds = durationInSeconds;
        }
    }

    /// <summary>
    /// Decorate a method with this attribute to retry it a specified number of times.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RetryAttribute : Attribute
    {
        public int MaxRetries { get; }
        public int DelayMilliseconds { get; }

        public RetryAttribute(int maxRetries, int delayMilliseconds = 1000)
        {
            MaxRetries = maxRetries;
            DelayMilliseconds = delayMilliseconds;
        }
    }

    /// <summary>
    /// Decorate a method with this attribute to execute a fallback method if the original method fails.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FallbackAttribute : Attribute
    {
        public string FallbackMethodName { get; }

        public FallbackAttribute(string fallbackMethodName)
        {
            FallbackMethodName = fallbackMethodName;
        }
    }
}

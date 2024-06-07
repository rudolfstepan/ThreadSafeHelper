# Thread-Safe, Timed Execution, Caching, Retry, Fallback Logic Source Code Generator

This project provides a set of C# attributes and a source code generator to simplify the implementation of thread-safe methods and methods that are regularly executed by a timer. The generator automatically handles synchronization, single execution, debouncing, and read-write locking.

## Attributes

### 1. ThreadSafeAttribute
**Purpose**: Ensures that a method is executed by a limited number of concurrent threads.

- **Parameters**:
  - `MaxConcurrentThreads` (int): Maximum number of threads that can execute the method concurrently.
  - `WaitForAvailability` (bool): Indicates whether additional threads should wait for availability (`true`) or be immediately aborted (`false`).

**Example Usage**:
```csharp
[ThreadSafe(1, waitForAvailability: false)]
public void CriticalSection()
{
    // Method body
}
```
### 2. SingleExecutionAttribute
**Purpose**: Ensures that a method is only executed once.

Example Usage:
```csharp
[SingleExecution]
public void InitializeOnce()
{
    // Method body
}
```
### 3. DebounceAttribute
**Purpose**: Prevents a method from being executed more frequently than a specified interval.

Parameters:
Milliseconds (int): Minimum time interval in milliseconds between consecutive method executions.
Example Usage:
```csharp
[Debounce(1000)]
public void HandleRapidEvents()
{
    // Method body
}
```
### 4. ReadWriteLockAttribute
**Purpose**: Implements a read-write lock to ensure that either multiple read operations or a single write operation can occur concurrently.

Parameters:
IsReadLock (bool): Indicates whether the method uses a read lock (true) or a write lock (false).
Example Usage:
```csharp
[ReadWriteLock(isReadLock: true)]
public void ReadData()
{
    // Method body
}
```
### 5. TimedExecutionAttribute
**Purpose**: Declares that a method should be regularly executed by a timer.

Parameters:
IntervalMilliseconds (int): Interval in milliseconds between method executions.
RunInBackground (bool): Indicates whether the method should be executed in a background thread (true) or the main thread (false).
Example Usage:
```csharp
[TimedExecution(2000, runInBackground: true)]
public void DoWork()
{
    // Method body
}
```
### 6. CacheAttribute
**Purpose**: Caching of functions to reduce computation
DurationInSeconds (int): Specifies the time in seconds of the cached function
```csharp
[Cache(durationInSeconds: 5)]
public string CachedMethod()
{
    // Method body
}
```

### 7. RetryAttribute
**Purpose**: Retry executing of function until maxRetries occurs
MaxRetries (int): Specifies the number of max retries until the function returns
DelayMilliseconds (int): Specifies the time in milliseconds to wait for the next retry
```csharp
[Retry(int maxRetries, int delayMilliseconds = 1000)]
public string Method()
{
    // Method body
}
```
### 8. FallbackAttribute
**Purpose**: Assign a fallback action/callback/method if the execution fails. Can be useful in combination with 'Retry'
MaxRetries (int): Specifies the number of max retries until the function returns
DelayMilliseconds (int): Specifies the time in milliseconds to wait for the next retry
```csharp
[Fallback(string fallbackMethodName)]
public string Method()
{
    // Method body
}
```
Or a combination of both attributes to minimize manuel coding
```csharp
[Retry(int maxRetries, int delayMilliseconds = 1000)]
[Fallback(string fallbackMethodName)]
public string Method()
{
    // Method body
}
```

Source Code Generator
The source code generator automatically generates the necessary code to implement the functionality specified by the attributes. This includes synchronization mechanisms, single execution checks, debouncing logic, read-write locks, and timer-based execution.

Generated Code Example
For a method decorated with multiple attributes, the generator creates a wrapper method that includes all specified functionality. Here is an example of what the generated code might look like:
```csharp
using System;
using System.Threading;
using System.Timers;

namespace YourNamespace
{
    public partial class MyService
    {
        private Timer _DoWork_timer;
        private Thread _DoWork_backgroundThread;

        private static readonly object DoWork_lockObject = new object();
        private static int DoWork_currentConcurrentThreads = 0;

        private static readonly ReaderWriterLockSlim DoWork_rwLock = new ReaderWriterLockSlim();

        private static DateTime DoWork_lastInvocation = DateTime.MinValue;

        private static bool DoWork_hasExecuted = false;

        private void StartDoWorkTimer()
        {
            _DoWork_timer = new Timer(2000);
            _DoWork_timer.Elapsed += (sender, e) =>
            {
                if (true)
                {
                    _DoWork_backgroundThread = new Thread(() => DoWork_Generated());
                    _DoWork_backgroundThread.IsBackground = true;
                    _DoWork_backgroundThread.Start();
                }
                else
                {
                    DoWork_Generated();
                }
            };
            _DoWork_timer.AutoReset = true;
            _DoWork_timer.Start();
        }

        private void StopDoWorkTimer()
        {
            _DoWork_timer?.Stop();
            _DoWork_timer?.Dispose();
        }

        public void DoWork_Generated()
        {
            while (true)
            {
                lock (DoWork_lockObject)
                {
                    if (DoWork_currentConcurrentThreads < 1)
                    {
                        DoWork_currentConcurrentThreads++;
                        break;
                    }
                    else if (!false)
                    {
                        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " wird abgebrochen.");
                        return;
                    }
                }
                Thread.Sleep(100);
            }

            if (true)
            {
                DoWork_rwLock.EnterReadLock();
            }
            else
            {
                DoWork_rwLock.EnterWriteLock();
            }
            try
            {
                if (!DoWork_hasExecuted)
                {
                    lock (this)
                    {
                        if (!DoWork_hasExecuted)
                        {
                            DoWork_hasExecuted = true;
                            DoWork_Implementation();
                            return;
                        }
                    }
                }

                var now = DateTime.Now;
                if ((now - DoWork_lastInvocation).TotalMilliseconds >= 1000)
                {
                    DoWork_lastInvocation = now;
                    DoWork_Implementation();
                    return;
                }

                DoWork_Implementation();
                return;
            }
            finally
            {
                if (true)
                {
                    DoWork_rwLock.ExitReadLock();
                }
                else
                {
                    DoWork_rwLock.ExitWriteLock();
                }
                lock (DoWork_lockObject)
                {
                    DoWork_currentConcurrentThreads--;
                }
            }
        }

        private void DoWork_Implementation()
        {
            // Original method body
            Console.WriteLine($"Die Methode wird von einem Timer aufgerufen. Zeit: {DateTime.Now}");
        }
    }
}
```
Usage Example
Here is an example demonstrating how to use the attributes and the generated code in a service class:
```csharp
public partial class MyService
{
    [ThreadSafe(1, waitForAvailability: false)]
    [SingleExecution]
    [Debounce(1000)]
    [ReadWriteLock(isReadLock: true)]
    [TimedExecution(2000, runInBackground: true)]
    public void DoWork()
    {
        DoWork_Implementation();
    }

    private void DoWork_Implementation()
    {
        Console.WriteLine($"Die Methode wird von einem Timer aufgerufen. Zeit: {DateTime.Now}");
    }
}
```
And the corresponding main program to start and stop the timer:
```csharp
internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Running");

        var service = new MyService();
        var startMethod = service.GetType().GetMethod("StartDoWorkTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        startMethod.Invoke(service, null);

        Console.WriteLine("Drücken Sie eine Taste, um das Programm zu beenden...");
        Console.ReadKey();

        var stopMethod = service.GetType().GetMethod("StopDoWorkTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        stopMethod.Invoke(service, null);
    }
}
```
Benefits
Simplicity: Developers only need to apply the relevant attributes, and the generator handles the rest.
Automation: The generator automates the implementation of synchronization mechanisms and timer logic.
Maintainability: Code remains maintainable and understandable, with complex synchronization logic abstracted away.
Flexibility: Supports combining multiple attributes to cater to different synchronization and execution requirements.

License
This project is licensed under the MIT License - see the LICENSE file for details.

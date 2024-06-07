using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ThreadSafeHelperGenerator.Attributes;


namespace CodeGeneratorTestApp
{
    /// <summary>
    /// Testklasse für den ThreadSafe-Generator
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running");

            var service = new ThreadingServiceExamples();

            // Starte den Timer für die Methode DoWork()
            // Die Methode wird in regelmäßigen Abständen ausgeführt,
            // abhängig von den Parametern des Attributes
            //var startMethod = service.GetType().GetMethod("StartDoWorkTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //startMethod.Invoke(service, null);

            //Console.WriteLine("Timer gestartet.");

            // Beispiel für die Ausführung der Methode Work()
            // Die Methode wird von mehreren Threads aufgerufen
            // Erstelle mehrere Threads
            //Thread[] threads = new Thread[10];
            //for (int i = 0; i < threads.Length; i++)
            //{
            //    // Stellen Sie sicher, dass die generierte Methode aufgerufen wird
            //    threads[i] = new Thread(service.Work_ThreadSafe);
            //    threads[i].Start();
            //}

            //// Warte darauf, dass alle Threads beendet sind
            //foreach (var thread in threads)
            //{
            //    thread.Join();
            //}

            //Console.WriteLine("Alle Threads beendet.");


            //var stopMethod = service.GetType().GetMethod("StopDoWorkTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //stopMethod.Invoke(service, null);
            //Console.WriteLine("Timer beendet.");




            // cacheAttribute Test
            //var startMethod = service.GetType().GetMethod("StartTimerTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //startMethod.Invoke(service, null);


            // retryAttribute & fallback Test
            var retryService = new RetryServiceExamples();

            // combine retry and fallback
            retryService.UnreliableMethod_RetryFallback();

            // retry only
            //retryService.UnreliableMethod_Retry();


            // fallback only
            //retryService.UnreliableMethod_Fallback();

            Console.WriteLine("Retry/Fallback beendet.");


            Console.ReadLine();
        }

    }

    /// <summary>
    /// Testklasse beinhaltet Methoden, die durch den ThreadSafe-Generator umgewandelt werden.
    /// </summary>
    public partial class ThreadingServiceExamples
    {
        #region CacheAttributeTest
        [TimedExecution(300)]
        public void Timer()
        {
            Timer_Implementation();
        }
        private void Timer_Implementation()
        {
            CachedWork();
        }

        [Cache(5)]
        public string CachedWork()
        {
            return CachedWork_Implementation();
        }

        private string CachedWork_Implementation()
        {
            string s =  "CacheAttributeTest " + DateTime.Now;

            Console.WriteLine(s);
            return s;
        }
        #endregion

        /// <summary>
        /// Durch den ThreadSafe-Generator wird die Methode so umgewandelt, dass sie thread-sicher ist.
        /// Abhängig von den Parametern des Attributes wird die Methode entweder nur von einem Thread
        /// gleichzeitig ausgeführt oder mehrere Threads können gleichzeitig darauf zugreifen.
        /// Im Falle von SingleExecution wird die Methode nur einmal ausgeführt.
        /// Im Fall von Debounce wird die Methode nur ausgeführt, wenn sie für eine bestimmte Zeit nicht aufgerufen wurde.
        /// Im Fall von ReadWriteLock wird die Methode nur von einem Thread ausgeführt, während andere Threads warten.
        /// Im Fall von TimedExecution wird die Methode in regelmäßigen Abständen ausgeführt.
        /// Im Fall von ThreadSafe wird die Methode thread-sicher gemacht, indem ein Lock verwendet wird.
        /// In diesem Beispiel wird die Methode Work() durch das Attribut ThreadSafe(2) in eine thread-sichere Methode umgewandelt.
        /// Das bedeutet nur zwei Threads können gleichzeitig auf die Methode zugreifen.
        /// Alle anderen Threads warten, bis die Methode verfügbar ist.
        /// </summary>
        // wir benutzen die Originalmethode, um die Thread-Sicherheit zu testen
        [ThreadSafe(2, waitForAvailability: true)]
        //[SingleExecution]
        //[Debounce(1000)
        //[ReadWriteLock(true)]
        public void Work()
        {
            Work_Implementation();
        }

        private void Work_Implementation()
        {
            Console.WriteLine($"Die Methode wird von Thread {Thread.CurrentThread.ManagedThreadId} ausgeführt.");
            Thread.Sleep(5000); // Simuliert eine langwierige Aufgabe
            Console.WriteLine($"Die Methode ist von Thread {Thread.CurrentThread.ManagedThreadId} fertig.");
        }

        /// <summary>
        /// Startet den Timer, der die Methode DoWork() in regelmäßigen Abständen ausführt.
        /// </summary>
        [TimedExecution(100)]
        public void DoWork()
        {
            DoWork_Implementation();
        }

        private void DoWork_Implementation()
        {
            //Console.WriteLine($"Die Methode wird vom Timer ausgeführt. " + DateTime.Now);

            // je nachdem welches Attribut benutzt wird, muss die passende autogenerierte/dekorierte Methode aufgerufen werden

            Work_ThreadSafe();
            //Work_SingleExecution();
            //Work_Debounce();
            //Work_ReadWriteLock();
        }
    }

    // Beispielklasse mit Methoden, die Retry und Fallback verwenden
    public partial class RetryServiceExamples
    {

        [Retry(3, 2000)]
        [Fallback(nameof(FallbackMethod_Implementation))]
        public bool UnreliableMethod()
        {
            return UnreliableMethod_Implementation();
        }

        private bool UnreliableMethod_Implementation()
        {

            // Simulierte unzuverlässige Aktion
            Console.WriteLine("Attempting operation...");
            throw new Exception("Operation failed");
        }

        private bool FallbackMethod_Implementation()
        {
            Console.WriteLine("Executing fallback method...");

            return false;
        }

        public static class RetryHelper
        {
            public static void Retry(Action action, Action fallback, int maxRetries, int delayMilliseconds)
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        action();
                        return;
                    }
                    catch
                    {
                        if (++attempt > maxRetries)
                        {
                            fallback?.Invoke();
                            throw;
                        }
                        Thread.Sleep(delayMilliseconds);
                    }
                }
            }

            public static T Retry<T>(Func<T> action, Func<T> fallback, int maxRetries, int delayMilliseconds)
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        return action();
                    }
                    catch
                    {
                        if (++attempt > maxRetries)
                        {
                            return fallback != null ? fallback() : default;
                        }
                        Thread.Sleep(delayMilliseconds);
                    }
                }
            }
        }
    }


}
using System;
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

            var service = new MyService();

            // Starte den Timer für die Methode DoWork()
            // Die Methode wird in regelmäßigen Abständen ausgeführt,
            // abhängig von den Parametern des Attributes
            var startMethod = service.GetType().GetMethod("StartDoWorkTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(service, null);

            Console.WriteLine("Timer gestartet.");

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

            Console.ReadLine();
        }
    }


    /// <summary>
    /// Testklasse beinhaltet Methoden, die durch den ThreadSafe-Generator umgewandelt werden.
    /// </summary>
    public partial class MyService
    {
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


}
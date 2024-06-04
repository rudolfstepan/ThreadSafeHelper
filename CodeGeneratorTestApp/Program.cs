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

            var startMethod = service.GetType().GetMethod("StartDoWorkTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(service, null);

            Console.WriteLine("Timer gestartet.");

            // Erstelle mehrere Threads
            Thread[] threads = new Thread[10];
            for (int i = 0; i < threads.Length; i++)
            {
                // Stellen Sie sicher, dass die generierte Methode aufgerufen wird
                threads[i] = new Thread(service.Work_ThreadSafe);
                threads[i].Start();
            }

            // Warte darauf, dass alle Threads beendet sind
            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine("Alle Threads beendet.");

            var stopMethod = service.GetType().GetMethod("StopDoWorkTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            stopMethod.Invoke(service, null);

            Console.WriteLine("Timer beendet.");

            Console.ReadLine();
        }
    }



    public partial class MyService
    {
        // wir benutzen die Originalmethode, um die Thread-Sicherheit zu testen
        [ThreadSafe(2, waitForAvailability: true)]
        //[TimedExecution(100)]
        //[SingleExecution]
        //[Debounce(1000)]
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

        [TimedExecution(1000)]
        public void DoWork()
        {
            DoWork_Implementation();
        }

        private void DoWork_Implementation()
        {
            Console.WriteLine($"Die Methode wird vom Timer ausgeführt. " + DateTime.Now);
        }
    }


}
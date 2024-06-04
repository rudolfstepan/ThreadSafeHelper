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

            Console.ReadLine();
        }
    }


    /// <summary>
    /// Ein Service, der eine Methode enthält, die thread-sicher gemacht werden soll
    /// </summary>
    public partial class MyService
    {
        // wir benutzen die Originalmethode, um die Thread-Sicherheit zu testen
        [ThreadSafe(5, waitForAvailability: true)]
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
    }

}
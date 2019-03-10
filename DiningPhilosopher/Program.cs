using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiningPhilosopher
{
    internal class Program
    {
        private static int _maxThinkingTime;
        private static int _numberOfPhilosophers;
        private static int _maxEatingTime;

        public static object[] Forks { get; set; }

        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Invalid number of Arguments");
            }

            _numberOfPhilosophers = Convert.ToInt32(args[0]);
            _maxThinkingTime = Convert.ToInt32(args[1]);
            _maxEatingTime = Convert.ToInt32(args[2]);

            Forks = new object[_numberOfPhilosophers];
            for (int i = 0; i < _numberOfPhilosophers; i++)
                Forks[i] = new object();

            var source = new CancellationTokenSource();
            var cancellationToken = source.Token;

            var threads = new List<Thread>();

            for (var taskCtr = 0; taskCtr < _numberOfPhilosophers; taskCtr++)
            {
                var ctr = taskCtr;
                var thread = new Thread(() => Eat(ctr, cancellationToken, true));
                thread.Start();
                threads.Add(thread);
            }

            Console.ReadKey();
            source.Cancel();

            try
            {
                foreach (var thread in threads) thread.Join();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                source.Dispose();
            }

            Console.ReadKey();
        }

        private static void Eat(int id, CancellationToken cancelToken, bool removeCircularWait = false)
        {
            var random = new Random();

            var leftFork = id;
            var rightFork = (id + 1) % _numberOfPhilosophers;

            var firstFork = leftFork;
            var secondFork = rightFork;

            if (removeCircularWait && id % 2 != 0)
            {
                firstFork = rightFork;
                secondFork = leftFork;
            }

            var forkWatch = new Stopwatch();
            var overallRuntimeWatch = new Stopwatch();

            overallRuntimeWatch.Start();
            while (true)
            {
                Thread.Sleep(random.Next(0, _maxThinkingTime));
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} finished thinking");

                forkWatch.Start();
                lock (Forks[firstFork])
                {
                    forkWatch.Stop();
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} took first fork: {firstFork}");

                    //Thread.Sleep(1000); //Sleep Here for instant Deadlock in circular wait

                    forkWatch.Start();
                    lock (Forks[secondFork])
                    {
                        forkWatch.Stop();
                        Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} took second fork: {secondFork}");

                        Thread.Sleep(random.Next(0, _maxEatingTime));
                    }
                }
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} is done eating");

                if (cancelToken.IsCancellationRequested)
                    break;
            }
            overallRuntimeWatch.Stop();

            Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} ran for {overallRuntimeWatch.Elapsed}");
            Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} waited {forkWatch.Elapsed} for forks");
            Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} is dead");
        }
    }
}
using System;
using System.Collections.Generic;
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
        private static CancellationTokenSource _source;
        private static CancellationToken _cancellationToken;
        public static object[] Forks { get; set; }

        private static void Main(string[] args)
        {
            if (args.Length != 3)
                Console.WriteLine("Invalid number of Arguments");

            _numberOfPhilosophers = Convert.ToInt32(args[0]);
            _maxThinkingTime = Convert.ToInt32(args[1]);
            _maxEatingTime = Convert.ToInt32(args[2]);

            Forks = Enumerable.Repeat(new object(),  _numberOfPhilosophers).ToArray();

            _source = new CancellationTokenSource();
            _cancellationToken = _source.Token;

            var tasks = new List<Thread>();
            var factory = new TaskFactory(_cancellationToken);

            for (var taskCtr = 0; taskCtr < _numberOfPhilosophers; taskCtr++)
            {
                var ctr = taskCtr;
                var thread = new Thread(() => Eat(ctr));
                thread.Start();
                tasks.Add(thread);
            }

            Console.ReadKey();
            _source.Cancel();

            try
            {
                foreach (var thread in tasks) thread.Join();
            }
            catch (AggregateException e)
            {
                foreach (var v in e.InnerExceptions)
                    Console.WriteLine(e.Message + " " + v.Message);
            }
            finally
            {
                _source.Dispose();
            }

            Console.ReadKey();
        }

        private static void Eat(int id)
        {
            var random = new Random();
            var secondForkIndex = (id + 1) % _numberOfPhilosophers;

            while (true)
            {
                Thread.Sleep(random.Next(0, _maxThinkingTime));
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} finished thinking");

                lock (Forks[id])
                {
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} took first fork: {id}");
                    lock (Forks[secondForkIndex])
                    {
                        Console.WriteLine(
                            $"{DateTime.Now:hh:mm:ss.fff}: phil {id} took second fork: {secondForkIndex}");

                        Thread.Sleep(random.Next(0, _maxEatingTime));
                        Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} is done eating");
                    }
                }

                if (_cancellationToken.IsCancellationRequested)
                    break;
            }

            Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} is dead");
        }
    }
}
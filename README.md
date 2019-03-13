# PPRG_DiningPhilosopher

Matthias Bruzek - Jürgen Schöner

---

## Naive implementation

Out program consists of a minimal console application that takes 3 input parameters in the following order:

- number of philosophers
- thinking time for each philosopher in ms
- eating time for each philosopher in ms

e.g. `5 1500 3000`

```c#
private static void Eat(int id, CancellationToken cancelToken)
{
    var random = new Random();

    var leftFork = id;
    var rightFork = (id + 1) % _numberOfPhilosophers;

    while (true)
    {
        Thread.Sleep(random.Next(0, _maxThinkingTime));
        Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} finished thinking");

        lock (Forks[leftFork])
        {
            Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} took first fork: {leftFork}");

            lock (Forks[rightFork])
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} took second fork: {rightFork}");

                Thread.Sleep(random.Next(0, _maxEatingTime));
            }
        }
        Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} is done eating");

        if (cancelToken.IsCancellationRequested)
            break;
    }

    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: phil {id} is dead");
}
```

### Test 1

At first we test with the input values `5 1500 3000`

### Output 1

```text
05:47:59.712: phil 2 finished thinking
05:47:59.716: phil 2 took first fork: 2
05:47:59.716: phil 2 took second fork: 3
05:48:00.182: phil 1 finished thinking
05:48:00.182: phil 1 took first fork: 1
05:48:00.312: phil 4 finished thinking
05:48:00.312: phil 4 took first fork: 4
05:48:00.312: phil 4 took second fork: 0
05:48:00.475: phil 0 finished thinking
05:48:00.700: phil 3 finished thinking
05:48:00.911: phil 4 is done eating
05:48:00.911: phil 0 took first fork: 0
05:48:01.297: phil 4 finished thinking
05:48:01.297: phil 4 took first fork: 4
05:48:01.813: phil 2 is done eating
.
.
.
```

... and so on. This seems to work well enough for the given input values. We decide that the number of philosophers and the thinking/eating time will be the critical factor, so we will try to get to a deadlock by using really low values.

### Test 2

We set the parameters to `2 50 50`

### Output 2

```text
05:37:54.355: phil 1 finished thinking
05:37:54.359: phil 0 finished thinking
05:37:54.365: phil 1 took first fork: 1
05:37:54.365: phil 0 took first fork: 0
```

As the program is not advancing futher than this point, it is clear that we reached a deadlock state between the two locks, when each philosopher already took his first fork and finds the second one already taken.

This case is also not easily reproducible and happens by chance, sometimes it manages to do some circles until the deadlock kicks in. But we found out that we can improve the chance of failure by reducing the thinking time. With the parameters `2 5 5` (the third parameter is actually useless here) we ran into a deadlock state every time of > 10 test runs.

### Results

We tested the chance to reach deadlock state within 10 seconds for 10 test runs.

Parameters      |   Deadlock chance |
---             |   ---:            |
`5 500 500`     | 0%                |
`3 50 50`       | 10%               |
`2 50 50`       | 90%               |
`2 5 5`         | 100%              |

> Test system: Processor Intel(R) Core(TM) i7-4710HQ CPU @ 2.50GHz, 2501 Mhz, 4 Core(s), 8 Logical Processor(s)

## Deadlock prevention

### What are the necessary conditions for deadlocks

#### circular wait

The circular wait condition is met because each philosopher waits for the fork on his right, which can be already claimed by the philosopher to his right and so on. This condition together with the hold and wait is exactly the reason why the naive implemenation reaches the deadlock.

#### hold and wait

Each philosopher first picks up the fork to the left and holds it, before checking the fork on the right.

#### no preemption

Because we use the lock statement, the releasing is in the control of the calling thread.

#### mutual exclusion

Only a single thread can access the locked resource at a given time.

> While a lock is held, the thread that holds the lock can again acquire and release the lock. Any other thread is blocked from acquiring the lock and waits until the lock is released. [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/lock-statement)

## Remove circular wait condition

We added the functionality to remove the cicular wait so that **odd philosophers to take the right fork first**.

```c#
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
```

### Input

`2 5 5`

### Output

```text
06:33:56.079: phil 1 ran for 00:00:10.5363150
06:33:56.080: phil 0 took second fork: 1
06:33:56.080: phil 1 waited 00:00:01.2529174 for forks
06:33:56.082: phil 1 is dead
06:33:56.087: phil 0 is done eating
06:33:56.087: phil 0 ran for 00:00:10.5445058
06:33:56.088: phil 0 waited 00:00:01.3006672 for forks
06:33:56.089: phil 0 is dead
```

We can see that the circular wait condition is removed, because the second lock will always be possible after the first one is taken. As both philosophers will try to take the same fork at first, it will always be one winning and taking the second fork, while the other waits for the first fork to be free again. This is also true for higher numbers of philosophers.

## Other techniques

We could follow another route by removing any of the other deadlock conditions.

### hold and wait

If a philosopher releases the first fork after failing to get the second fork, and starts to think again, the philosopher to his left will have the chance to get his scond fork and eat. The problem here will be that the philosophers will likely starve, because the right fork will have a high chance to be taken.

### other thoughts

- Two philosophers could share one fork (theoretically)
- there could be an orchestating entity that releases the fork if it is held by a philosopher who waits for the second fork
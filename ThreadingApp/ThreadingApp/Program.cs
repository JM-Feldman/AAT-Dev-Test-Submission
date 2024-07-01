using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using System.IO;

class Program
{
    private static List<int> globalList = new List<int>();
    private static object lockObject = new object();
    private static bool stopThreads = false;

    static void Main(string[] args)
    {
        Thread oddNumberThread = new Thread(AddRandomOddNumbers);
        Thread primeNumberThread = new Thread(AddNegativePrimes);
        Thread evenNumberThread = null;

        oddNumberThread.Start();
        primeNumberThread.Start();

        //The globalList is protected by a lockObject to ensure the thread isn't lost
        while (true)
        {
            lock (lockObject)
            {
                if (globalList.Count >= 250000 && evenNumberThread == null)
                {
                    evenNumberThread = new Thread(AddEvenNumbers);
                    evenNumberThread.Start();
                }

                if (globalList.Count >= 1000000)
                {
                    stopThreads = true;
                    break;
                }
            }
        }

        oddNumberThread.Join();
        primeNumberThread.Join();
        evenNumberThread?.Join();

        lock (lockObject)
        {
            globalList.Sort();
            int oddCount = globalList.Count(x => x % 2 != 0);
            int evenCount = globalList.Count(x => x % 2 == 0);

            Console.WriteLine($"Odd numbers count: {oddCount}");
            Console.WriteLine($"Even numbers count: {evenCount}");

            // Serialize to binary
            using (FileStream fs = new FileStream("globalList.bin", FileMode.Create))
            {
                BinaryWriter writer = new BinaryWriter(fs);
                foreach (int num in globalList)
                {
                    writer.Write(num);
                }
            }

            // Serialize to XML
            XmlSerializer serializer = new XmlSerializer(typeof(List<int>));
            using (TextWriter writer = new StreamWriter("globalList.xml"))
            {
                serializer.Serialize(writer, globalList);
            }
        }
    }

    private static void AddRandomOddNumbers()
    {
        Random random = new Random();
        while (!stopThreads)
        {
            lock (lockObject)
            {
                if (globalList.Count < 1000000)
                {
                    int num = random.Next(1, int.MaxValue);
                    if (num % 2 != 0)
                    {
                        globalList.Add(num);
                    }
                }
            }
        }
    }

    private static void AddNegativePrimes()
    {
        int num = 2;
        while (!stopThreads)
        {
            if (IsPrime(num))
            {
                lock (lockObject)
                {
                    if (globalList.Count < 1000000)
                    {
                        globalList.Add(-num);
                    }
                }
            }
            num++;
        }
    }

    private static void AddEvenNumbers()
    {
        int num = 2;
        while (!stopThreads)
        {
            lock (lockObject)
            {
                if (globalList.Count < 1000000)
                {
                    globalList.Add(num);
                    num += 2;
                }
            }
        }
    }

    private static bool IsPrime(int number)
    {
        if (number < 2) return false;
        for (int i = 2; i <= Math.Sqrt(number); i++)
        {
            if (number % i == 0) return false;
        }
        return true;
    }
}

//Resources used:
//https://www.tutorialspoint.com/csharp/csharp_multithreading.htm
//https://learn.microsoft.com/en-us/dotnet/api/system.io.binarywriter?view=net-8.0
//https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/serialize-object-xml
//https://www.c-sharpcorner.com/blogs/check-a-number-is-prime-number-or-not-in-c-sharp1
//https://www.c-sharpcorner.com/UploadFile/de41d6/monitor-and-lock-in-C-Sharp/

using EF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Test
{
    class Program
    {
        const string ConnectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Users;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        static void Main(string[] args)
        {
            var testsRincewindOrm = new TestsRincewindOrm(GetRincewindOrmContext());
            var testsEf = new TestsEf();

            Console.WriteLine("Прогрев...");
            Progrev(testsRincewindOrm);
            Progrev(testsEf);

            Console.WriteLine();
            Console.WriteLine("Entity Framework Core:");
            var efTime = RunTest(testsEf);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Rincewind ORM Core:");
            var rormTime = RunTest(testsRincewindOrm);
            string mResult;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Entity Framework Core: " + efTime);
            Console.WriteLine("Rincewind ORM Core: " + rormTime);

            if (efTime < rormTime)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                mResult = $"Победил Entity Framework Core c отрывом {rormTime - efTime}ms";
                Console.WriteLine(mResult);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                mResult = $"Победил Rincewind ORM Core c отрывом {efTime - rormTime}ms";
                Console.WriteLine(mResult);
            }

            Console.ForegroundColor = ConsoleColor.Gray;

            Console.ReadKey();
        }

        private static double RunTest(ITests tests)
        {
            var totalTime = 0D;
 
            totalTime += tests.Add(1);
            totalTime += tests.Add(2);
            totalTime += tests.Add(5);
            totalTime += tests.Add(10);
            totalTime += tests.Add(50);
            totalTime += tests.Add(100);
            totalTime += tests.Add(500);

            totalTime += tests.Get(1);
            totalTime += tests.Get(2);
            totalTime += tests.Get(5);
            totalTime += tests.Get(10);
            totalTime += tests.Get(50);
            totalTime += tests.Get(100);
            totalTime += tests.Get(500);
            //totalTime += tests.Get(1000);

            totalTime += tests.Update(1);
            totalTime += tests.Update(2);
            totalTime += tests.Update(5);
            totalTime += tests.Update(10);
            totalTime += tests.Update(50);
            totalTime += tests.Update(100);
            totalTime += tests.Update(500);
            //totalTime += tests.Update(1000);

            totalTime += tests.Search();

            totalTime += tests.Delete(1);
            totalTime += tests.Delete(2);
            totalTime += tests.Delete(5);
            totalTime += tests.Delete(10);
            totalTime += tests.Delete(50);
            totalTime += tests.Delete(100);
            totalTime += tests.Delete(500);
            //totalTime += tests.Delete(1000);
            tests.DeleteAll();
            return totalTime;
        }

        private static void Progrev(ITests tests)
        {
            tests.Add(10);
            tests.Get(10);
            tests.Update(10);
            tests.Search();
            tests.Delete(10);
        }

        static RincewindOrmContext GetRincewindOrmContext()
        {
            return new RincewindOrmContext(ConnectionString);
        }

    
    }
}

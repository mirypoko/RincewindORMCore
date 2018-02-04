using EF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace Test
{
    public class TestsEf : ITests
    {
        public double Add(int count)
        {
            Console.Write($"Добавление записей ({count})... ");
            var entities = new List<User>();
            for (int j = 0; j < count; j++)
            {
                entities.Add(new User());
            }
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = new ApplicationEfContext();
            context.AddRange(entities);
            context.SaveChanges();
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public double Update(int count)
        {
            Console.Write($"Обновление записей ({count})... ");
            var entities = GetUsersFromDb(count);
            foreach (var entity in entities)
            {
                entity.Name = "Update";
            }
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = new ApplicationEfContext();
            context.UpdateRange(entities);
            context.SaveChanges();
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public double Search()
        {
            Console.Write($"Поиск записей... ");
            SetNamesForFirstAndLastElementsInDb();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = new ApplicationEfContext();
            var result = context.Users.Where(u => u.Name == "first" || u.Name == "last").ToList();
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public double Get(int count)
        {
            Console.Write($"Получение записей ({count})... ");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = new ApplicationEfContext();
            var result = context.Users.Take(count).ToList();
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public double Delete(int count)
        {
            Console.Write($"Удаление записей ({count})... ");
            var entities = GetUsersFromDb(count);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = new ApplicationEfContext();
            context.RemoveRange(entities);
            context.SaveChanges();
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public void DeleteAll()
        {
            var context = new ApplicationEfContext();
            var entities = context.Users.ToList();
            context.RemoveRange(entities);
            context.SaveChanges();
        }

        void SetNamesForFirstAndLastElementsInDb()
        {
            var context = new ApplicationEfContext();
            var first = context.Users.First();
            var last = context.Users.Last();
            first.Name = "first";
            last.Name = "last";
            context.SaveChanges();
        }

        List<User> GetUsersFromDb(int count)
        {
            var context = new ApplicationEfContext();
            return context.Users.Take(count).ToList();
        }
    }
}

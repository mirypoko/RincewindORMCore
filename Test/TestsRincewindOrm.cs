using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Test
{
    public class TestsRincewindOrm : ITests
    {
        private RincewindOrmContext Context { get; }

        public TestsRincewindOrm(RincewindOrmContext context)
        {
            Context = context;
        }

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
            Context.Users.AddRange(entities);
            Context.SaveChanges();
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
            Context.Users.UpdateRange(entities);
            Context.SaveChanges();
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
            var result = Context.Users.Where(u => u.Name == "first" || u.Name == "last");
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public double Get(int count)
        {
            Console.Write($"Получение записей ({count})... ");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = Context.Users.ToList(count);
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
            Context.Users.RemoveRange(entities);
            Context.SaveChanges();
            stopwatch.Stop();
            Console.WriteLine($"Время: ({stopwatch.Elapsed.TotalMilliseconds})");
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public void DeleteAll()
        {
            var entities = Context.Users.ToList();
            Context.Users.RemoveRange(entities);
            Context.SaveChanges();
        }

        private void SetNamesForFirstAndLastElementsInDb()
        {
            var first = Context.Users.ToList(1).First();
            var last = Context.Users.ToList().Last();
            first.Name = "first";
            last.Name = "last";
            Context.Users.Update(first);
            Context.Users.Update(last);
            Context.SaveChanges();
        }

        private List<User> GetUsersFromDb(int count)
        {
            return Context.Users.ToList(count);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Test
{
    public static class RandomUtil
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private static readonly Random Rnd = new Random();

        public static int GetRandomNumber(int min, int max)
        {
            return Rnd.Next(min, max);
        }

        public static string GetRandomString(int length)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[GetRandomNumber(0, chars.Length - 1)]);
            }

            return stringBuilder.ToString();
        }
    }
}

using System;

        public static class RandomProvider
        {
            private static readonly Random instance = new Random();

            public static Random GetRandom()
            {
                return instance;
            }
        }
using ID;
using System;
using System.Collections.Generic;

namespace EETester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            double p = 0;
            // EventExchangerLister instance = EventExchangerLister();
            List<string> Devices = EventExchangerLister.Attached();
            Console.WriteLine(Devices[0]);
            EventExchangerLister.RENC_SetPosition(0);
            EventExchangerLister.Start();
            while (true)
            {
                double l = EventExchangerLister.GetAxis(1);
                if ((l != p) && !double.IsNaN(l))
                    Console.WriteLine(l);
                p = l;
                if (Console.KeyAvailable)
                {
                    ConsoleKey k = Console.ReadKey(false).Key;
                    if (k == ConsoleKey.Enter) break;
                    if (k == ConsoleKey.A) EventExchangerLister.RENC_SetUp(100, 0, 50, 1, 1);
                }
            }


            EventExchangerLister.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ID;

namespace EETester
{
    class Program
    {
        static void Main(string[] args)
        {
            EventExchangerLister instance = new EventExchangerLister();
            List<string> Devices = instance.Attached();
            instance.RENC_SetUp(100, 10, 20, 1, 1);
            instance.Start();
            instance.RENC_SetPosition(51);
            while (true)
            {
                var l = instance.GetAxis(1);
                if (!double.IsNaN(l)) Console.WriteLine(l);
                if (Console.KeyAvailable) break;
            }
            instance.Stop();
        }
    }
}

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
            
            instance.Start();
            
            while (true)
            {
                var l = instance.GetAxis(1);
                Console.WriteLine(l);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ID;

namespace EETester
{
    class Program
    {
        static void Main(string[] args)
        {
            EventExchanger instance = new EventExchanger();
            string a = instance.Attached();
            string pn = instance.ProductName();
        }
    }
}

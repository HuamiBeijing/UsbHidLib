using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsbHidLib;

namespace UsbHidTest
{
   class Program
   {
      static void Main(string[] args)
      {
         var devs = Browser.Browse();
         Console.WriteLine(String.Join("\n", devs.Select(info=>String.Format("VID={0:X4}, PID={1:X4}, Product={2}", info.Vid, info.Pid, info.Product))));

         var devInfo = devs.Last();

         using (var dev = new Device(devInfo.Path))
         {
            var data = new byte[5];
            dev.Read(data);
            Console.WriteLine(String.Join(" ", data));
         }
      }

   }
}

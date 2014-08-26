using System;
using System.Linq;
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
            
            while (true)
            {
               var size = dev.GetPreparsedPacketSize();
               if (size == 0)
               {
                  Console.Write(".");
                  continue;
               }
               var data = new byte[size];
               dev.Read(data);
               Console.WriteLine(String.Join(" ", data));
            }
         }
      }

   }
}

using BetterSerial;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var serial = new Port("COM14")
            {
                BaudRate = 115200,
                DtrEnable = true,
                RtsEnable = true,
            };

            using var stream = serial.Open();

            var io = new LineIO(stream, '\u0003', '\u0002');

            io.LineReceived += Console.WriteLine;

            await io.WriteLine("i:0");

            Console.ReadLine();
            await io.DisposeAsync();
        }
    }
}

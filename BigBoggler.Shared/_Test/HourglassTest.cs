using BigBoggler.Timing;
using BigBoggler.Timing.Server;
using BigBoggler.Timing.Client;
using System;

namespace BigBoggler.Test
{
    internal class HourglassTest
    {
        public static void TestBasic()
        {
            // Test base
            var h1 = new Hourglass();
            h1.Duration = TimeSpan.FromSeconds(5);
            h1.Run();
            
            // Test server
            var h2 = new ServerHourglass();
            h2.OnExpiredAsync(async () => { await System.Threading.Tasks.Task.Delay(1); });
            
            // Test client
            var h3 = new ClientHourglass();
            var formatted = h3.RemainingTimeFormatted;
        }
    }
}
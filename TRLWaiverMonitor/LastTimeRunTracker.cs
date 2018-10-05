using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRLWaiverMonitor
{
    public class LastTimeRunTracker
    {
        private string FilePath = Environment.CurrentDirectory + "../../../LastTimeRun.txt";

        public DateTime GetLastTimeRun()
        {
            var lastTimeRun = DateTime.Now.Subtract(new TimeSpan(0,0,10,0));

            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    // Read the stream to a string, and write the string to the console.
                    lastTimeRun = DateTime.Parse(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return lastTimeRun;
        }
        public void UpdateLastTimeRun()
        {
            try
            {   // Open the text file using a stream reader.
                using (StreamWriter sw = new StreamWriter(FilePath, false))
                {
                    sw.Write(DateTime.Now.AddHours(-3));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }
    }
}

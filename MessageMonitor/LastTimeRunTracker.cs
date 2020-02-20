using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TRLWaiverMonitor
{
    public class LastTimeRunTracker
    {
        private string FilePath = Environment.CurrentDirectory + "../../../LastTimeRun.txt";
        private string AssemblyPath;

        public LastTimeRunTracker()
        {
            AssemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase + "../../../../LastTimeRun.txt").AbsolutePath;
        }
        public DateTime GetLastTimeRun()
        {
            var lastTimeRun = DateTime.Now.Subtract(new TimeSpan(0,0,10,0));
            
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(AssemblyPath))
                {
                    // Read the stream to a string, and write the string to the console.
                    lastTimeRun = DateTime.Parse(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"The file could not be read: FilePath: {FilePath}; AssemblyPath: {AssemblyPath}; Message: ");
                Console.WriteLine(e.Message);
            }

            return lastTimeRun;
        }
        public void UpdateLastTimeRun()
        {
            try
            {   // Open the text file using a stream reader.
                using (StreamWriter sw = new StreamWriter(AssemblyPath, false))
                {
                    sw.Write(DateTime.Now.AddHours(-3));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"The file could not be written: FilePath: {FilePath}; AssemblyPath: {AssemblyPath}; Message: ");
                Console.WriteLine(e.Message);
            }
        }
    }
}

using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace TRLWaiverMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            //SlackClientTest.TestPostMessage();
            var options = new ChromeOptions();
            //options.AddArgument("--headless");
            var driver = new ChromeDriver(options);
            Console.Title = "TRLWaiverMonitor";
            var count = 0;
            while (true)
            {
                var currentMinute = DateTime.Now.Minute;
                if (currentMinute > 55)
                {
                    driver.Quit();
                    return;
                }

                try
                {
                    driver.Navigate().GoToUrl("https://football.fantasysports.yahoo.com/f1/11384/transactions");
                }
                catch
                {
                    new SlackClient().PostMessage($"TRL Waiver Program has stopped at {DateTime.Now}.");
                    driver.Quit();
                    return;
                }

                var waiverTable = driver.FindElements(By.XPath("//tbody[1]/tr"));
                var lastTimeRunTracker = new LastTimeRunTracker();
                var lastTimeRun = lastTimeRunTracker.GetLastTimeRun();
                var waivers = new List<Waiver>();

                foreach (var waiverElement in waiverTable)
                {
                    var waiver = new Waiver();
                    if (waiverElement.FindElements(By.XPath("./td/div/span/span[contains(@class, 'F-timestamp')]")).Count == 0)
                    {
                        continue;
                    }
                    
                    waiver.Time = DateTime.ParseExact(waiverElement.FindElement(By.XPath("./td/div/span/span[contains(@class, 'F-timestamp')]")).Text, "MMM d, h:mm tt", null, System.Globalization.DateTimeStyles.None);

                    if (new DateTime(lastTimeRun.Year, lastTimeRun.Month, lastTimeRun.Day, lastTimeRun.Hour, lastTimeRun.Minute, 0, DateTimeKind.Unspecified) <=
                        new DateTime(waiver.Time.Year, waiver.Time.Month, waiver.Time.Day, waiver.Time.Hour, waiver.Time.Minute, 0, DateTimeKind.Unspecified))
                    {
                        waiver.Team = waiverElement.FindElement(By.XPath("./td/div/span/a")).Text;

                        var playerInfo = waiverElement.FindElements(By.XPath("./td/div[@class='Pbot-xs']"));

                        var playerActions = waiverElement.FindElements(By.XPath("./td[1]/span"));
                        if (!playerActions.Any())
                        {
                            continue;
                        }
                        waiver.TopPlayerAction = GetAction(playerActions[0].GetAttribute("class"));
                        if (waiver.TopPlayerAction == Waiver.PlayerAction.Trade)
                        {
                            continue;
                        }

                        if (playerActions.Count == 2)
                        {
                            waiver.BottomPlayerAction = GetAction(playerActions[1].GetAttribute("class"));
                        }
                        else
                        {
                            waiver.BottomPlayerAction = waiver.TopPlayerAction;
                        }
                        ////tbody/tr[1]/td[@class='Ta-end']/div/span/a[@class='Tst-team-name'

                        waiver.TopPlayerName = playerInfo[0].FindElement(By.XPath("./a[1]")).Text;
                        waiver.TopPlayerInfo = playerInfo[0].FindElement(By.XPath("./span")).Text;
                        //tr[contains(@class, 'F-positive')]
                        //tr[contains(@class, 'F-negative')]
                        //tr[contains(@class, 'F-trade')]

                        if (playerInfo.Count == 2)
                        {
                            waiver.BottomPlayerName = playerInfo[1].FindElement(By.XPath("./a[1]")).Text;
                            waiver.BottomPlayerInfo = playerInfo[1].FindElement(By.XPath("./span")).Text;
                        }
                    }
                    else
                    {
                        break;
                    }

                    waivers.Add(waiver);
                }


                if (waivers.Any())
                {
                    var slackString = new StringBuilder();
                    slackString.Append($"*************************\n{DateTime.Now}\n*************************\n");
                    foreach (var waiver in waivers)
                    {
                        slackString.Append(waiver);
                        slackString.Append("\n\n");
                    }

                    new SlackClient().PostMessage(slackString.ToString());
                    //write to slack
                }

                lastTimeRunTracker.UpdateLastTimeRun();

                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 3, 0));
                //driver.Quit();
                count++;
            }
        }

        static void ExecuteCommand(string command)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            process.WaitForExit();

            // *** Read the streams ***
            // Warning: This approach can lead to deadlocks, see Edit #2
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            Console.WriteLine("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            Console.WriteLine("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            Console.WriteLine("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            process.Close();
        }

        private static Waiver.PlayerAction GetAction(string actionClass)
        {
            if (actionClass.Contains("F-positive"))
            {
                return Waiver.PlayerAction.Added;
            }
            if (actionClass.Contains("F-negative"))
            {
                return Waiver.PlayerAction.Dropped;
            }

            return Waiver.PlayerAction.Trade;
        }
    }
}

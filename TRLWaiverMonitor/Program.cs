using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
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

            while (true)
            {
                driver.Navigate().GoToUrl("https://football.fantasysports.yahoo.com/f1/22480/transactions");

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
                    slackString.Append($"*************************\n{DateTime.Now}\n*************************");
                    foreach (var waiver in waivers)
                    {
                        slackString.Append(waiver);
                        slackString.Append("\n\n");
                    }

                    new SlackClient().PostMessage(slackString.ToString());
                    //write to slack
                }

                lastTimeRunTracker.UpdateLastTimeRun();

                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 5, 0));
            }
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

using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
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
            driver.Navigate().GoToUrl("https://football.fantasysports.yahoo.com/f1/22480/transactions");

            var waiverTable = driver.FindElements(By.XPath("//tbody/tr"));

            foreach (var waiverElement in waiverTable)
            {
                var waiver = new Waiver();
                var playerInfo = waiverElement.FindElements(By.XPath("./td/div[@class='Pbot-xs']"));

                var playerActions = waiverElement.FindElements(By.XPath("./td[1]/span"));
                waiver.TopPlayerAction = GetAction(playerActions[0].GetAttribute("class"));
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

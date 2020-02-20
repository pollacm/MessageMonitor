using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageMonitor;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;

namespace TRLWaiverMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var acceptableWatchTimesForCalculation = new List<string>
            {
                "minute",
                "minutes"
            };
            //SlackClientTest.TestPostMessage();
            //String pathToProfile = @"C:\Users\cxp6696\ChromeProfiles\User Data";
            String pathToProfile = @"C:\Users\Owner\ChromeProfiles\User Data";
            //string pathToChromedriver = @"C:\Users\cxp6696\source\repos\MessageMonitor\packages\Selenium.WebDriver.ChromeDriver.77.0.3865.4000\driver\win32\chromedriver.exe";
            string pathToChromedriver = @"C:\Users\Owner\source\repos\MessageMonitor\packages\Selenium.WebDriver.ChromeDriver.77.0.3865.4000\driver\win32\chromedriver.exe";
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("user-data-dir=" + pathToProfile);
            Environment.SetEnvironmentVariable("webdriver.chrome.driver", pathToChromedriver);

            ChromeDriver driver = new ChromeDriver(options);
            Console.Title = "MessageMonitor";
            

            driver.NavigateToUrl("https:/studio.youtube.com/channel/UCUDTfpBksfE4KqLYjG9u00g/comments/inbox?utm_campaign=upgrade&utm_medium=redirect&utm_source=%2Fcomments&filter=%5B%5D");
            SelectElement selectBox = new SelectElement(driver.FindElementByXPath("//ytcp-comments-filter[@id='filter-bar']//select[@class='tb-comment-filter-studio-select-auto-load tb-comment-filter-studio-select']"));
            selectBox.SelectByText("100 results");
            var button = driver.FindElementByXPath("//ytcp-comments-filter[@id='filter-bar']//button[@class='tb-btn tb-btn-grey tb-comment-filter-studio-go'][contains(text(),'Go')]");
            button.Click();
            Thread.Sleep(10000);

            var messages = driver.FindElementsByXPath("//body//ytcp-comment-thread");
            ProcessComments(messages);

            driver.NavigateToUrl("https://studio.youtube.com/channel/UCUDTfpBksfE4KqLYjG9u00g/comments/spam?utm_campaign=upgrade&utm_medium=redirect&utm_source=%2Fcomments&filter=%5B%5D");
            Thread.Sleep(3000);
            ScrollToBottom(driver);
            Thread.Sleep(3000);

            messages = driver.FindElementsByXPath("//body//ytcp-comment-thread");
            ProcessComments(messages);
            
            if (messages.Any())
            {
                var slackString = new StringBuilder();
                slackString.Append($"*************************\n{DateTime.Now}\n*************************\n");
                foreach (var message in messages)
                {
                    slackString.Append(message);
                    slackString.Append("\n\n");
                }

                new SlackClient().PostMessage(slackString.ToString());
                //write to slack
            }

            driver.Quit();
        }

        private static void ProcessComments(ReadOnlyCollection<IWebElement> comments)
        {
            foreach (var comment in comments)
            {
                if (comment.FindElements(By.XPath("./ytcp-comment[@id='comment']//yt-formatted-string[@class='author-text style-scope ytcp-comment']")).Count == 1)
                {
                    var commenterName = comment.FindElement(By.XPath("./ytcp-comment[@id='comment']//yt-formatted-string[@class='author-text style-scope ytcp-comment']")).Text;
                    var message = new Message();
                    message.MessengerName = commenterName;

                    var videoName = comment.FindElement(By.XPath("./ytcp-comment[@id='comment']//div//ytcp-comment-video-thumbnail//a//yt-formatted-string")).Text;
                    message.VideoName = videoName;
                    message.Comment = comment.FindElement(By.XPath("./ytcp-comment[@id='comment']//div//div[@id='content']//ytcp-comment-expander//div//yt-formatted-string")).Text;
                    message.ListType = GetSubscriberType(commenterName);

                    var watchTimeAmount = comment.FindElement(By.XPath("./ytcp-comment[1]/div[1]/div[1]/div[2]/div[1]/yt-formatted-string[1]")).Text;
                    //break if comment is already saved
                    if (watchTimeAmount.Contains("days") || watchTimeAmount.Contains("weeks"))
                    {
                        break;
                    }

                    if (watchTimeAmount.Contains("seconds"))
                    {
                        watchTimeAmount = "1 minute";
                    }

                    var watchTimeInMinutes = int.Parse(watchTimeAmount.Split(' ')[0]);

                    var currentTime = DateTime.Now;
                    message.Time = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute - watchTimeInMinutes, 0);
                    message.StartingTimeSlot = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0);
                    message.EndingTimeSlot = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour + 1, 0, 0);

                }
            }
        }

        private static Message.ListTypeEnum GetSubscriberType(string name)
        {
            var partialName = name.Length > 12 ? name.Substring(0, 12).ToLower() : name.ToLower();
            Message.ListTypeEnum messageType;
            if (YoutubeSubscriberManager.Program.whitelist.Any(l => l.Contains(partialName)))
            {
                messageType = Message.ListTypeEnum.White;
            }
            else if (YoutubeSubscriberManager.Program.orangelist.Any(l => l.Contains(partialName)))
            {
                messageType = Message.ListTypeEnum.Orange;
            }
            else if (YoutubeSubscriberManager.Program.yellowlist.Any(l => l.Contains(partialName)))
            {
                messageType = Message.ListTypeEnum.Yellow;
            }
            else if (YoutubeSubscriberManager.Program.blacklist.Any(l => l.Contains(partialName)))
            {
                messageType = Message.ListTypeEnum.Black;
            }
            else if (YoutubeSubscriberManager.Program.pinklist.Any(l => l.Contains(partialName)))
            {
                messageType = Message.ListTypeEnum.Pink;
            }
            else
            {
                messageType = Message.ListTypeEnum.Other;
            }

            return messageType;
        }

        private static void ScrollToBottom(ChromeDriver driver)
        {
            var jse = (IJavaScriptExecutor)driver;
            jse.ExecuteScript("scroll(0, 100000);");
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

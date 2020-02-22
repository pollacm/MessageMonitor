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
using YoutubeSubscriberManager.Comment;
using Extensions = TubeBuddyScraper.Extensions;

namespace TRLWaiverMonitor
{
    class Program
    {
        public static List<Comment> commentsFromPage = new List<Comment>();

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

            driver.NavigateToUrl("https://studio.youtube.com/channel/UCUDTfpBksfE4KqLYjG9u00g/comments/inbox?filter=%5B%5D");
            SelectElement selectBox = new SelectElement(driver.FindElementByXPath("//ytcp-comments-filter[@id='filter-bar']//select[@class='tb-comment-filter-studio-select-auto-load tb-comment-filter-studio-select']"));
            selectBox.SelectByText("100 results");
            var button = driver.FindElementByXPath("//ytcp-comments-filter[@id='filter-bar']//button[@class='tb-btn tb-btn-grey tb-comment-filter-studio-go'][contains(text(),'Go')]");
            button.Click();
            Thread.Sleep(10000);

            var messages = driver.FindElementsByXPath("//body//ytcp-comment-thread");
            LoadComments(messages);

            driver.NavigateToUrl("https://studio.youtube.com/channel/UCUDTfpBksfE4KqLYjG9u00g/comments/spam");
            Thread.Sleep(3000);
            //ScrollToBottom(driver);
            //Thread.Sleep(3000);

            messages = driver.FindElementsByXPath("//body//ytcp-comment-thread");
            LoadComments(messages);
            
            ProcessComments();
            
            var currentTime = DateTime.Now;
            var commentRepo = new CommentRepo();
            var lastHour = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0).AddHours(-1);
            var lastHourOfComments = commentRepo.GetComments().Where(c => c.StartingTimeSlot > lastHour);
            if (lastHourOfComments.Any())
            {
                var slackString = new StringBuilder();
                slackString.Append($"*************************\n{DateTime.Now}\n*************************\n");
                foreach (var lastHourOfComment in lastHourOfComments)
                {
                    slackString.Append(Extensions.WriteComment(lastHourOfComment));
                }

                new SlackClient().PostMessage(slackString.ToString());
                //write to slack
            }

            driver.Quit();
        }

        private static void LoadComments(ReadOnlyCollection<IWebElement> comments)
        {
            foreach (var comment in comments)
            {
                if (comment.FindElements(By.XPath("./ytcp-comment[@id='comment']//yt-formatted-string[@class='author-text style-scope ytcp-comment']")).Count == 1)
                {
                    var commenterName = comment.FindElement(By.XPath("./ytcp-comment[@id='comment']//yt-formatted-string[@class='author-text style-scope ytcp-comment']")).Text;
                    var message = new Comment();
                    message.MessengerName = commenterName;

                    var videoName = comment.FindElement(By.XPath("./ytcp-comment[@id='comment']//div//ytcp-comment-video-thumbnail//a//yt-formatted-string")).Text;
                    message.VideoName = videoName;
                    message.Message = comment.FindElement(By.XPath("./ytcp-comment[@id='comment']//div//div[@id='content']//ytcp-comment-expander//div//yt-formatted-string")).Text;
                    message.ListType = GetSubscriberType(commenterName);

                    var watchTimeAmount = comment.FindElement(By.XPath("./ytcp-comment[1]/div[1]/div[1]/div[2]/div[1]/yt-formatted-string[1]")).Text;
                    //break if comment is already saved
                    if (watchTimeAmount.Contains("day") || watchTimeAmount.Contains("week"))
                    {
                        break;
                    }

                    var minutesSet = false;
                    if (watchTimeAmount.Contains("second") || watchTimeAmount.Contains("minute"))
                    {
                        minutesSet = true;
                    }

                    if (watchTimeAmount.Contains("second"))
                    {
                        watchTimeAmount = "1 minute";
                    }

                    var watchTimeInMinutesOrHours = int.Parse(watchTimeAmount.Split(' ')[0]);

                    if (watchTimeInMinutesOrHours > 5 && !minutesSet)
                        break;

                    var currentTime = DateTime.Now;
                    if (minutesSet)
                    {
                        message.Time = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0).AddMinutes(-watchTimeInMinutesOrHours);
                        message.StartingTimeSlot = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0);
                        message.EndingTimeSlot = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0).AddHours(1);
                    }
                    else
                    {
                        message.StartingTimeSlot = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0).AddHours(-watchTimeInMinutesOrHours);
                        message.EndingTimeSlot = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0).AddHours(-watchTimeInMinutesOrHours + 1);
                    }

                    commentsFromPage.Add(message);
                }
            }
        }

        private static void ProcessComments()
        {
            var commentRepo = new CommentRepo();
            var oldComments = commentRepo.GetEligableVideosUpdateOfMessengers();
            foreach (var oldComment in oldComments)
            {
                commentRepo.SetMatchingCommenterNamesForTimeSlot(oldComments, oldComment);
            }
            foreach (var commentFromPage in commentsFromPage)
            {
                commentRepo.SetMatchingCommenterNamesForTimeSlot(commentsFromPage, commentFromPage);
            }

            var commentsGatheredFromPage = commentsFromPage;

            commentsGatheredFromPage.AddRange(oldComments);
            commentRepo.RefreshComments(commentsGatheredFromPage);
        }

        private static Comment.ListTypeEnum GetSubscriberType(string name)
        {
            var partialName = name.Length > 12 ? name.Substring(0, 12).ToLower() : name.ToLower();
            Comment.ListTypeEnum messageType;
            if (YoutubeSubscriberManager.Program.whitelist.Any(l => l.Contains(partialName)))
            {
                messageType = Comment.ListTypeEnum.White;
            }
            else if (YoutubeSubscriberManager.Program.orangelist.Any(l => l.Contains(partialName)))
            {
                messageType = Comment.ListTypeEnum.Orange;
            }
            else if (YoutubeSubscriberManager.Program.yellowlist.Any(l => l.Contains(partialName)))
            {
                messageType = Comment.ListTypeEnum.Yellow;
            }
            else if (YoutubeSubscriberManager.Program.blacklist.Any(l => l.Contains(partialName)))
            {
                messageType = Comment.ListTypeEnum.Black;
            }
            else if (YoutubeSubscriberManager.Program.pinklist.Any(l => l.Contains(partialName)))
            {
                messageType = Comment.ListTypeEnum.Pink;
            }
            else
            {
                messageType = Comment.ListTypeEnum.Other;
            }

            return messageType;
        }
    }
}

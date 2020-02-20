using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace MessageMonitor
{
    public static class Extensions
    {
        //public static IWebElement WaitUntilElementExists(this ChromeDriver driver, By elementLocator, int timeout = 5000)
        //{
        //    try
        //    {
        //        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
        //        return wait.Until(ExpectedConditions.ElementExists(elementLocator));
        //        return null;
        //    }
        //    catch (NoSuchElementException)
        //    {
        //        Console.WriteLine("Element with locator: '" + elementLocator + "' was not found in current context page.");
        //        throw;
        //    }

        //    return null;
        //}

        public static void NavigateToUrl(this ChromeDriver driver, string url)
        {
            try
            {
                driver.Navigate().GoToUrl(url);
            }
            catch (Exception e)
            {
                //NavigateToUrl(driver, url);
                // Ignore the exception.  
            }
        }
    }
}

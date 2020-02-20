using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRLWaiverMonitor
{
    public static class SlackClientTest
    {
        public static void TestPostMessage()
        {
            SlackClient client = new SlackClient();
            client.PostMessage("THIS IS A TEST MESSAGE! SQUEEDLYBAMBLYFEEDLYMEEDLYMOWWWWWWWW!");
        }
    }
}

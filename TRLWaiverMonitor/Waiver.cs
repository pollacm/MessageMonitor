using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRLWaiverMonitor
{
    public class Waiver
    {
        public string TopPlayerName { get; set; }
        public string TopPlayerInfo { get; set; }
        public PlayerAction TopPlayerAction { get; set; }
        public string BottomPlayerName { get; set; }
        public string BottomPlayerInfo { get; set; }
        public PlayerAction BottomPlayerAction { get; set; }
        public string Team { get; set; }
        public string Time { get; set; }

        public enum PlayerAction
        {
            Added,
            Dropped,
            Trade
        }
    }
}

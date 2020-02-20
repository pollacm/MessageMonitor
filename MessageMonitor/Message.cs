using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor
{
    public class Message
    {
        public string MessengerName { get; set; }
        public string VideoName { get; set; }
        public string Comment { get; set; }
        public DateTime Time { get; set; }
        public DateTime StartingTimeSlot { get; set; }
        public DateTime EndingTimeSlot { get; set; }
        public string AdditionalMessengersForTimeSlot { get; set; }
        public ListTypeEnum ListType { get; set; }

        public enum ListTypeEnum
        {
            White,
            Black,
            Yellow,
            Orange,
            Pink,
            Other
        }
    }
}

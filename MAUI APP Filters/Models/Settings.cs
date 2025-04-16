using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAppFilter.Models
{
    public class Settings
    {
        public List<string> AllowedDomains { get; set; }
        public List<TimeSlot> TimeSlots { get; set; }
    }

    public class TimeSlot
    {
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}

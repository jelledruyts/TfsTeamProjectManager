using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace TeamProjectManager.Modules.BuildAndRelease
{
    public class BuildDefinitionTriggerSchedulesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var buildDefinition = value as BuildDefinition;
            if (buildDefinition == null)
            {
                return null;
            }
            var schedules = new StringBuilder();
            foreach (var schedule in buildDefinition.Triggers.OfType<ScheduleTrigger>().SelectMany(t => t.Schedules).Where(s => s.DaysToBuild != ScheduleDays.None))
            {
                if (schedules.Length > 0)
                {
                    schedules.Append("; ");
                }
                var firstSchedule = true;
                foreach (var scheduleDay in Enum.GetValues(typeof(ScheduleDays)).Cast<ScheduleDays>().Where(s => s != ScheduleDays.All && s != ScheduleDays.None))
                {
                    if (schedule.DaysToBuild.HasFlag(scheduleDay))
                    {
                        if (!firstSchedule)
                        {
                            schedules.Append(", ");
                        }
                        firstSchedule = false;
                        schedules.Append(scheduleDay.ToString());
                    }
                }
                schedules.AppendFormat(culture, " at {0}:{1:00}", schedule.StartHours, schedule.StartMinutes);
            }
            return schedules.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
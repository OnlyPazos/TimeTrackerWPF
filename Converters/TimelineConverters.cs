using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeTracker.Converters
{
    public class IntervalToCanvasLeftConverter : IMultiValueConverter
    {
        // values[0] = Interval.Start (DateTime)
        // values[1] = TimelineStart (DateTime)
        // values[2] = TimelineTotal (TimeSpan)
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4) return 0;
            if (!(values[0] is DateTime start) ||
                !(values[1] is DateTime timelineStart) ||
                !(values[2] is TimeSpan timelineTotal) ||
                !(values[3] is double canvasWidth))
                return 0;

            double proportion = (start - timelineStart).TotalMilliseconds / timelineTotal.TotalMilliseconds;
            return proportion * canvasWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntervalToCanvasWidthConverter : IMultiValueConverter
    {
        // values[0] = Interval.Duration (TimeSpan)
        // values[1] = TimelineTotal (TimeSpan)
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return 0;
            if (!(values[0] is TimeSpan duration)
                || !(values[1] is TimeSpan timelineTotal)
                || !(values[2] is double canvasWidth))
                return 0;

            double proportion = duration.TotalMilliseconds / timelineTotal.TotalMilliseconds;
            return proportion * canvasWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

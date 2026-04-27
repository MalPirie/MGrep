using System;
using System.Globalization;
using System.Windows.Data;

namespace MGrep;

internal class FileNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 1 && values[0] is string file && values[1] is string folder && file.StartsWith(folder))
        {
            return file[folder.Length..].TrimStart('\\');
        }

        return values[0];
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
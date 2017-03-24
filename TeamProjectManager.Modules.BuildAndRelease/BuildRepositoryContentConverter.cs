using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TeamProjectManager.Modules.BuildAndRelease
{
    public class BuildRepositoryContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var repository = value as BuildRepository;
            if (repository == null)
            {
                return null;
            }
            return !string.IsNullOrWhiteSpace(repository.RootFolder) ? repository.RootFolder : repository.DefaultBranch;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
/**
 * @description
 * Converts a bool to a Visibility (Visible or Collapsed).
 *
 * @notes
 * - True => Visible
 * - False => Collapsed
 */

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PromptNormalizerApp
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b && b) return Visibility.Visible;
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException("Only one-way conversion is supported.");
		}
	}
}

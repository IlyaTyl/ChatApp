using System.Windows;
using System.Windows.Media;

public static class VisualTreeHelperExtensions
{
    public static T FindVisualTreeParent<T>(this DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null && !(parent is T))
            parent = VisualTreeHelper.GetParent(parent);
        return parent as T;
    }

    public static T FindVisualOrLogicalParent<T>(this DependencyObject child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T t)
                return t;
            child = VisualTreeHelper.GetParent(child) ?? LogicalTreeHelper.GetParent(child);
        }
        return null;
    }
}
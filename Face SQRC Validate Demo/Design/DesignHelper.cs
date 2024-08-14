using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace FR_Validate_Demo.Design
{
    class Bool2VisibleOrCollapsed : IValueConverter
    {
        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = (bool)value;

            if (bValue != Reverse)
            {
                return Visibility.Visible;
            }
            else
            {
                if (Collapse)
                    return Visibility.Collapsed;
                else
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return !Reverse;
            else
                return Reverse;
        }
    }

    class Num01ToVisibleOrCollapsed : IValueConverter
    {
        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int iValue = (int)value;

            if (iValue >= 1)
                return Visibility.Visible;
            else
            {
                if (Collapse)
                    return Visibility.Collapsed;
                else
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return !Reverse;
            else
                return Reverse;
        }
    }

    class Obj2Enable : IValueConverter
    {
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value == null ? "0" : value.ToString().Trim();

            bool bReturn = false;

            switch (strValue)
            {
                case "":
                case "0":
                    bReturn = false;
                    break;
                default:
                    bReturn = true;
                    break;
            }

            if (Reverse)
                return !bReturn;
            else
                return bReturn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return "1";
                else
                    return "0";
            }
            return "0";
        }
    }

    class MultiObjToVisibilityConverter : IMultiValueConverter
    {

        public bool FirstTrue { get; set; }
        public bool SecondTrue { get; set; }

        public object Convert(object[] values,
                                Type targetType,
                                object parameter,
                                System.Globalization.CultureInfo culture)
        {
            bool b1 = false;
            bool b2 = false;
            bool visible = false;

            b1 = values[0] != null ? true : false;
            b2 = values[1] != null ? true : false;

            if (FirstTrue && !SecondTrue)
            {
                visible = b1 && !b2;
            }
            else if (FirstTrue && SecondTrue)
            {
                visible = b1 && b2;
            }
            else if (!FirstTrue && SecondTrue)
            {
                visible = !b1 && b2;
            }

            if (visible)
                return Visibility.Visible;
            else
                return Visibility.Hidden;
        }

        public object[] ConvertBack(object value,
                                    Type[] targetTypes,
                                    object parameter,
                                    System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class Obj2Visible : IValueConverter
    {
        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public bool bVisible { get; set; }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = false;

            if (value != null)
                bValue = true;

            if (bVisible)
            {
                if (bValue != Reverse)
                {
                    return Visibility.Visible;
                }
                else
                {
                    if (Collapse)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Hidden;
                }
            }
            else
            {
                if (bValue != Reverse)
                    return true;
                else
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (bVisible)
            {
                Visibility visibility = (Visibility)value;

                if (visibility == Visibility.Visible)
                    return !Reverse;
                else
                    return Reverse;
            }
            else
            {
                bool bValue = (bool)value;
                return Reverse & bValue;
            }

        }
    }

    class DropdownBehaviour : Behavior<Button>
    {
        private bool isContextMenuOpen;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(Button.ClickEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        void AssociatedObject_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button source = sender as Button;
            if (source != null && source.ContextMenu != null)
            {
                if (!isContextMenuOpen)
                {
                    // Add handler to detect when the ContextMenu closes
                    source.ContextMenu.AddHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed), true);
                    // If there is a drop-down assigned to this button, then position and display it 
                    source.ContextMenu.PlacementTarget = source;
                    source.ContextMenu.Placement = PlacementMode.Bottom;
                    source.ContextMenu.IsOpen = true;
                    isContextMenuOpen = true;
                }

                //source.AddHandler(Button.)
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(Button.ClickEvent, new RoutedEventHandler(AssociatedObject_Click));
        }

        void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            isContextMenuOpen = false;
            var contextMenu = sender as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
            }

            //var v1 = (Button)(sender as ContextMenu).PlacementTarget;
            //Console.WriteLine(v1.Tag);
            //v1.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Up));
        }
    }

    class Value2Visible : IValueConverter
    {
        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public bool bVisible { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = false;

            if (value != null && value.ToString().ToUpper() != "FALSE" && value.ToString().ToUpper() != "F" && value.ToString().ToUpper() != "0")
                bValue = true;

            if (bVisible)
            {
                if (bValue != Reverse)
                {
                    return Visibility.Visible;
                }
                else
                {
                    if (Collapse)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Hidden;
                }
            }
            else
            {
                if (bValue != Reverse)
                    return true;
                else
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (bVisible)
            {
                Visibility visibility = (Visibility)value;

                if (visibility == Visibility.Visible)
                    return !Reverse;
                else
                    return Reverse;
            }
            else
            {
                bool bValue = (bool)value;
                return Reverse & bValue;
            }

        }
    }
}

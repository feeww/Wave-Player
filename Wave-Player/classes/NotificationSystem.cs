using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Wave_Player.classes
{
    public class NotificationSystem
    {
        private static readonly Queue<NotificationItem> _notificationQueue = new Queue<NotificationItem>();
        private static bool _isProcessingQueue = false;
        private static Panel _containerPanel;
        private static Window _notificationWindow;
        private static Window _parentWindow;
        private static List<Border> _activeNotifications = new List<Border>();

        public static void Initialize(Window parentWindow)
        {
            _parentWindow = parentWindow;

            _notificationWindow = new Window
            {
                Width = 300,
                SizeToContent = SizeToContent.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize
            };

            _containerPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(23)
            };

            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0),
                Background = Brushes.Transparent
            };

            scrollViewer.Content = _containerPanel;
            _notificationWindow.Content = scrollViewer;

            parentWindow.Loaded += (s, e) =>
            {
                try
                {
                    _notificationWindow.Owner = parentWindow;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error(Owner): {ex.Message}");
                }

                _notificationWindow.Show();
                UpdateNotificationWindowPosition();
            };

            parentWindow.LocationChanged += (s, e) => UpdateNotificationWindowPosition();
            parentWindow.SizeChanged += (s, e) => UpdateNotificationWindowPosition();
            parentWindow.StateChanged += (s, e) => UpdateNotificationWindowPosition();
            parentWindow.Activated += (s, e) =>
            {
                if (_notificationWindow != null && !_notificationWindow.IsVisible)
                    _notificationWindow.Show();
            };

            parentWindow.Closed += (s, e) =>
            {
                if (_notificationWindow != null && _notificationWindow.IsVisible)
                    _notificationWindow.Close();
            };
        }

        private static void UpdateNotificationWindowPosition()
        {
            if (_notificationWindow == null || !_notificationWindow.IsVisible) return;

            try
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                _notificationWindow.Left = screenWidth - _notificationWindow.ActualWidth - 20;
                _notificationWindow.Top = screenHeight - _notificationWindow.ActualHeight - 20;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error(Position): {ex.Message}");
            }
        }

        public static void Show(string message, NotificationType type = NotificationType.Info, int durationMs = 3000)
        {
            if (_notificationWindow == null)
            {
                throw new InvalidOperationException("NotificationSystem must be initialized before use");
            }

            _notificationQueue.Enqueue(new NotificationItem { Message = message, Type = type, Duration = durationMs });

            if (!_isProcessingQueue)
            {
                _isProcessingQueue = true;
                _notificationWindow.Dispatcher.BeginInvoke(new Action(ProcessQueue));
            }
        }

        private static void ProcessQueue()
        {
            if (_notificationQueue.Count == 0)
            {
                _isProcessingQueue = false;
                if (_activeNotifications.Count == 0)
                {
                    UpdateNotificationWindowPosition();
                }
                return;
            }

            var item = _notificationQueue.Dequeue();
            ShowNotification(item.Message, item.Type, item.Duration);
        }

        private static void ShowNotification(string message, NotificationType type, int durationMs)
        {
            if (!_notificationWindow.IsVisible && _parentWindow.IsLoaded)
            {
                _notificationWindow.Show();
            }

            string icon = type switch
            {
                NotificationType.Success => "✓",
                NotificationType.Error => "✕",
                NotificationType.Warning => "⚠",
                _ => "ℹ",
            };

            var primaryColor = Application.Current.Resources["PrimaryColor"];
            var secondaryColor = Application.Current.Resources["SecondaryColor"];

            LinearGradientBrush backgroundBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };

            LinearGradientBrush iconBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };

            if (type == NotificationType.Error)
            {
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromRgb(50, 0, 0), 0));
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromRgb(80, 0, 0), 1));
                iconBrush.GradientStops.Add(new GradientStop(Colors.Red, 0));
                iconBrush.GradientStops.Add(new GradientStop(Colors.DarkRed, 1));
            }
            else if (type == NotificationType.Warning)
            {
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromRgb(50, 40, 0), 0));
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromRgb(80, 60, 0), 1));
                iconBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0));
                iconBrush.GradientStops.Add(new GradientStop(Colors.DarkOrange, 1));
            }
            else
            {
                backgroundBrush.GradientStops.Add(new GradientStop((Color)primaryColor, 0.0));
                backgroundBrush.GradientStops.Add(new GradientStop((Color)secondaryColor, 1.0));
                iconBrush = backgroundBrush.Clone();
            }

            Border notificationBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
                BorderBrush = backgroundBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8), 
                Opacity = 0,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 3,
                    BlurRadius = 8,
                    Opacity = 0.3,
                    Color = Colors.Black
                }
            };

            Grid notificationGrid = new Grid
            {
                Margin = new Thickness(12)
            };

            notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock iconBlock = new TextBlock
            {
                Text = icon,
                FontSize = 18,
                Foreground = iconBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(iconBlock, 0);

            TextBlock messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(messageBlock, 1);

            notificationGrid.Children.Add(iconBlock);
            notificationGrid.Children.Add(messageBlock);

            notificationBorder.Child = notificationGrid;

            _containerPanel.Children.Insert(0, notificationBorder);
            _activeNotifications.Add(notificationBorder);


            _notificationWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateNotificationWindowPosition();
            }), System.Windows.Threading.DispatcherPriority.Render);

            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(durationMs - 300)
            };

            fadeOutAnimation.Completed += (s, e) =>
            {
                _containerPanel.Children.Remove(notificationBorder);
                _activeNotifications.Remove(notificationBorder);

                UpdateNotificationWindowPosition();
                _notificationWindow.Dispatcher.BeginInvoke(new Action(ProcessQueue));
            };

            Storyboard.SetTarget(fadeInAnimation, notificationBorder);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));

            Storyboard.SetTarget(fadeOutAnimation, notificationBorder);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(fadeOutAnimation);
            storyboard.Begin();

            _notificationWindow.Dispatcher.BeginInvoke(new Action(ProcessQueue));
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    internal class NotificationItem
    {
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public int Duration { get; set; }
    }
}
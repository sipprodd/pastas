using System.Windows;
using System.Windows.Media.Animation;

namespace Pastas.Presentation.Notifications;

public partial class ToastNotificationWindow : Window
{
    public ToastNotificationWindow(string title, string subtitle)
    {
        InitializeComponent();
        TitleText.Text = title;
        SubtitleText.Text = subtitle;
        Opacity = 0;
    }

    public void PlayShowAnimation()
    {
        BeginAnimation(OpacityProperty, null);

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            FillBehavior = FillBehavior.Stop
        };

        fade.Completed += (_, _) => Opacity = 1;

        BeginAnimation(OpacityProperty, fade);
    }
}
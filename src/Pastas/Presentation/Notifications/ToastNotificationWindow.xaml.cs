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
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(Window.OpacityProperty));

        storyboard.Completed += (_, _) => Opacity = 1;

        storyboard.Children.Add(fade);
        storyboard.Begin();
    }
}

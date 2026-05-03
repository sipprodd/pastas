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

    protected override bool ShowActivated => false;

    public void PlayShowAnimation(double finalTop)
    {
        var storyboard = new Storyboard();
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(Window.OpacityProperty));

        var slide = new DoubleAnimation(finalTop + 12, finalTop, TimeSpan.FromMilliseconds(180));
        Storyboard.SetTarget(slide, this);
        Storyboard.SetTargetProperty(slide, new PropertyPath(Window.TopProperty));

        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Begin();
    }
}

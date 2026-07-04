namespace FloatingHud;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0)
        {
            try
            {
                HudSettingsStore.ConfigureDirectory(e.Args[0]);
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"无法使用指定的数据目录：{exception.Message}",
                    "Floating HUD",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
        }

        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}

using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(EquipmentBorrowing.UiTests.TestAppBuilder))]

namespace EquipmentBorrowing.UiTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class HeadlessApp : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}


using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;

[assembly: AvaloniaTestApplication(typeof(EquipmentBorrowing.UiTests.TestAppBuilder))]

namespace EquipmentBorrowing.UiTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class HeadlessApp : Avalonia.Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new StyleInclude(new Uri("avares://EquipmentBorrowing.Desktop/"))
        {
            Source = new Uri("avares://EquipmentBorrowing.Desktop/Styles/CampusTheme.axaml")
        });
    }
}


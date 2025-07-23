namespace Setback

open System.Diagnostics
open System.IO

open Elmish

open Avalonia
open Avalonia.Styling
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.Controls.ApplicationLifetimes

type MainWindow() as this =
    inherit HostWindow(
        Title = "Bernsrite Setback",
        Width = 500.0,
        Height = 150.0)
    do
        Program.mkSimple Model.init Message.update View.view
            |> Program.withSubscription Message.subscribe
            |> Program.withHost this
            // |> Program.withConsoleTrace
            |> Program.runWithAvaloniaSyncDispatch ()

type App() =
    inherit Application(
        RequestedThemeVariant = ThemeVariant.Dark)

    override this.Initialize() =
        this.Styles.Add (FluentTheme())

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let mainWindow = MainWindow()
            desktopLifetime.MainWindow <- mainWindow
        | _ -> ()

module Program =

    let configureKSetback () =

        let iniPath = @"C:\Program Files\KSetback\KSetback.ini"
        let oldLines = File.ReadAllLines(iniPath)
        let newLines =
            [
                "Password = Rosie"
                "Master = NS"
            ]
                |> Seq.where (fun newLine ->
                    Array.contains newLine oldLines |> not)
        File.AppendAllLines(iniPath, newLines)

        Process.Start(@"C:\Program Files\KSetback\KSetback.exe")
            |> ignore

    [<EntryPoint>]
    let main(args: string[]) =
        // configureKSetback ()
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)

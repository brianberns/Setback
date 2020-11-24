namespace Setback.Cfrm.PlayGui

open System
open System.Windows.Forms

module Program =

    [<EntryPoint; STAThread>]
    let main argv =
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm())
        0

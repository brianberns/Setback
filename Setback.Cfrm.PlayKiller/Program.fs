namespace Setback

module Program =

    let wrapper = Killer.createWrapper ()
    Killer.play wrapper |> ignore

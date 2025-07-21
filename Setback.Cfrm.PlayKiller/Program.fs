namespace Setback

module Program =

    Killer.handshake ()
    let wrapper = Killer.createWrapper ()
    Killer.play wrapper |> ignore

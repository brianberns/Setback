namespace Setback.Cfrm.PlayGui

open System.Collections.Generic
open System.Windows.Forms

/// Manages a queue of actions on a timer.
type ActionQueue(interval) =

    /// Queue of actions.
    let queue = Queue<unit -> unit>()

    /// Single-threaded timer.
    let timer =
        new Timer(Interval = interval)

    /// Executes the top action in the queue.
    let execute _ =
        if queue.Count > 0 then
            let action = lock queue (fun _ ->
                if queue.Count = 1 then
                    timer.Enabled <- false
                queue.Dequeue())
            action ()

        // initialize
    do
        timer.Tick.Add(execute)
        timer.Start()

    /// Adds the given action to the queue.
    member __.Enqueue(action) =
        lock queue (fun _ ->
            queue.Enqueue(action)
            timer.Enabled <- true)

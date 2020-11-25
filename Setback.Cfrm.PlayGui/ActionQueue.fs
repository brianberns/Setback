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
            let action = queue.Dequeue()
            action ()

        // initialize
    do
        timer.Tick.Add(execute)
        timer.Start()

    /// Adds the given action to the queue.
    member __.Enqueue(action) =
        queue.Enqueue(action)

    /// Indicates whether the queue's timer is running.
    member __.Enabled
        with set(value) = timer.Enabled <- value
        and get() = timer.Enabled

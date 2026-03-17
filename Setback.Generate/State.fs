namespace Setback.Generate

open System

open Setback.Learn
open Setback.Model

/// Advantage state.
type AdvantageState =
    {
        /// Current model, if any.
        ModelOpt : Option<AdvantageModel>

        /// Stored training data.
        SampleStore : AdvantageSampleStore
    }

    /// Cleanup.
    member this.Dispose() =
        this.ModelOpt
            |> Option.iter _.Dispose()
        this.SampleStore.Dispose()

    interface IDisposable with

        /// Cleanup.
        member this.Dispose() =
            this.Dispose()

module AdvantageState =

    /// Creates an advantage state.
    let create modelOpt sampleStore =
        {
            ModelOpt = modelOpt
            SampleStore = sampleStore
        }

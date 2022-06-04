namespace Setback.Cfrm

open System
open System.Reflection

module Random =

    /// Saves the state of a random number generator.
    let save (rng : Random) =
        rng.GetType().GetFields(BindingFlags.NonPublic ||| BindingFlags.Instance)
            |> Array.map (fun field ->
                let value =
                    match field.GetValue(rng) with
                        | :? Array as array -> array.Clone()
                        | value -> value
                field, value)

    /// Restores the state of a random number generator.
    let restore state =
        let rng = Random()
        for (field : FieldInfo, value) in state do
            field.SetValue(rng, value)
        rng

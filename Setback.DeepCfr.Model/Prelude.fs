namespace Setback.DeepCfr.Model

module Vector =

    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.Distributions

    /// Gets an element from a vector.
    let get (vector : Vector<_>) index =
        vector[index]

    /// Samples a vector.
    let inline sample rng (vector : Vector<_>) =
        let vector' =
            vector
                |> Seq.map float
                |> Seq.toArray
        Categorical.Sample(rng, vector')

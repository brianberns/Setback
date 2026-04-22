namespace Setback.Learn

open MathNet.Numerics.LinearAlgebra
open Setback.Model

/// An observed advantage event.
type AdvantageSample =
    {
        /// Encoded info set.
        Encoding : Encoding

        /// Observed regrets.
        Regrets : Vector<float32>

        /// 1-based iteration number.
        Iteration : int
    }

module AdvantageSample =

    /// Creates an advantage sample.
    let create encoding regrets iteration =
        assert(Array.length encoding = Model.inputSize)
        assert(Vector.length regrets = Model.outputSize)
        assert(iteration >= 1)
        {
            Encoding = encoding
            Regrets = regrets
            Iteration = iteration
        }

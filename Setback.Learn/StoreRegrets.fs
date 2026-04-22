namespace Setback.Learn

open System
open System.IO

open MathNet.Numerics.LinearAlgebra

open Setback.Model

module StoreRegrets =

    /// Maximum possible number of actions available in an info
    /// set.
    let private maxActionCount =
        Setback.Setback.numCardsPerHand

    /// Size of one regret entry.
    let private entrySize = sizeof<byte> + sizeof<float32>

    /// Number of bytes in packed regrets.
    let packedSize =
        maxActionCount * entrySize

    /// Writes the given regrets to the given stream. Each entry is a
    /// (byte index, float32 regret) pair; unused slots are padded with
    /// (Byte.MaxValue, 0f) so every record has a fixed size.
    let write (wtr : BinaryWriter) (regrets : Vector<float32>) =

            // write non-zero value pairs directly to the writer
        let mutable nWritten = 0
        for i = 0 to regrets.Count - 1 do
            let regret = regrets[i]
            if regret <> 0f then
                assert(i < int Byte.MaxValue)
                assert(nWritten < maxActionCount)
                wtr.Write(byte i)
                wtr.Write(regret)
                nWritten <- nWritten + 1

            // write padding for unused slots
        for _ = nWritten to maxActionCount - 1 do
            wtr.Write(Byte.MaxValue)
            wtr.Write(0f)

    /// Reads regrets from the given stream.
    let read (rdr : BinaryReader) =
        let bytes = rdr.ReadBytes(packedSize)
        assert(bytes.Length = packedSize)
        seq {
            for j = 0 to maxActionCount - 1 do
                let pos = j * entrySize
                let i = bytes[pos]
                let regret = BitConverter.ToSingle(bytes, pos + 1)
                if regret = 0f then
                    assert(i = Byte.MaxValue)
                else
                    int i, regret
        }
            |> SparseVector.ofSeqi Model.outputSize
            |> CreateVector.DenseOfVector

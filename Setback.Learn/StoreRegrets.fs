namespace Setback.Learn

open System
open System.IO

open Microsoft.Win32.SafeHandles

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

    /// Writes the given regrets to the given file.
    let write handle fileOffset (regrets : Vector<float32>) =

            // get non-zero value pairs
        let pairs =
            [|
                for i, regret in Seq.indexed regrets do
                    if regret <> 0f then
                        i, regret
            |]
        assert(pairs.Length <= maxActionCount)

            // write value pairs with padding
        let buf =
            [|
                    // write value pairs
                for i, regret in pairs do
                    assert(i < int Byte.MaxValue)
                    byte i
                    yield! BitConverter.GetBytes(regret)

                    // write padding
                for _ = pairs.Length to maxActionCount - 1 do
                    Byte.MaxValue
                    yield! BitConverter.GetBytes(0f)
            |]
        RandomAccess.Write(handle, buf, fileOffset)

    /// Reads regrets from the given file.
    let read (handle : SafeFileHandle) (fileOffset : int64) =
        let buf = Array.zeroCreate<byte> packedSize
        let nBytesRead =
            RandomAccess.Read(handle, buf, fileOffset)
        assert(nBytesRead = packedSize)
        seq {
            for j = 0 to maxActionCount - 1 do
                let pos = j * entrySize
                let i = buf[pos]
                let regret = BitConverter.ToSingle(buf, pos + 1)
                if regret = 0f then
                    assert(i = Byte.MaxValue)
                else
                    int i, regret
        }
            |> SparseVector.ofSeqi Model.outputSize
            |> CreateVector.DenseOfVector

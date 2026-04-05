namespace Setback.Learn

open System.IO
open Setback.Model

module StoreEncoding =

    /// Number of bytes in a packed encoding.
    let packedSize =
        (Model.inputSize + 7) / 8   // round up

    /// Writes the given encoding to the given stream.
    let write (wtr : BinaryWriter) (encoding : Encoding) =
        assert(encoding.Length = Model.inputSize)
        let bytes =
            [|
                for chunk in Array.chunkBySize 8 encoding do   // 8 bits/byte
                    (0uy, Array.indexed chunk)
                        ||> Array.fold (fun byte (i, flag) ->
                            if flag then byte ||| (1uy <<< i)
                            else byte)
            |]
        wtr.Write(bytes)

    /// Reads an encoding from the given stream.
    let read (rdr : BinaryReader) : Encoding =
        let bytes = rdr.ReadBytes(packedSize)
        assert(bytes.Length = packedSize)
        bytes
            |> Array.collect (fun byte ->
                Array.init 8 (fun i ->      // 8 bits/byte
                    (byte &&& (1uy <<< i)) <> 0uy))
            |> Array.take Model.inputSize   // trim padding

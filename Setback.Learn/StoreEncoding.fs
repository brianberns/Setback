namespace Setback.Learn

open System.IO
open Setback.Model

module StoreEncoding =

    /// Number of bytes in a packed encoding.
    let packedSize =
        (Model.inputSize + 7) / 8   // round up

    (*
     * Packing layout: flag j is stored as bit (j mod 8) of byte (j / 8).
     * We use `j >>> 3` for the byte index (j / 8) and `j &&& 7` for the
     * bit position within that byte (j mod 8). `1uy <<< (j &&& 7)` is
     * then a single-bit mask selecting flag j's bit.
     *)

    /// Writes the given encoding to the given stream.
    let write (wtr : BinaryWriter) (encoding : Encoding) =
        assert(encoding.Length = Model.inputSize)
        let bytes = Array.zeroCreate<byte> packedSize
        for j = 0 to encoding.Length - 1 do
            if encoding[j] then
                bytes[j >>> 3] <-   // set flag j's bit in its byte
                    bytes[j >>> 3] ||| (1uy <<< (j &&& 7))
        wtr.Write(bytes)

    /// Reads an encoding from the given stream.
    let read (rdr : BinaryReader) : Encoding =
        let bytes = rdr.ReadBytes(packedSize)
        assert(bytes.Length = packedSize)
        let flags = Array.zeroCreate<bool> Model.inputSize
        for j = 0 to Model.inputSize - 1 do
            flags[j] <-   // flag j is set iff its bit is 1 in the packed byte
                (bytes[j >>> 3] &&& (1uy <<< (j &&& 7))) <> 0uy
        flags

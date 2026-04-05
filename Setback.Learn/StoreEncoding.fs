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
     * bit position within that byte (j mod 8).
     *)

    /// Mask selecting flag's bit.
    let inline private mask iFlag =
        1uy <<< (iFlag &&& 7)

    /// Gets the byte index of the given flag.
    let inline private getByteIndex iFlag =
        iFlag >>> 3

    /// Writes the given encoding to the given stream.
    let write (wtr : BinaryWriter) (encoding : Encoding) =
        assert(encoding.Length = Model.inputSize)
        let bytes = Array.zeroCreate<byte> packedSize
        for iFlag = 0 to encoding.Length - 1 do
            if encoding[iFlag] then
                let iByte = getByteIndex iFlag
                bytes[iByte] <- bytes[iByte] ||| mask iFlag   // set flag's bit in its byte
        wtr.Write(bytes)

    /// Reads an encoding from the given stream.
    let read (rdr : BinaryReader) : Encoding =
        let bytes = rdr.ReadBytes(packedSize)
        assert(bytes.Length = packedSize)
        let flags = Array.zeroCreate<bool> Model.inputSize
        for iFlag = 0 to Model.inputSize - 1 do
            let iByte = getByteIndex iFlag
            flags[iFlag] <- (bytes[iByte] &&& mask iFlag) <> 0uy   // flag is set iff its bit is 1 in the packed byte
        flags

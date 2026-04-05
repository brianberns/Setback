namespace Setback.Learn

open System.IO

/// Data store header type.
type StoreHeaderType =
    {
        /// File format identifier.
        Magic : byte[]
    }

module StoreHeaderType =

    /// Number of bytes in a packed header.
    let getPackedSize headerType =
        (headerType.Magic.Length * sizeof<byte>)   // file format ID
            + sizeof<int32>                        // iteration #

/// Data store header.
type StoreHeader =
    {
        /// Header type.
        Type : StoreHeaderType

        /// Iteration number.
        Iteration : int32
    }

module StoreHeader =

    /// Creates a store header.
    let create headerType iteration =
        {
            Type = headerType
            Iteration = iteration
        }

    /// Writes a header to the given stream.
    let write (wtr : BinaryWriter) header =
        assert(wtr.BaseStream.Length = 0L)

            // write magic string
        wtr.Write(header.Type.Magic)

            // write iteration number
        assert(header.Iteration >= 0)
        wtr.Write(header.Iteration)

    /// Reads a header from the given stream.
    let read (rdr : BinaryReader) headerType =

            // read magic string
        let magic' = rdr.ReadBytes(headerType.Magic.Length)
        assert(magic'.Length = headerType.Magic.Length)
        if magic' <> headerType.Magic then
            failwith $"Invalid magic bytes: {magic'}"

            // read iteration number
        let iter = rdr.ReadInt32()
        assert(iter >= 0)

        create headerType iter

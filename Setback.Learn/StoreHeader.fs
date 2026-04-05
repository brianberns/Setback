namespace Setback.Learn

open System.IO

/// Data store header.
type StoreHeader =
    {
        /// Iteration number.
        Iteration : int32
    }

module StoreHeader =

    /// File format identifier.
    let private magic = "Stbk"B

    /// Number of bytes in a packed header.
    let packedSize =
        (magic.Length * sizeof<byte>)   // file format ID
            + sizeof<int32>             // iteration #

    /// Creates a store header.
    let create iteration =
        {
            Iteration = iteration
        }

    /// Writes a header to the given stream.
    let write (wtr : BinaryWriter) header =
        assert(wtr.BaseStream.Length = 0L)

            // write magic string
        wtr.Write(magic)

            // write iteration number
        assert(header.Iteration >= 0)
        wtr.Write(header.Iteration)

    /// Reads a header from the given stream.
    let read (rdr : BinaryReader) =

            // read magic string
        let magic' = rdr.ReadBytes(magic.Length)
        assert(magic'.Length = magic.Length)
        if magic' <> magic then
            failwith $"Invalid magic bytes: {magic'}"

            // read iteration number
        let iter = rdr.ReadInt32()
        assert(iter >= 0)

        create iter

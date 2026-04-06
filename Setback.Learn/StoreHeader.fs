namespace Setback.Learn

open System.IO

module StoreIteration =

    /// Writes an iteration number.
    let write (wtr : BinaryWriter) (iteration : int32) =
        assert(iteration > 0)
        wtr.Write(iteration)

    /// Reads an iteration number.
    let read (rdr : BinaryReader) =
        let iteration = rdr.ReadInt32()
        assert(iteration > 0)
        iteration

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
        wtr.Write(magic)
        StoreIteration.write wtr header.Iteration

    /// Reads a header from the given stream.
    let read (rdr : BinaryReader) =

            // read and validate format
        let magic' = rdr.ReadBytes(magic.Length)
        assert(magic'.Length = magic.Length)
        if magic' <> magic then
            failwith $"Invalid file format: Expected %A{magic} but read %A{magic'}"

        let iter = StoreIteration.read rdr
        create iter

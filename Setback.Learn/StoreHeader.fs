namespace Setback.Learn

open System
open System.IO

open Microsoft.Win32.SafeHandles

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

    /// Writes a header to the given file.
    let write handle header =
        assert(RandomAccess.GetLength(handle) = 0L)

            // write magic string
        RandomAccess.Write(handle, header.Type.Magic, 0L)

            // write iteration number
        assert(header.Iteration >= 0)
        let buf = BitConverter.GetBytes(header.Iteration)
        RandomAccess.Write(handle, buf, int64 header.Type.Magic.Length)

    /// Reads a header from the given file.
    let read (handle : SafeFileHandle) headerType =

            // read magic string
        let magic' = Array.zeroCreate<byte> headerType.Magic.Length
        let nBytesRead = RandomAccess.Read(handle, magic', 0L)
        assert(nBytesRead = headerType.Magic.Length)
        if magic' <> headerType.Magic then
            failwith $"Invalid magic bytes: {magic'}"

            // read iteration number
        let buf = Array.zeroCreate<byte> sizeof<int32>
        let nBytesRead =
            RandomAccess.Read(handle, buf, int64 headerType.Magic.Length)
        assert(nBytesRead = sizeof<int32>)
        let iter = BitConverter.ToInt32(buf, 0)
        assert(iter >= 0)

        create headerType iter

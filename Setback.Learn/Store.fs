namespace Setback.Learn

open System
open System.IO

open Microsoft.Win32.SafeHandles

/// Persistent store for advantage samples.
type AdvantageSampleStore =
    {
        /// Underlying file handle.
        Handle : SafeFileHandle

        /// Path of the underlying file.
        Path : string

        /// Iteration for all samples in this store.
        Iteration : int
    }

    /// Cleanup.
    member this.Dispose() =
        this.Handle.Dispose()

    /// Cleanup.
    interface IDisposable with
        member this.Dispose() =
            this.Dispose()

module AdvantageSampleStore =

    module private Header =

        /// Store header type.
        let headerType = { Magic = "Stbk"B }

        /// Number of bytes in a packed header.
        let packedSize =
            StoreHeaderType.getPackedSize headerType

    /// Number of bytes in a serialized sample.
    let private packedSampleSize =
        StoreEncoding.packedSize + StoreRegrets.packedSize

    /// Is the given store in a valid state?
    let private isValid store =
        (RandomAccess.GetLength(store.Handle)
            - int64 Header.packedSize)
                % int64 packedSampleSize = 0L

    /// Creates a new sample store at the given location.
    let create iteration path =

            // open handle
        let handle =
            File.OpenHandle(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read)

            // build store
        let store =
            {
                Handle = handle
                Path = path
                Iteration = iteration
            }
        StoreHeader.create Header.headerType iteration
            |> StoreHeader.write handle
        assert(isValid store)
        store

    /// Opens an existing sample store at the given location.
    let openRead path =

            // open handle
        let handle =
            File.OpenHandle(
                path,
                FileMode.Open,
                FileAccess.Read)

            // build store
        let header =
            StoreHeader.read handle Header.headerType
        let store =
            {
                Handle = handle
                Path = path
                Iteration = header.Iteration
            }
        assert(isValid store)
        store

    /// Gets the number of samples in the given store.
    let getSampleCount store =
        assert(isValid store)
        (RandomAccess.GetLength(store.Handle)
            - int64 Header.packedSize)
                / int64 packedSampleSize

    /// Reads the sample at the given index in the given store.
    let readSample (idx : int64) store =
        assert(isValid store)
        assert(idx >= 0)
        assert(idx < getSampleCount store)

        let fileOffset =
            int64 Header.packedSize
                + idx * int64 packedSampleSize
        let encoding =
            StoreEncoding.read store.Handle fileOffset
        let regrets =
            StoreRegrets.read store.Handle
                (fileOffset + int64 StoreEncoding.packedSize)
        AdvantageSample.create encoding regrets store.Iteration

    /// Appends the given samples to the end of the given store.
    let appendSamples samples store =
        assert(isValid store)

        let baseOffset = RandomAccess.GetLength(store.Handle)
        for i, sample in Seq.indexed samples do
            let fileOffset =
                baseOffset + int64 i * int64 packedSampleSize
            StoreEncoding.write store.Handle fileOffset sample.Encoding
            StoreRegrets.write store.Handle
                (fileOffset + int64 StoreEncoding.packedSize)
                sample.Regrets

        assert(isValid store)

type AdvantageSampleStore with

    /// The number of samples in this store.
    member store.Count =
        AdvantageSampleStore.getSampleCount store

    /// Gets the sample at the given index in this store.
    member store.Item
        with get(idx) =
            AdvantageSampleStore.readSample idx store

namespace Setback.Learn

open System
open System.IO

open Microsoft.Win32.SafeHandles

/// Persistent store for advantage samples.
type AdvantageSampleStore =
    {
        /// Underlying file stream.
        Stream : FileStream

        /// Iteration for all samples in this store.
        Iteration : int
    }

    /// Path of the underlying file.
    member this.Path =
        this.Stream.Name

    /// Cleanup.
    member this.Dispose() =
        this.Stream.Dispose()

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
        (store.Stream.Length - int64 Header.packedSize)
            % int64 packedSampleSize = 0L

    /// Creates a writer for the given stream.
    let private createWriter stream =
        new BinaryWriter(
            stream,
            Text.Encoding.Default,
            leaveOpen = true)

    /// Creates a reader for the given stream.
    let private createReader stream =
        new BinaryReader(
            stream,
            Text.Encoding.Default,
            leaveOpen = true)

    /// Creates a new sample store at the given location.
    let create iteration path =

            // open stream
        let stream =
            new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read)

            // write header
        let store =
            {
                Stream = stream
                Iteration = iteration
            }
        use wtr = createWriter store.Stream
        StoreHeader.create Header.headerType iteration
            |> StoreHeader.write wtr
        assert(isValid store)
        store

    /// Opens an existing sample store at the given location.
    let openRead path =

            // open handle
        let stream =
            new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read)

            // read header
        use rdr = createReader stream
        let header =
            StoreHeader.read rdr Header.headerType
        let store =
            {
                Stream = stream
                Iteration = header.Iteration
            }
        assert(isValid store)
        store

    /// Gets the number of samples in the given store.
    let getSampleCount store =
        assert(isValid store)
        (store.Stream.Length - int64 Header.packedSize)
            / int64 packedSampleSize

    /// Reads all samples in the given store.
    let readSamples store =
        assert(isValid store)
        use rdr = createReader store.Stream
        seq {
            assert(store.Stream.Position <= store.Stream.Length)
            while store.Stream.Position < store.Stream.Length do
                let encoding = StoreEncoding.read rdr
                let regrets = StoreRegrets.read rdr
                AdvantageSample.create encoding regrets store.Iteration
        }

    /// Writes the given samples to the given store.
    let writeSamples samples store =
        assert(isValid store)
        use wtr = createWriter store.Stream
        for sample in samples do
            StoreEncoding.write wtr sample.Encoding
            StoreRegrets.write wtr sample.Regrets
        assert(isValid store)

type AdvantageSampleStore with

    /// The number of samples in this store.
    member store.Count =
        AdvantageSampleStore.getSampleCount store

namespace Setback.Learn

open System
open System.IO

open Microsoft.Win32.SafeHandles

/// Persistent store for advantage samples.
type AdvantageSampleStore =
    {
        /// Underlying file stream.
        Stream : FileStream

        /// Buffered access to file stream.
        Access : Choice<BinaryWriter, BinaryReader>

        /// Iteration for all samples in this store.
        Iteration : int
    }

    /// Path of the underlying file.
    member this.Path =
        this.Stream.Name

    /// Cleanup.
    member this.Dispose() =
        match this.Access with
            | Choice1Of2 wtr -> wtr.Dispose()
            | Choice2Of2 rdr -> rdr.Dispose()
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

    /// Creates a new sample store at the given location.
    let create iteration path =

            // open stream
        let stream =
            new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                bufferSize = (1 <<< 20),   // 1 MB
                options = FileOptions.None)
        let wtr = new BinaryWriter(stream)

            // create store
        let store =
            {
                Stream = stream
                Access = Choice1Of2 wtr
                Iteration = iteration
            }

            // write header
        StoreHeader.create Header.headerType iteration
            |> StoreHeader.write wtr
        assert(isValid store)

        store

    /// Opens an existing sample store at the given location.
    let openRead path =

            // open stream
        let stream =
            new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize = (1 <<< 20),   // 1 MB
                options = FileOptions.SequentialScan)
        let rdr = new BinaryReader(stream)

            // read header
        let header =
            StoreHeader.read rdr Header.headerType

            // create store
        let store =
            {
                Stream = stream
                Access = Choice2Of2 rdr
                Iteration = header.Iteration
            }
        assert(isValid store)
        store

    /// Gets the number of samples in the given store.
    let getSampleCount store =
        assert(isValid store)
        (store.Stream.Length - int64 Header.packedSize)
            / int64 packedSampleSize

    /// Writes the given samples to the given store.
    let writeSamples samples store =
        assert(isValid store)
        match store.Access with
            | Choice1Of2 wtr ->
                for sample in samples do
                    StoreEncoding.write wtr sample.Encoding
                    StoreRegrets.write wtr sample.Regrets
                assert(isValid store)
            | _ -> failwith "Invalid access"

    /// Reads all samples in the given store.
    let readSamples store =
        assert(isValid store)
        match store.Access with
            | Choice2Of2 rdr ->
                seq {
                    assert(store.Stream.Position <= store.Stream.Length)
                    while store.Stream.Position < store.Stream.Length do
                        let encoding = StoreEncoding.read rdr
                        let regrets = StoreRegrets.read rdr
                        AdvantageSample.create encoding regrets store.Iteration
                }
            | _ -> failwith "Invalid access"

type AdvantageSampleStore with

    /// The number of samples in this store.
    member store.Count =
        AdvantageSampleStore.getSampleCount store

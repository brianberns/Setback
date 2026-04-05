namespace Setback.Learn

open System
open System.IO

open Microsoft.Win32.SafeHandles

/// Persistent store for suffled advantage samples.
type AdvantageSampleShuffledStore =
    {
        /// Underlying file stream.
        Stream : FileStream

        /// Maximum iteration of samples in this store.
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

module AdvantageSampleShuffledStore =

    module private Header =

        /// Store header type.
        let headerType = { Magic = "StbS"B }

        /// Number of bytes in a packed header.
        let packedSize =
            StoreHeaderType.getPackedSize headerType

    /// Number of bytes in a serialized sample.
    let private packedSampleSize =
        StoreEncoding.packedSize        // encoding
            + StoreRegrets.packedSize   // regrets
            + sizeof<int32>             // iteration

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

    /// Creates a new shuffled store at the given location.
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

    /// Writes an iteration number.
    let writeIteration handle fileOffset (iteration : int32) =
        assert(iteration > 0)
        let buf = BitConverter.GetBytes(iteration)
        RandomAccess.Write(handle, buf, fileOffset)

    /// Reads an iteration number.
    let readIteration (handle : SafeFileHandle) (fileOffset : int64) =
        let buf = Array.zeroCreate<byte> sizeof<int32>
        let nBytesRead =
            RandomAccess.Read(handle, buf, fileOffset)
        assert(nBytesRead = sizeof<int32>)
        let iteration = BitConverter.ToInt32(buf)
        assert(iteration > 0)
        iteration

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
        let iteration =
            readIteration store.Handle
                (fileOffset
                    + int64 StoreEncoding.packedSize
                    + int64 StoreRegrets.packedSize)
        assert(iteration <= store.Iteration)
        AdvantageSample.create encoding regrets iteration

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
            assert(sample.Iteration <= store.Iteration)
            writeIteration store.Handle
                (fileOffset
                    + int64 StoreEncoding.packedSize
                    + int64 StoreRegrets.packedSize)
                sample.Iteration

        assert(isValid store)

type AdvantageSampleShuffledStore with

    /// The number of samples in this store.
    member store.Count =
        AdvantageSampleShuffledStore.getSampleCount store

    /// Gets the sample at the given index in this store.
    member store.Item
        with get(idx) =
            AdvantageSampleShuffledStore.readSample idx store

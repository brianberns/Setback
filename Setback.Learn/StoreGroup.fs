namespace Setback.Learn

/// A group of sample stores.
type AdvantageSampleStoreGroup =
    {
        /// Stores in this group.
        Stores : AdvantageSampleStore[]
    }

    /// Number of stores in this group.
    member this.Count =
        this.Stores.Length

    /// Store indexer.
    member this.Item(iStore) =
        this.Stores[iStore]

    /// Highest iteration represented by this group.
    member this.Iteration =
        this.Stores
            |> Seq.map _.Iteration
            |> Seq.max

    /// Number of samples in this group.
    member this.NumSamples =
        this.Stores
            |> Seq.sumBy _.Count

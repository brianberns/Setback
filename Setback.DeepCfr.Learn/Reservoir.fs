namespace Setback.DeepCfr.Learn

open System

/// Reservoir sampler.
/// https://en.wikipedia.org/wiki/Reservoir_sampling
type Reservoir<'t> =
    private {

        /// Random number generator.
        Random : Random

        /// Maximum number of items this reservoir can hold.
        Capacity : int

        /// Items stored in this reservoir.
        ItemMap : Map<int, 't>

        /// Number of items this reservoir currently holds.
        Count : int

        /// Number of items this reservoir has seen.
        NumSeen : int
    }

    /// Items held by this reservoir.
    member this.Items = this.ItemMap.Values

module Reservoir =

    /// Creates an empty reservoir.
    let create rng capacity =
        assert(capacity > 0)
        {
            Random = rng
            Capacity = capacity
            ItemMap = Map.empty
            Count = 0
            NumSeen = 0
        }

    /// Attempts to add the given item to the given reservoir,
    /// possibly replacing an existing item.
    let add item reservoir =
        assert(reservoir.Count = reservoir.ItemMap.Count)
        assert(reservoir.Count <= reservoir.Capacity)
        assert(reservoir.Count <= reservoir.NumSeen)

        let numSeen = reservoir.NumSeen + 1
        let idxOpt, count =
            if reservoir.Count < reservoir.Capacity then
                assert(reservoir.Count = reservoir.NumSeen)
                Some reservoir.Count, numSeen
            else
                let idx = reservoir.Random.Next(numSeen)
                if idx < reservoir.Capacity then
                    Some idx, reservoir.Count
                else None, reservoir.Count
        assert(count <= reservoir.Capacity)
        assert(count <= numSeen)

        let itemMap =
            idxOpt
                |> Option.map (fun idx ->
                    Map.add idx item reservoir.ItemMap)
                |> Option.defaultValue reservoir.ItemMap
        {
            reservoir with
                ItemMap = itemMap
                Count = count
                NumSeen = numSeen
        }

    /// Attempts to add the given items to the given reservoir,
    /// possibly replacing existing items.
    let addMany items reservoir =
        (reservoir, items)
            ||> Seq.fold (fun reservoir item ->
                add item reservoir)

namespace Setback.Web

open Setback

type ISetbackApi =
    {
        /// Chooses an action for the given info set.
        GetActionIndex : InformationSet -> Async<int> (*action index*)

        /// Gets the strategy for the given info set.
        GetStrategy : InformationSet -> Async<float[]> (*action probabilities*)
    }

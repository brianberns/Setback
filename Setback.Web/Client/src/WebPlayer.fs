namespace Setback.Web.Client

module Remoting =

    open Fable.Remoting.Client
    open Browser.Dom
    open Setback.Web

    /// Prefix routes with /Setback.
    let routeBuilder typeName methodName = 
        sprintf "/Setback/%s/%s" typeName methodName

    /// Server API.
    let api =
        Remoting.createApi()
            |> Remoting.withRouteBuilder routeBuilder
            |> Remoting.buildProxy<ISetbackApi>

    /// Chooses an action for the given info set.
    let getActionIndex infoSet =
        async {
            match! Async.Catch(api.GetActionIndex infoSet) with
                | Choice1Of2 index -> return index
                | Choice2Of2 exn ->
                    failwith exn.Message   // is there a better way to handle this?
                    return -1
        }

    /// Gets the strategy for the given info set.
    let getStrategy infoSet =
        async {
            match! Async.Catch(api.GetStrategy infoSet) with
                | Choice1Of2 strategy -> return strategy
                | Choice2Of2 exn ->
                    failwith exn.Message   // is there a better way to handle this?
                    return Array.empty
        }

/// Plays Setback by calling a remote server.
module WebPlayer =

    open Setback

    /// Takes an action in the given game.
    let takeAction game =

            // get legal actions in this situation
        let infoSet = Game.currentInfoSet game
        let legalActions = infoSet.LegalActions

            // choose play
        match legalActions.Length with
            | 0 -> failwith "Unexpected"
            | 1 -> async { return legalActions[0] }
            | _ ->
                async {
                    let! iAction =
                        Remoting.getActionIndex infoSet
                    return legalActions[iAction]
                }

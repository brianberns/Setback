namespace Setback.Cfrm

/// An action that applies during playout of a deal.
type PlayAction =

    /// Leading a card on a new trick.
    | Lead of LeadAction

    /// Playing a card on a trick that's already started.
    | Follow of FollowAction

module PlayAction =

    /// Actions available in the given situation.
    let getActions hand handLowTrumpRankOpt playout =
        if playout.CurrentTrick.NumPlays = 0 then
            playout
                |> LeadAction.getActions hand handLowTrumpRankOpt
                |> Array.map Lead
        else
            playout
                |> FollowAction.getActions hand handLowTrumpRankOpt
                |> Array.map Follow

    /// Finds a card to play corresponding to the given action in
    /// the given situation.
    let getPlay hand handLowTrumpRankOpt playout = function
        | Lead action ->
            LeadAction.getPlay hand handLowTrumpRankOpt playout action
        | Follow action ->
            FollowAction.getPlay hand handLowTrumpRankOpt playout action

    /// String representation of a play action.
    let layout : SpanLayout<char> =
        assert(LeadAction.layout.Length = FollowAction.layout.Length)
        SpanLayout.ofLength LeadAction.layout.Length

    /// String representation of a play action.
    let copyTo span = function
        | Lead action -> LeadAction.copyTo span action
        | Follow action -> FollowAction.copyTo span action

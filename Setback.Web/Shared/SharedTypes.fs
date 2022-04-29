namespace Setback.Web.Shared

type ISetbackApi =
    {
        GetActionIndex : string (*key*) -> Async<Option<int>> (*action index*)
    }

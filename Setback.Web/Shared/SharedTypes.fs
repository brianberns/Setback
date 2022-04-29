namespace Setback.Web

type ISetbackApi =
    {
        GetActionIndex : string (*key*) -> Async<Option<int>> (*action index*)
    }

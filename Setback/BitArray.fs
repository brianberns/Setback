namespace Setback

type BitArray =
    {
        Value : uint64
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BitArray =

    let private rotate index =
#if DEBUG
        if index < 0 || index > 63 then
            failwith "Index out of range"
#endif
        1UL <<< index

    let empty =
        { Value = 0UL }

    let getFlag index bitArray =
        let rotated = rotate index
        bitArray.Value &&& rotated = rotated

    let setFlag index flag bitArray =
        let rotated = rotate index
        let value =
            if flag then
                bitArray.Value ||| rotated
            else
                bitArray.Value ^^^ rotated
        { Value = value }

[<AutoOpen>]
module BitArrayExt =
    type BitArray with
        member bitArray.GetFlag index = bitArray |> BitArray.getFlag index
        member bitArray.SetFlag index value = bitArray |> BitArray.setFlag index value

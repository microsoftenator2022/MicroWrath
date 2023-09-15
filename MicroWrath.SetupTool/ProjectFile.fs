[<RequireQualifiedAccess>]
module ProjectFile

open System.Xml.Linq
open Resources

let getTemplate() =
    assemblyResources["csproj.template"]
    |> XDocument.Parse

type ElementValue =
| Children of XElement seq
| Value of string
| Empty

let element (name : string) (attributes : (string * string) seq option) (value : ElementValue) =
    let element = XElement(name)

    match attributes |> Option.toList |> List.collect Seq.toList with
    | [] -> ()
    | values ->
        for key, value in values do
            element.SetAttributeValue(key, value)

    match value with
    | Value s -> element.SetValue s
    | Children children -> element.Add(children |> Seq.toArray)
    | Empty -> ()

    element

let itemGroup children = element "ItemGroup" None (Children children)

let create imports =
    let xml = getTemplate()
    let root = xml.Root

    for name in imports |> Seq.rev do
        let element = element "Import" (Some (seq { yield "Project", name })) Empty
        root.AddFirst(element)

    xml

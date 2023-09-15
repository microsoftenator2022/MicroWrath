open System.Xml.Linq
open System.Reflection
open System.IO
open System.Diagnostics

let getElementByPath (path : string list) (element : XElement) =
    let rec getElementByPath (path : string list) (element : XElement) : XElement option =
        match path with
        | [single] when single = element.Name.LocalName -> Some element
        | head :: tail when head = element.Name.LocalName ->
            element.Elements()
            |> Seq.tryPick (fun e -> getElementByPath tail e)
        | _ -> None

    element.Elements()
    |> Seq.tryPick (fun e -> getElementByPath path e)

let getProperty propertyName root = getElementByPath [ "PropertyGroup"; propertyName ] root

let setOrAddProperty propertyName value root =
    match getProperty propertyName root with
    | Some e when e.Value = value -> e
    | Some e ->
        printfn "Setting %s to '%s'" propertyName value
        e.SetValue(value)
        e
    | None ->
        let xe = XElement(propertyName)
        xe.SetValue(value)

        printfn "Adding %s = %s" xe.Name.LocalName xe.Value
    
        let xe =
            root.Elements()
            |> Seq.tryFind (fun e -> e.Name.LocalName = "PropertyGroup")
            |> Option.map (fun e ->
                e.Add(xe)
                xe)

        match xe with
        | Some e -> e
        | None -> failwith "Could not find a PropertyGroup element"

let infoJson projectName =
    $"""{{
  "Id": "{projectName}",
  "Version": "1.0.0.0",
  "DisplayName": "{projectName}",
  "AssemblyName": "MicroWrath.Loader.dll",
  "EntryMethod": "MicroWrath.MicroMod.Load"  
}}
"""

open Resources

[<EntryPoint>]
let main args =
    if args.Length < 1 then
        failwith "No path specified"

    let projectDir = args[0]
    
    let directory =
        if projectDir |> Directory.Exists |> not then
            printfn "Creating project directory '%s'" projectDir
            Directory.CreateDirectory projectDir
        else DirectoryInfo(projectDir)

    let propsDir = Path.Join(projectDir, "build")

    if propsDir |> Directory.Exists |> not then
        printfn "Creating directory '%s'" propsDir
        Directory.CreateDirectory propsDir |> ignore
        
    let projectName = directory.Name

    let imports =
        assemblyResources.Keys
        |> Seq.where (fun key -> key.EndsWith ".props" || key.EndsWith ".targets")
        |> Seq.map (fun key -> key, assemblyResources[key])

    let projectXml = imports |> Seq.map (fun (key, _) -> $@".\build\{key}") |> ProjectFile.create

    for (name, contents) in imports do
        let path = Path.Join(propsDir, name)
        printfn "Creating '%s'" path
        File.WriteAllText(path, contents)

    let projectFilePath = Path.Join(projectDir, $"{projectName}.csproj")

    printfn "Creating project file"
    File.WriteAllText(projectFilePath, projectXml.ToString())

    printfn "Creating guids.json"
    File.WriteAllText(Path.Join(projectDir, "guids.json"), "{}")

    printfn "Creating info.json"
    File.WriteAllText(Path.Join(projectDir, "info.json"), infoJson (projectName))

    use proc = Process.Start("dotnet", $@"add {projectFilePath} package MicroWrath -s C:\Users\Aaron\nuget.local --prerelease")
    proc.WaitForExit() |> ignore
    use proc = Process.Start("dotnet", $"restore {projectFilePath}")
    proc.WaitForExit() |> ignore
    use proc = Process.Start("dotnet", $@"clean {projectFilePath}")
    proc.WaitForExit() |> ignore

    0
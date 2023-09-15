module Resources

open System.Reflection
open System.IO

let assemblyResources =
    let ass = Assembly.GetExecutingAssembly()
    let assName = ass.GetName().Name

    ass.GetManifestResourceNames()
    |> Seq.where (fun n -> n.StartsWith(assName))
    |> Seq.map (fun n ->
        use s = ass.GetManifestResourceStream(n)
        use stream = new StreamReader(s)
        n.Remove(0, assName.Length + 1), stream.ReadToEnd())
    |> Map.ofSeq

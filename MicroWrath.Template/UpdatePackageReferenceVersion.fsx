open System.IO
open System.Text.RegularExpressions

let newVersion =
    let arg = fsi.CommandLineArgs[1]
    Regex.Replace(arg, @"((?:\d\.?)+)(\-(?:prerelease|debug))\-(?:(?:\w+\-?)+)", "$1$2-*")

Path.GetFullPath "." |> printfn "Looking in %s"

let templates =
    Directory.GetDirectories("content")
    |> Seq.collect (fun d -> Directory.EnumerateFiles(d, "*.csproj", SearchOption.AllDirectories))

let packageReferenceRegex = Regex(@"(<PackageReference\s+Include=""MicroWrath""\s+Version="")[\w\d\.\-\*]+?(""\s*/>)")

let replaceVersion original = packageReferenceRegex.Replace(original, $"${{1}}{newVersion}${{2}}")
    

for path in templates do
    printfn "Project file %s" path

    let str = File.ReadAllText path

    File.WriteAllText(path, replaceVersion str)
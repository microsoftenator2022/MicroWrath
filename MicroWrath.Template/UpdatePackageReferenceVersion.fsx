open System.IO
open System.Text.RegularExpressions

Path.GetFullPath "." |> printfn "Looking in %s"

let templates =
    Directory.GetDirectories("content")
    |> Seq.collect (fun d -> Directory.EnumerateFiles(d, "*.csproj", SearchOption.AllDirectories))

let packageReferenceRegex = Regex(@"(<PackageReference\s+Include=""MicroWrath""\s+Version="")(?:(?:[\w\d\.\-\*]+?)|\{Version\})?(""\s*/>)")

let newVersion =
    let versionRegex = Regex(@"((?:\d\.?)+)(?:(\-(?:prerelease|debug))\-(?:(?:\w+\-?)+))?")

    let arg = fsi.CommandLineArgs[1]

    let m = versionRegex.Match(arg)
    let num = m.Groups[1]
    let buildType = m.Groups[2]

    if buildType.Success then
        $"{num.Value}{buildType.Value}-*"
    else
        num.Value

    //Regex.Replace(arg, @"((?:\d\.?)+)(\-(?:prerelease|debug))\-(?:(?:\w+\-?)+)", "$1$2-*")

let replaceVersion original = packageReferenceRegex.Replace(original, $"${{1}}{newVersion}${{2}}")

for path in templates do
    printfn "Project file %s" path

    let str = File.ReadAllText path

    File.WriteAllText(path, replaceVersion str)
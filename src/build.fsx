#r "packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open System.Text.RegularExpressions
open Fake
open Fake.ConfigurationHelper
open Fake.ReleaseNotesHelper
open Fake.StrongNamingHelper

setBuildParam "MSBuild" @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\msbuild.exe"

let outDir = "./../out"
let testDir = outDir @@ "test"
let buildDir = outDir @@ "build"
let releaseDir = outDir @@ "release"
let strongFile = "../LZ4.snk"
let secretFile = "../passwords.user"

let releaseNotes = "../CHANGES.md" |> LoadReleaseNotes

let testFile fn = (fileInfo fn).Exists
let getSecret key defaultValue =
    let result =
        match testFile secretFile with
        | false -> defaultValue
        | _ ->
            try
                let xml = readConfig secretFile
                let xpath = sprintf "/secret/%s" key
                let node = xml.SelectSingleNode(xpath)
                node.InnerText |> Some
            with _ -> defaultValue
    match result with
    | None -> failwithf "Secret value '%s' is required" key
    | Some x -> x

let assemblyVersionRxDef =
    [
        """(?<=^\s*\[assembly:\s*AssemblyVersion(Attribute)?\(")[0-9]+(\.([0-9]+|\*)){1,3}(?="\))""", false, false
        """(?<=^\s*PRODUCTVERSION\s+)[0-9]+(\,([0-9]+|\*)){1,3}(?=\s*$)""", false, true
        """(?<=^\s*VALUE\s+"ProductVersion",\s*")[0-9]+(\.([0-9]+|\*)){1,3}(?="\s*$)""", false, false
        """(?<=^\s*\[assembly:\s*AssemblyFileVersion(Attribute)?\(")[0-9]+(\.([0-9]+|\*)){1,3}(?="\))""", true, false
        """(?<=^\s*FILEVERSION\s+)[0-9]+(\,([0-9]+|\*)){1,3}(?=\s*$)""", true, true
        """(?<=^\s*VALUE\s+"FileVersion",\s*")[0-9]+(\.([0-9]+|\*)){1,3}(?="\s*$)""", true, false
    ] |> List.map (fun (rx, p, c) -> (Regex(rx, RegexOptions.Multiline), p, c))

let updateVersionInfo productVersion (version: string) fileName =
    let fixVersion commas =
        match commas with | true -> version.Replace(".", ",") | _ -> version

    let allRx =
        assemblyVersionRxDef
        |> Seq.filter (fun (_, p, _) -> p || productVersion)
        |> Seq.map (fun (rx, _, c) -> (rx, c))
        |> List.ofSeq

    let source = File.ReadAllText(fileName)
    let replace s (rx: Regex, c) = rx.Replace(s, fixVersion c)
    let target = allRx |> Seq.fold replace source
    if source <> target then
        trace (sprintf "Updating: %s" fileName)
        File.WriteAllText(fileName, target)

Target "KeyGen" (fun _ ->
    match testFile strongFile with
    | true -> ()
    | _ -> strongFile |> sprintf "-k %s" |> StrongName id
)

Target "Clean" (fun _ ->
    !! "**/bin" ++ "**/obj" |> CleanDirs
    "./../out" |> DeleteDir
)

Target "Build" (fun _ ->
    let build platform sln =
        sln
        |> MSBuildReleaseExt null [ ("Platform", platform) ] "Restore;Build"
        |> Log (sprintf "Build-%s-Output: " platform)

    !! "*.sln" |> build "x86"
    !! "*.sln" |> build "x64"
)

Target "Version" (fun _ ->
    !! "**/Properties/AssemblyInfo.cs"
    |> Seq.iter (updateVersionInfo false releaseNotes.AssemblyVersion)

    !! "LZ4/Properties/AssemblyInfo.cs"
    ++ "LZ4.net2/Properties/AssemblyInfo.cs"
    ++ "LZ4.portable/Properties/AssemblyInfo.cs"
    ++ "LZ4.netcore/Properties/AssemblyInfo.cs"
    ++ "LZ4.silverlight/Properties/AssemblyInfo.cs"
    ++ "LZ4pn/Properties/AssemblyInfo.cs"
    ++ "LZ4ps/Properties/AssemblyInfo.cs"
    ++ "LZ4cc/AssemblyInfo.cpp" ++ "LZ4cc/app.rc"
    ++ "LZ4mm/AssemblyInfo.cpp" ++ "LZ4mm/app.rc"
    |> Seq.iter (updateVersionInfo true releaseNotes.AssemblyVersion)
)

Target "Release" (fun _ ->
    [ "any"; "x86"; "x64" ]
    |> Seq.iter (fun dir ->
        let targetDir = releaseDir @@ dir
        targetDir |> CleanDir
        [ "LZ4.dll"; "LZ4pn.dll" ]
        |> Seq.map (fun fn -> "LZ4/bin/Release" @@ fn)
        |> Copy targetDir
    )

    [ "x86"; "x64" ] |> Seq.iter (fun platform ->
        let w32platform = match platform with | "x86" -> "win32" | _ -> platform
        [ "LZ4cc.dll"; "LZ4mm.dll" ]
        |> Seq.iter (fun fn ->
            let source = (sprintf "bin/%s/Release" w32platform) @@ fn
            let targetAny = releaseDir @@ "any" @@ (changeExt (sprintf ".%s.dll" platform) fn)
            let targetXxx = releaseDir @@ platform @@ fn
            source |> CopyFile targetAny
            source |> CopyFile targetXxx
        )
    )

    [ "netcore"; "portable"; "silverlight"; "net2" ]
    |> Seq.iter (fun platform ->
        let sourceDir = sprintf "LZ4.%s/bin/Release/**" platform
        let targetDir = releaseDir @@ platform
        targetDir |> CleanDir
        !! (sourceDir @@ "*.dll") |> Copy targetDir
    )

    releaseDir @@ "net4" |> CleanDir
    !! (releaseDir @@ "any" @@ "*.dll")
    |> CopyFiles (releaseDir @@ "net4")

    let fullSnk = (fileInfo strongFile).FullName
    let libzApp = "packages/LibZ.Tool/tools/libz.exe"
    let libzArgs = sprintf """ inject-dll -a LZ4.dll -i *.dll -e LZ4.dll --move -k "%s" """ fullSnk
    { defaultParams with
        Program = libzApp;
        WorkingDirectory = releaseDir @@ "net4";
        CommandLine = libzArgs }
    |> shellExec |> ignore
)

Target "Test" (fun _ ->
    [ "x86"; "x64" ]
    |> Seq.iter (fun platform ->
        let suffix = match platform with | "x86" -> "-x86" | _ -> ""
        !! (platform |> sprintf "LZ4.Tests/bin/%s/Release/LZ4.Tests.dll")
        |> NUnit (fun p -> { p with Framework = "4.0"; ToolName = sprintf "nunit-console%s.exe" suffix; TimeOut = TimeSpan.FromHours(1.0) })
    )
)

Target "Nuget" (fun _ ->
    /// let apiKey = getSecret "nuget" None
    let apiKey = "apikey"
    let version = releaseNotes.AssemblyVersion
    let libDir spec = spec |> sprintf @"lib\%s" |> Some
    let portableSpec = "portable-net4+win8+wpa81+MonoAndroid+MonoTouch+Xamarin.iOS"
    let silverlightSpec = "portable-net4+win8+wpa81+sl5+wp8+MonoAndroid+MonoTouch+Xamarin.iOS"

    let files = [
        ("net2\\*.dll", libDir "net2", None)
        ("net4\\*.dll", libDir "net4-client", None)
        ("portable\\*.dll", libDir portableSpec, None)
        ("silverlight\\*.dll", libDir silverlightSpec, None)
        ("netcore\\LZ4.dll", libDir "netstandard1.0", None)
    ]
    
    let net16dep = ("NETStandard.Library", "1.6.1")

    let coreDependencies = [
        ("NETStandard.Library", "1.6.1")
        ("lz4net.unsafe.netcore", "[" + version + "]")
    ]
    
    let dependencies = [
        { FrameworkVersion = "net2"; Dependencies = [] }
        { FrameworkVersion = "net4-client"; Dependencies = [] }
        { FrameworkVersion = portableSpec; Dependencies = [] }
        { FrameworkVersion = silverlightSpec; Dependencies = [] }
        { FrameworkVersion = "netstandard1.0"; Dependencies = coreDependencies }
    ]

    NuGet (fun p -> 
        { p with
            Version = version
            WorkingDir = @"../out/release"
            OutputPath = @"../out/release"
            ReleaseNotes = releaseNotes.Notes |> toLines
            References = [@"LZ4.dll"]
            AccessKey = apiKey
            Files = files
            DependenciesByFramework = dependencies
        }
    ) "lz4net.nuspec"

    NuGet (fun p -> 
        { p with
            Version = version
            WorkingDir = @"../out/release"
            OutputPath = @"../out/release"
            ReleaseNotes = releaseNotes.Notes |> toLines
            AccessKey = apiKey
            Files = [ ("netcore\\LZ4pn.dll", libDir "netstandard1.0", None) ]
            DependenciesByFramework = [ { FrameworkVersion = "netstandard1.0"; Dependencies = [ net16dep ] } ]
        }
    ) "lz4net.unsafe.netcore.nuspec"

)

Target "Zip" (fun _ ->
    let zipName suffix = sprintf "lz4net-%s-%s.zip" releaseNotes.AssemblyVersion suffix
    let zipDir suffix dirName =
        !! (releaseDir @@ dirName @@ "*.*")
        |> Zip (releaseDir @@ dirName) (releaseDir @@ (zipName suffix))
    "net4" |> zipDir "net4-allinone"
    "net2" |> zipDir "net2-safe"
    "portable" |> zipDir "portable"
    "silverlight" |> zipDir "silverlight"
    "x86" |> zipDir "net4-x86"
    "x64" |> zipDir "net4-x64"
    "netcore" |> zipDir "netcore"
)

Target "Dist" ignore

"KeyGen" ==> "Build"
"Version" ==> "Build"
"Clean" ==> "Release"
"Build" ==> "Release"
"Build" ==> "Test"
"Release" ==> "Nuget"
"Release" ==> "Zip"
"Nuget" ==> "Dist"
"Zip" ==> "Dist"

RunTargetOrDefault "Release"

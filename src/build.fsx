#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing
open StrongNamingHelper

let outDir = "./../out"
let testDir = outDir @@ "test"
let buildDir = outDir @@ "build"
let releaseDir = outDir @@ "release"
let snk = "LZ4.snk"

let testFile fn = (fileInfo fn).Exists

Target "KeyGen" (fun _ ->
    match testFile snk with
    | true -> ()
    | _ -> snk |> sprintf "-k %s" |> StrongName id
)

Target "Clean" (fun _ ->
    !! "**/bin" ++ "**/obj" |> CleanDirs
    "./../out" |> DeleteDir
)

Target "Build" (fun _ -> 
    !! "*.sln"
    |> MSBuildRelease buildDir "Build"
    |> Log "Build-Output: "
)

"KeyGen" ==> "Build"

RunTargetOrDefault "Build"
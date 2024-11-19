#r "nuget: Fun.Build, 1.1.14"

open System.IO
open Fun.Build

let restore name = stage $"Restoring {name}" { run $"dotnet restore {name}" }

let build name = stage $"Building {name}" {
  run $"dotnet build {name} --no-restore -c Release"
}

let test name = stage $"Testing {name}" {
  run $"dotnet test {name} --no-restore"
}

let pack name = stage $"Packing {name}" {
  run $"dotnet pack {name} --no-build --no-restore -o dist"
}

let pushNugets = stage $"Push to NuGet" {

  run(fun ctx -> async {

    let nugetApiKey = ctx.GetEnvVar "NUGET_DEPLOY_KEY"
    let nugets = Directory.GetFiles(__SOURCE_DIRECTORY__ + "/dist", "*.nupkg")

    for nuget in nugets do
      printfn "Pushing %s" nuget

      let! res =
        ctx.RunSensitiveCommand
          $"dotnet nuget push {nuget} --skip-duplicate  -s https://api.nuget.org/v3/index.json -k {nugetApiKey}"

      match res with
      | Ok _ -> return ()
      | Error err -> failwith err
  })
}


pipeline "release" {
  let project = "JDeck"
  restore project
  build project
  pack project
  pushNugets
  runIfOnlySpecified true
}

pipeline "release:local" {
  let project = "JDeck"
  restore project
  build project
  pack project
  runIfOnlySpecified true
}

pipeline "ci:library" {
  let project = "JDeck"
  restore project
  build project
  test project
  runIfOnlySpecified true
}

pipeline "ci:docs" {
  stage "Restoring Tools" { run "dotnet tool restore" }
  restore "JDeck"
  build "JDeck"

  stage "Fsdocs build" {
    run "dotnet fsdocs build --properties Condfiguration=Release"
  }

  runIfOnlySpecified true
}

pipeline "ci:sample" {
  let project = "Sample/Sample.fsproj"
  restore project
  build project

  runIfOnlySpecified true
}

pipeline "build" {
  stage "Restoring Tools" { run "dotnet tool restore" }
  stage "Restore Solution" { run "dotnet restore" }
  stage "Build Solution" { run "dotnet build --no-restore -tl" }
  stage "Test Solution" { run "dotnet test --no-restore" }

  runIfOnlySpecified true
}

tryPrintPipelineCommandHelp()

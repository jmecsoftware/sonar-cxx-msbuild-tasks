﻿open System
open System.IO
open System.IO.Compression
open System.Text
open System.Net
open System.Text.RegularExpressions
open System.Diagnostics
open System.Reflection
open MsbuildTasksCommandExecutor
open FSharp.Data

open Options



[<EntryPoint>]
let main argv = 

    printfn "%A" argv
    let arguments = parseArgs(argv)
    let mutable ret = 0

    try
        if arguments.ContainsKey("h") then
            ShowHelp()
        else
            let options = new OptionsData(argv)
            options.ValidateSolutionOptions()
            options.ConfigureMsbuildRunner()
            options.ConfigureInstallationOfTools()
            options.CreatOptionsForAnalysis()
            options.Setup()

            try                
                if SonarRunnerPhases.BeginPhase(options) <> 0 then
                    ret <- 1
                    printf "[CxxSonarQubeMsbuidRunner] Failed to execute Begin Phase, check log"
                else
                    let targetFile = Path.Combine(options.HomePath, ".sonarqube", "bin", "Targets", "SonarQube.Integration.targets")
                    PatchMSbuildSonarRunnerTargetsFiles(targetFile)
                    
                    if SonarRunnerPhases.RunBuild(options) <> 0 then
                        ret <- 1
                        printf "[CxxSonarQubeMsbuidRunner] Failed to build project, check log in .cxxresults\BuildLog.txt"
                    else
                        if SonarRunnerPhases.EndPhase(options) <> 0 then
                            ret <- 1
                            printf "[CxxSonarQubeMsbuidRunner] Failed analyse project, check log"            
            with
            | ex ->
                printf "Exception During Run: %s \r\n %s" ex.Message ex.StackTrace            
                ret <- 1

            options.Clean()

        with
        | ex ->
            printf "Exception During Run: %s" ex.Message
            ret <- 1
        
    ret

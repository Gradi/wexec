module wexec.Program

open System.Reflection
open Argu
open Printf
open System
open System.Diagnostics
open System.Text
open System.Threading
open CliArgs


let printUsage (parser: ArgumentParser<CliArgs>) =
    let exe = parser.ProgramName
    let version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    let str = StringBuilder ()

    bprintf str "Version: %s\n" version
    bprintf str $"Usage: %s{exe} [optional arguments to %s{exe}] -- [command and it's arguments].\n"

    bprintf str $"Example: %s{exe} -- calc.exe\n"
    bprintf str "\tRuns \"calc.exe\" without arguments.\n"
    bprintf str $"Example: %s{exe} -- calc.exe arg1 arg2 arg3\n"
    bprintf str "\tRuns \"calc.exe\" with \"arg1\", \"arg2\", \"arg3\" arguments.\n"
    bprintf str $"Example: %s{exe} -p high -w -- calc.exe arg1 arg2 arg3\n"
    bprintf str "\tRuns \"calc.exe\" with \"high\" priority, waits for it and arguments being \"arg1\", \"arg2\", \"arg3\"."
    bprintf str $"\"--\" is used to split arguments to \"%s{exe}\" and arguments to subcommand.\n"
    printfn "%s" (parser.PrintUsage(message = str.ToString ()))


let sleep (parseResults: ParseResults<CliArgs>) =
    let seconds = parseResults.GetResult (Delay, defaultValue = 0)
    if seconds > 0 then
        Thread.Sleep (TimeSpan.FromSeconds (float seconds))


let splitArguments (argv: string array) =
    let folder (myArgs, jobArgs, state) argument =
        match state, argument with
        | true, arg -> (myArgs, jobArgs @ [ arg ], true)
        | false, "--" -> (myArgs, jobArgs, true)
        | false, arg -> (myArgs @ [ arg ], jobArgs, false)

    let myArgs, jobArgs, _ =
        argv
        |> Array.fold folder ([], [], false)

    (Array.ofList myArgs, jobArgs)



[<EntryPoint>]
let main argv =
    try
        let myArgs, jobArgs = splitArguments argv
        let parser = ArgumentParser.Create<CliArgs>()
        let parseResults = parser.ParseCommandLine (myArgs, raiseOnUsage = false)

        if parseResults.IsUsageRequested then
            printUsage parser
            0
        else
            match jobArgs with
            | [] -> 0
            | exe :: args ->
                sleep parseResults

                match parseResults.Contains CpuAffinity with
                | false ->
                    let info = ProcessStartInfo (exe, args)
                    use job = Process.Start info
                    if parseResults.Contains Priority then
                        job.PriorityClass <- parseResults.GetResult Priority
                    if parseResults.Contains Wait then
                        job.WaitForExit ()
                        job.ExitCode
                    else
                        0

                // Use WinApi cause .NET doesn't have api to set cpu affinity.
                | true ->
                    let cpuAffinityMask = parseCpuAffinity (parseResults.GetResult CpuAffinity)
                    use job = Process.start exe args
                    job.SetCpuAffinity cpuAffinityMask
                    if parseResults.Contains Priority then
                        job.SetProcessPriority (parseResults.GetResult Priority)

                    job.ResumeMainThread ()

                    if parseResults.Contains Wait then
                        job.WaitForExit ()
                        job.GetExitCode ()
                    else
                        0

    with
    | exc ->
        eprintfn "%O" exc
        255

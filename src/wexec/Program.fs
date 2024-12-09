module wexec.Program

open Argu
open Printf
open System
open System.Diagnostics
open System.Text
open System.Threading

type CliArgs =
    | [<AltCommandLine("-p")>] Priority of ProcessPriorityClass
    | [<AltCommandLine("-w")>] Wait
    | [<AltCommandLine("-d")>] Delay of int

    interface IArgParserTemplate with

        member this.Usage =
            match this with
            | Priority _ -> "Sets process priority."
            | Wait -> "Waits process before exit."
            | Delay _ -> "Delays start. Seconds."

let printUsage (parser: ArgumentParser<CliArgs>) =
    let exe = parser.ProgramName
    let str = StringBuilder ()
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
                let info = ProcessStartInfo (exe, args)
                sleep parseResults
                use job = Process.Start info
                if parseResults.Contains Priority then
                    job.PriorityClass <- parseResults.GetResult Priority
                if parseResults.Contains Wait then
                    job.WaitForExit ()
                    job.ExitCode
                else
                    0

    with
    | exc ->
        eprintfn "%O" exc
        255

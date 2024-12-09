module wexec.CliArgs

open System.Diagnostics
open System.Globalization
open System.Text.RegularExpressions
open Argu
open System


type CliArgs =
    | [<AltCommandLine("-p")>] Priority of ProcessPriorityClass
    | [<AltCommandLine("-w")>] Wait
    | [<AltCommandLine("-d")>] Delay of int
    | [<AltCommandLine("-c")>] CpuAffinity of string

    interface IArgParserTemplate with

        member this.Usage =
            match this with
            | Priority _ -> "Sets process priority."
            | Wait -> "Waits process before exit."
            | Delay _ -> "Delays start. Seconds."
            | CpuAffinity _ -> "Cpu affinity. Can be: number (3) -- number of cpus, hex number (0x3) -- bit mask for cpus, list (1,2,3,5-6) list of cpus."


let (|JustNumber|_|) (input: string) =
    let result = Regex.Match (input, "^([0-9]+)$")
    match result.Success with
    | false -> None
    | true -> Some <| Int32.Parse (result.Groups[1].Value)


let (|HexNumber|_|) (input: string) =
    let result = Regex.Match (input, "^0x([0-9a-fA-F]+)$")
    match result.Success with
    | false -> None
    | true -> Some <| UInt64.Parse (result.Groups[1].Value, NumberStyles.HexNumber)


let (|CpuList|_|) (input: string) =
    let result = Regex.Match (input, "^(\d+(-\d+)?)(,(\d+(-\d+)?))*$")
    match result.Success with
    | false -> None
    | true ->

        let parseOne (text: string) =
            if Regex.IsMatch (text, "^\d+$") then
                [ Int32.Parse text ]
            else
                let matchResult = Regex.Match (text, "^(\d+)-(\d+)$")
                match matchResult.Success with
                | false -> failwithf "Should not happen"
                | true -> [ Int32.Parse matchResult.Groups[1].Value .. Int32.Parse matchResult.Groups[2].Value ]

        input.Split ','
        |> Seq.ofArray
        |> Seq.map parseOne
        |> Seq.collect id
        |> List.ofSeq
        |> Some


let parseCpuAffinity (affinity: string) : uint64 =
    let setBit (value: uint64) (bitNumber: int) : uint64 =
        value ||| ( 1UL <<< bitNumber)

    let applyBits (bitNumbers: int list) =
        List.fold setBit 0UL bitNumbers

    match affinity with
    | JustNumber cpuCount -> applyBits [ 0 .. (cpuCount - 1) ]
    | HexNumber mask -> mask
    | CpuList bitList -> applyBits bitList
    | _ -> failwithf "\"%s\" is not valid cpu affinity specifier." affinity




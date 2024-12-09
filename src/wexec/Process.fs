module wexec.Process

open System
open System.Diagnostics
open System.Runtime.InteropServices
open System.Text
open PInvoke


module private WinApi =

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool SetProcessAffinityMask(IntPtr hProcess, uint64 dwProcessAffinityMask)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool SetPriorityClass(IntPtr hProcess, int32 dwPriorityClass)


type W32Process (procInfo: Kernel32.PROCESS_INFORMATION) =

    let INFINITE : int = 0xffffffff

    member _.SetCpuAffinity (mask: uint64) =
        match WinApi.SetProcessAffinityMask (procInfo.hProcess, mask) with
        | true -> ()
        | false ->
            let error = Kernel32.GetLastError ()
            failwithf "Failed to SetProcessAffinityMask(%x). GetLastError=%O" mask error

    member _.SetProcessPriority (priority: ProcessPriorityClass) =
        let numValue = int priority
        match WinApi.SetPriorityClass (procInfo.hProcess, numValue) with
        | true -> ()
        | false ->
            let error = Kernel32.GetLastError ()
            failwithf "Failed to SetPriorityClass(%x). GetLastError=%O" numValue error

    member _.ResumeMainThread () =
        use handle = new Kernel32.SafeObjectHandle (procInfo.hThread, ownsHandle = false)
        Kernel32.ResumeThread handle |> ignore

    member _.WaitForExit () =
        use handle = new Kernel32.SafeObjectHandle (procInfo.hProcess, ownsHandle = false)
        Kernel32.WaitForSingleObject (handle, INFINITE) |> ignore

    member _.GetExitCode () =
        let mutable exitCode : int = 0
        match Kernel32.GetExitCodeProcess (procInfo.hProcess, &exitCode) with
        | true -> exitCode
        | false ->
            let error = Kernel32.GetLastError ()
            failwithf "Failed to get exit code of process. GetLastError=%O" error

    member this.Dispose () = (this :> IDisposable).Dispose ()

    interface IDisposable with

        member _.Dispose () =
            Kernel32.CloseHandle procInfo.hThread |> ignore
            Kernel32.CloseHandle procInfo.hProcess |> ignore


let escapeArgument (arg: string) =
    let result = StringBuilder arg.Length
    for char in arg do
        match char with
        | '"' -> result.Append "\\\"" |> ignore
        | '\'' -> result.Append "\\'" |> ignore
        | char -> result.Append char |> ignore

    result.ToString ()


let start (exe: string) (args: string list) =
    let mutable startInfo = Kernel32.STARTUPINFO.Create ()
    let mutable procInfo = Kernel32.PROCESS_INFORMATION ()

    let argsSingleStr =
        args
        |> List.map escapeArgument
        |> List.map (fun arg -> sprintf "\"%s\"" arg)
        |> String.concat " "

    let commandLine = sprintf "\"%s\" %s" exe argsSingleStr

    let result = Kernel32.CreateProcess (Unchecked.defaultof<string>, commandLine, IntPtr.Zero, IntPtr.Zero,
                                         false, Kernel32.CreateProcessFlags.CREATE_SUSPENDED,
                                         IntPtr.Zero, Unchecked.defaultof<string>, &startInfo, &procInfo)

    match result with
    | true -> new W32Process (procInfo)
    | false ->
        let error = Kernel32.GetLastError ()
        failwithf "Failed to CreateProcess(%s). GetLastError=%O" commandLine error

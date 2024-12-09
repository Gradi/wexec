# wexec

Runs subcommand. That's all.
Nuance is that `wexec` is compiled with `/SUBSYSTEM:windows` which disables automatic
console window creation.

## Usage

```
Usage: wexec [optional arguments to wexec] -- [command and it's arguments].
Example: wexec -- calc.exe
        Runs "calc.exe" without arguments.
Example: wexec -- calc.exe arg1 arg2 arg3
        Runs "calc.exe" with "arg1", "arg2", "arg3" arguments.
Example: wexec -p high -w -- calc.exe arg1 arg2 arg3
        Runs "calc.exe" with "high" priority, waits for it and arguments being "arg1", "arg2", "arg3"."--" is used to split arguments to "wexec" and arguments to subcommand.

USAGE: wexec [--help] [--priority <normal|idle|high|realtime|belownormal|abovenormal>] [--wait] [--delay <int>] [--cpuaffinity <string>]

OPTIONS:

    --priority, -p <normal|idle|high|realtime|belownormal|abovenormal>
                          Sets process priority.
    --wait, -w            Waits process before exit.
    --delay, -d <int>     Delays start. Seconds.
    --cpuaffinity, -c <string>
                          Cpu affinity. Can be: number (3) -- number of cpus, hex number (0x3) -- bit mask for cpus, list (1,2,3,5-6) list of cpus. Windows only.
    --help                display this list of options.
```

P.s. If you try run `wexec` from console/terminal you will not see any output. That's okay.


namespace Textwexec.Tests
open System
open NUnit.Framework
open FsUnit
open wexec.CliArgs


module TestCliArgs =

    // Here "just number" means count of cpus, not bit index.
    [<TestCase("1", ExpectedResult = 0b1UL)>]
    [<TestCase("2", ExpectedResult = 0b11UL)>]
    [<TestCase("3", ExpectedResult = 0b111)>]
    [<TestCase("4", ExpectedResult = 0b1111)>]

    [<TestCase("0-0", ExpectedResult = 0b1UL)>]
    [<TestCase("0-1", ExpectedResult = 0b11UL)>]
    [<TestCase("4-5", ExpectedResult = 0b110000UL)>]
    [<TestCase("10-17", ExpectedResult = 0b111111110000000000UL)>]

    [<TestCase("1,5,9", ExpectedResult = 0b1000100010UL)>]

    [<TestCase("7-13,3,7", ExpectedResult = 0b11111110001000UL)>]

    [<TestCase("0xdeadbeef", ExpectedResult = 0b11011110101011011011111011101111UL)>]
    let ``parseCpuAffinity just works`` (input: string) = parseCpuAffinity input


    [<TestCase("")>]
    [<TestCase("absd")>]
    [<TestCase("")>]
    let ``parseCpuAffinity fail in invalid input`` (input: string) =
        (fun () -> parseCpuAffinity input |> ignore)
        |> should throw typeof<Exception>








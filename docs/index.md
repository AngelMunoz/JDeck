# JDeck a System.Text.Json wrapper

JDeck is a  [Thoth.Json]-like Json decoder based on `System.Text.Json` in a single file with no external
dependencies. Plays well with other libraries that use `System.Text.Json` like [FSharp.SystemTextJson]

> **Note:** While JDeck has no dependencies to start working right away, it is recommended to
> use [FsToolkit.ErrorHandling]

## Usage

For most F# types, you can use the `Decode.auto` function to decode JSON as shown below:

```fsharp
#r "nuget: JDeck, 1.0.0-beta-*"

open JDeck

type Person = {
  Name: string
  Age: int
  Emails: string list
}
let json = """{"name": "Alice", "age": 30, emails: ["alice@name.com", "alice@age.com"] }"""

let result: Result<Person, DecodeError> = Decoding.auto(json)

match result with
| Ok person -> printfn "Person: %A" person
| Error err -> printfn "Error: %A" err
```

In cases where the data is inconclusive, you deserialize Discriminated Unions or does not play well with F# immutability, you can create a manual decoder.

```fsharp
#r "nuget: JDeck, 1.0.0-beta-*"

open System.Text.Json
open JDeck

type Person = {
  Name: string
  Age: int
  Emails: string list
}
type ServerResponse = { Data: Person; Message: string }

module Person =
  let Decoder person = decode {
    let! name = Required.Property.get("name", Optional.string)
    and! age = Required.Property.get("name", Required.string)
    and! emails = Required.Property.list("emails", Optional.string)
    return {
      Name = name |> Option.defaultValue "<missing name>"
      Age = age
      // Remove any optional value from the array
      Emails = emails |> Array.choose id
    }
  }
// Inconclusive data coming from the server
let person = """{"name": null, "age": 30, emails: ["alice@name.com", "alice@age.com", null] }"""

let result: Result<ServerResponse, DecodeError> =
  // ServerResponse will decode automatically while Person will use the custom decoder
  Decoding.auto(
    $$"""{ "data": {{person}}, "message": "Success" }""",
    // Include your own decoder
    JsonSerializerOptions() |> Decode.useDecoder Person.Decoder
  )

 match result with
 | Ok person -> printfn "Person: %A" person
 | Error err -> printfn "Error: %A" err
```


## Acknowledgements

Nothing is done in the void, in this case I'd like to thank the following libraries for their inspiration and ideas:

- [Thoth.Json] for the inspiration and a cross-runtime solution to JSON decoding, compatible with F#, JS, and Python.
- [FsToolkit.ErrorHandling] for the general mechanism for dealing with Result types and Computation expressions

[Thoth.Json]: https://github.com/thoth-org/Thoth.Json
[FSharp.SystemTextJson]: https://github.com/Tarmil/FSharp.SystemTextJson
[FsToolkit.ErrorHandling]: https://github.com/demystifyfp/FsToolkit.ErrorHandling

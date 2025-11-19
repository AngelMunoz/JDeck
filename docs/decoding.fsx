(** # Decoding Guide

When you use <abbr title="System.Text.Json">STJ</abbr> to deserialize JSON, you would likely expect to Just call
*)

(***hide***)
#r "nuget: JDeck, 1.0.0"

open System.Text.Json
open System.Text.Json.Serialization
open JDeck

(***show***)
type MyType = { id: string }

let myObject = JsonSerializer.Deserialize<MyType>("""{"id":"a1b2c3d4"}""")
(**

And call it a day, and that's fine for most of the cases specially if you own the server which is producing the JSON.

There are a few cases where you might want to customize the deserialization process, for example:

- You want to decode a JSON object into a discriminated union.
- The server is returning a JSON object with inconsistent keys, data or structure.
- The JSON deserializing process is introducing nulls in your F# code.

In these cases, you can customize the deserialization process by manually mapping the JSON to your F# types.

## Automatic Decoding

Before we dive into manual decoding, let's see how you can automatically decode a JSON string into an F# type. using normal means of deserialization.
In addition to the `JsonSerializer.Deserialize` method, you can use the `Decoding` type provided by JDeck to decode JSON strings into F# types.

*)

let myobj = Decoding.auto<MyType>("""{"id":"a1b2c3d4"}""")

(**
The `auto` method calls `JsonDocument.Parse(jsonString)` internally and then deserializes the JSON object into the provided type. It works the same way as `JsonSerializer.Deserialize`.

For cases where you want to customize the deserialization process, you can register decoders in a JsonSerializerOptions instance and pass it to the `auto` method.
For the next case, let's assume that for some reason we need a special decoding process for the `MyType` type.
*)

(***hide***)
let myDecoder =
  fun jsonElement -> decode {
    let! id = Required.Property.get ("id", Required.string) jsonElement
    return { id = id }
  }
(***show***)

let options = JsonSerializerOptions() |> Codec.useDecoder<MyType>(myDecoder)

let myobj2 = Decoding.auto<MyType>("""{"id":"a1b2c3d4"}""", options)
// Or
let myobj3 =
  JsonSerializer.Deserialize<MyType>("""{"id":"a1b2c3d4"}""", options)

(**
In this way, you're able to customize the deserialization process for a specific type.

If your custom decoder relies on other custom decoders (for nested types) and needs access to the active JsonSerializerOptions (for example to call Decode.autoJsonOptions options), register it with a factory so it receives the same options instance:
*)

(***hide***)
let contactDecoder
  (opts: JsonSerializerOptions)
  : Decoder<{| value: string |}> =
  fun json -> decode {
    // can use opts here, e.g. Decode.autoJsonOptions opts
    let! value = Required.Property.get ("value", Required.string) json
    return {| value = value |}
  }
(***show***)

let opts = JsonSerializerOptions() |> Codec.useDecoderWithOptions contactDecoder
let obj = Decoding.auto<{| value: string |}>("""{"value":"ok"}""", opts)

(**
Speaking of decoders, a decoder is defined as:

*)
type Decoder<'TResult> = JsonElement -> Result<'TResult, DecodeError>
(**

Where `JsonElement` is the type representing a JSON object in <abbr title="System.Text.Json">STJ</abbr>.
where `'TResult` is the type you want to decode the JSON into. For example a `Decoder<int>` would map a JSON string to an integer.

## Required Vs Optional

A Decoder can be defined as required or optional. A required decoder will fail if the data does not match the expected type, and if it is missing from the JSON string,
while an optional decoder will not fail if the data is missing or has a null value.

As an example let's see the following two values.
*)
let value = Decoding.fromString("10", Required.int)
printfn $"Value: %A{value}" // Value: Ok 10

let noValue = Decoding.fromString("null", Optional.int)
printfn $"Value: %A{noValue}" // Value: Ok None

(**
As you see, the value is successful and the `noValue` is successful as well, but it is `None`. given that null is not a valid integer in F#.
However, if you try to decode a value that is not an integer with an optional integer decoder, it will fail.
*)

let invalidValue = Decoding.fromString("\"abc\"", Optional.int)
printfn $"Value: %A{invalidValue}"
// Error { value = abc
//         kind = String
//         rawValue = "\"abc\""
//         targetType = System.Object
//         message = "Expected 'Number' but got `String`"
//         exn = None
//         index = None
//         property = None }

(**
With this you can be sure that the data you are decoding is of the expected type, even if it is missing or not.

## Decoding JSON Objects

As you're already aware, decoding primitives returns results, and this means that in order to decode a JSON object you need to do it only on successful results.
Thus needing to nest and generate pyramids of `match` expressions which is not ideal not funny, unmaintainable and cumbersome.

> ***Note***: In general we recommend that you use [FsToolkit.ErrorHandling]'s `result {}` computation expression to handle the errors and the results of the decoders. In a very seamless way. Please refer to the [FsToolkit Section](./using-with-fstoolkit.html) for more information.

For cases like those and if you want to avoid the dependency on [FsToolkit.ErrorHandling], you can use the built-in `decode {}` computation expression. we provide.

The `decode {}` computation expression is a way to chain multiple decoders together in a single expression, and it will short-circuit if any of the decoders fail.
*)

type Person = {
  name: string
  age: int
  email: string option
}

let objectDecoder: Decoder<Person> =
  fun jsonElement -> decode {
    let! name = Required.Property.get ("name", Required.string) jsonElement
    and! age = Required.Property.get ("age", Required.int) jsonElement
    // An optional property means that the key "emails" can be missing from the JSON object
    // However, if the key is present it must comply with the decoder (in this case a string)
    and! email = Optional.Property.get ("emails", Required.string) jsonElement

    return {
      name = name
      age = age
      email = email
    }
  }
(**
Another way to decode the above object is to expect the email key to be present in the document, but it can be null.
*)

let objDecoder2 jsonElement = decode {
  let! name = Required.Property.get ("name", Required.string) jsonElement
  and! age = Required.Property.get ("age", Required.int) jsonElement
  // Now the key must be present in the JSON object, but it can be null.
  and! email = Required.Property.get ("emails", Optional.string) jsonElement

  return {
    name = name
    age = age
    email = email
  }
}

(**
## Decoding Discriminated Unions

Decoding a discriminated union is a bit complex as it may be represented in many shapes or forms, there isn't really a general concensus on how to represent a discriminated union in JSON and that's the reason it is not supported by default in STJ.
Let's define the following discriminated union.
*)

type PageStatus =
  | Idle
  | Loading
  | FailedWith of string
  | Special of int

(**
How do you represent this in JSON? a string or an array with a string and a value? or an object with a key and a value? however you decide to represent it, you need to write a decoder for it.

For cases like this, we provide a helper function called `oneOf` which takes a list of decoders and tries to decode the JSON object with each decoder until one of them succeeds.

First let's define the decoders for the `PageStatus` type.
*)

module PageStatus =
  // decodes {"status": "idle"}
  let idleAndLoadingDecoder el = decode {
    let! value = Required.string el

    match value with
    | "idle" -> return Idle
    | "loading" -> return Loading
    | _ ->
      return!
        DecodeError.ofError(
          el.Clone(),
          "The provided value is not either idle or loading"
        )
        |> Result.Error
  }

  // decodes {"status": ["failed-with", "message"] }
  // decodes {"status": ["special", <int status>] }
  let failedOrSpecialDecoder el = decode {
    return!
      el
      |> Required.Property.get(
        "status",
        fun el -> decode {
          let! type' = Decode.decodeAt Required.string 0 el

          match type' with
          | "failed-with" ->
            return!
              Decode.decodeAt Required.string 1 el |> Result.map FailedWith
          | "special" ->
            return! Decode.decodeAt Required.int 1 el |> Result.map Special
          | _ ->
            return!
              DecodeError.ofError(
                el,
                "The provided value is not either \"failed-with\" or \"special\""
              )
              |> Error
        }
      )
  }

(**

> ***Note***: Don't forget to call `.Clone()` on the `JsonElement` when you're returning an error, as the JsonElement may come from a JsonDocument which is a disposable type.
> If you don't clone it, you run the risk of the JsonElement being disposed before you can use it.

As you can see, the `idleAndLoadingDecoder` decoder expects a JSON object with a key `status` and a value of either `idle` or `loading`,
while the `failedDecoder` expects a JSON object with a key `status` and a value of an array with two elements,
the first element is `failed-with` and the second element is the message.

Now let's use the `oneOf` function to decode the JSON object.
*)

let decodedValue =
  Decoding.fromString(
    """{"status": "idle"}""",
    Decode.oneOf [
      PageStatus.failedOrSpecialDecoder
      PageStatus.idleAndLoadingDecoder
    ]
  )
// val decodedValue : Result<PageStatus, DecodeError> = Ok Idle

(**
While the order of the decoders in the `oneOf` is not important,
since we're in a stop-on-first-success mode,
it is recommended to put the most likely decoders to succeed first.

[FsToolkit.ErrorHandling]: https://github.com/demystifyfp/FsToolkit.ErrorHandling
*)

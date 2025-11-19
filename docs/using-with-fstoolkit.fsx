(**
# Using with FsToolkit.ErrorHandling

One of the most popular libraries for workflows in F# is [FsToolkit.ErrorHandling], this library provides several computation expressions to handle errors in a functional way.

JDeck uses the `Result` type to handle errors, so it's easy to integrate with FsToolkit.ErrorHandling, Our `decode {}` computation expression is basically just a result <abbr title="Computation Expression">CE</abbr> in disguise and constrained to a particular error type.

It works for our purposes when you don't want the dependency on the library however, our CE is severely limitated to our particular use case.
If you're already using FsToolkit in your codebase we recommend using the `result {}` or the `validation {}` CE instead.

*)

(***hide***)

#r "nuget:FsToolkit.ErrorHandling"
#r "nuget: JDeck, 1.0.0"

open System
open System.IO
open System.Text.Json
open FsToolkit.ErrorHandling
open JDeck


(***show***)
let jDeckDecoder =
  fun el -> decode { // built-in CE
    let! value = Required.int el
    return {| value = value |}
  }

let fsToolkitDecoder =
  fun el -> result { // FsToolkit CE
    let! value = Required.int el
    return {| value = value |}
  }

let fsToolkitValDecoder =
  fun el -> validation { // FsToolkit CE
    let! value = Required.int el
    return {| value = value |}
  }

(**
As you can see, the above decoders are completely equivalent with the exception of the validation one, this returns a list of errors instead of a single error.
Given that the result CE is a drop-in replacement for the decode CE, from now on we'll focus on the validation CE.

## Validations

For cases where you'd like to keep decoding after an error for further recollection, this is the way to go.
You may have seen some functions and methods with `col` or `collect` in their names, these are meant to be used with the `validation {}` CE.
*)

// For example let's say we want to decode the payload of a posted user to our server
type User = { username: string; emails: string seq }


(***hide***)

(** First we define some rules for our validations, in the case of the username we want to be sure that it is not an empty string, and it is within a certain limit of characters. These validations here are arbitrary but you should be able to see how can you enforce your own domain rules when decoding the json objects *)

(***show***)
let usernameRules (value: string) (el: JsonElement) = validation {
  let! _ =
    value
    |> Result.requireNotEmpty "Name cannot be empty"
    |> Result.mapError(fun msg -> DecodeError.ofError(el.Clone(), msg))

  and! _ =
    value.Length >= 6
    |> Result.requireTrue "username has to be at last 6 characters"
    |> Result.mapError(fun msg -> DecodeError.ofError(el.Clone(), msg))

  and! _ =
    value.Length <= 20
    |> Result.requireTrue "username has to be at most 20 characters"
    |> Result.mapError(fun msg -> DecodeError.ofError(el.Clone(), msg))

  return ()
}

(***hide***)

(** In the case of the emails, we will do very simple validations but as you may imagine, you can validate domains, against a regex, and even if it already exists if you pass the correct information to this validation. *)

(***show***)
let emailRules (index: int, value: string) (el: JsonElement) = result {
  let! _ =
    value.Contains("@")
    |> Result.requireTrue $"Element at {index} - must contain @"
    |> Result.mapError(fun msg -> DecodeError.ofIndexed(el.Clone(), index, msg))

  and! _ =
    value.Contains(".")
    |> Result.requireTrue $"Element at {index} - must contain ."
    |> Result.mapError(fun msg -> DecodeError.ofIndexed(el.Clone(), index, msg))

  return ()
}
(***hide***)


(** Then we can decode the payload and apply the validations
 Keep in mind that we're using strings for simplicity here
 but the errors should match the actual error of your domain types *)

(***show***)
let bindJson (reqBody: string) = validation {
  use document = JsonDocument.Parse(reqBody)
  let json = document.RootElement

  let! username =
    let decoder =
      fun el -> validation {
        let! value = Required.string el
        do! usernameRules value el
        return value
      }

    Required.Property.get ("username", decoder) json

  let! emails =
    // for validation to work we need to wrap the decoders in a validation {} CE
    // this is because we can't define overloads based on the return type.
    let decoder =
      fun (index: int) el -> validation {
        // decode the element as a string
        let! email = Required.string el

        // validate that it is a valid email according to our rules
        do! emailRules (index, email) el

        // return a validated email
        return email
      }

    json |> Required.Property.get("emails", Decode.sequenceCol(decoder))

  return { username = username; emails = emails }
}

(***hide***)
let reqBody =
  """{ "username": "John Doe", "emails": ["email1@email.com", null, "email2email.com", "not-an-email", null] }"""

(** When we apply this decoder which is also validating our rules to the following JSON string, we expect it to fail, but rather than telling us a single error it will collect the ones available and report them together.*)

(***show***)
// { "username": "John Doe", "emails": ["email1@email.com", null, "email2email.com", "not-an-email", null] }
match bindJson reqBody with
| Ok user -> printfn "User: %A" user
| Error errors -> printfn "Errors: %A" (errors |> List.map _.message)
// Errors: [
//   "Expected 'String' but got `Null`"
//   "Element at 2 - must contain @"
//   "Element at 3 - must contain @"
//   "Expected 'String' but got `Null`"
// ]

(***hide***)
let reqBody2 =
  """{ "username": "John Doe", "emails": ["email1@email.com", "email2@email.com", "not-an-email", "email4@emailcom"] }"""

(** If we provide a non-null string list then we're able to see just the errors that correspond to our validations *)
(***show***)
// { "username": "John Doe", "emails": ["email1@email.com", "email2@email.com", "not-an-email", "email4@emailcom] }
match bindJson reqBody2 with
| Ok user -> printfn "User: %A" user
| Error errors -> printfn "Errors: %A" (errors |> List.map _.message)
// Errors: ["Element at 2 - must contain @"; "Element at 3 - must contain ."]


(**
> ***Note***: Sometimes for simplicity, folks use strings as the resulting error, it is recommended that you provide a more meaningful and information rich type for your errors, as these will be passed on potentially over several layers and the information could be lost if you don't provide a proper type.

*)

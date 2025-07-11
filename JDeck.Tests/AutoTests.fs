namespace JDeck.Tests

open System.Text.Json
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck

[<AutoOpen>]
module AutoTypes =
  type T1 = { name: string; age: int }

type TC =
  | A of int
  | B
  | C of bool

type T2 = { t1: T1; tcustom: TC }

module ReadmeExamples =
  type Person = {
    Name: string
    Age: int
    Emails: string list
  }

  type ServerResponse = { Data: Person; Message: string }

[<TestClass>]
type AutoTests() =

  [<TestMethod>]
  member _.``JDeck can auto decode things``() =
    match Decoding.auto """{ "name": "John Doe", "age": 30 }""" with
    | Ok person ->
      Assert.AreEqual<string>("John Doe", person.name)
      Assert.AreEqual<int>(30, person.age)
    | Error err -> Assert.Fail(err.message)


  [<TestMethod>]
  member _.``JDeck can auto decode types with nested decoders``() =
    let tcDecoder tc =
      Decode.oneOf
        [
          (Required.int >> Result.map A)
          (Required.boolean >> Result.map C)
          (Required.unit >> Result.map(fun _ -> B))
        ]
        tc

    let options = JsonSerializerOptions() |> Codec.useDecoder tcDecoder

    match
      Decoding.auto(
        """{ "t1": { "name": "John Doe", "age": 30 }, "tcustom": 10 }""",
        options
      )
    with
    | Ok t2 ->
      Assert.AreEqual<string>("John Doe", t2.t1.name)
      Assert.AreEqual<int>(30, t2.t1.age)

      match t2.tcustom with
      | A a -> Assert.AreEqual<int>(10, a)
      | _ -> Assert.Fail()

    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck readme example 1 works``() =

    let json =
      """{"name": "Alice", "age": 30, "emails": ["alice@name.com", "alice@age.com"] }"""

    let result: Result<ReadmeExamples.Person, DecodeError> =
      Decoding.auto(
        json,
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)
      )

    match result with
    | Ok person ->

      Assert.AreEqual<string>("Alice", person.Name)
      Assert.AreEqual<int>(30, person.Age)
      Assert.AreEqual<int>(2, person.Emails.Length)
      Assert.AreEqual<string>("alice@name.com", person.Emails[0])
      Assert.AreEqual<string>("alice@age.com", person.Emails[1])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck readme example 2 works``() =

    let personDecoder: Decoder<ReadmeExamples.Person> =
      fun person -> decode {
        let! name = person |> Required.Property.get("name", Optional.string)
        and! age = person |> Required.Property.get("age", Required.int)

        and! emails =
          person |> Required.Property.list("emails", Optional.string)

        return {
          Name = name |> Option.defaultValue "<missing name>"
          Age = age
          // Remove any optional value from the list
          Emails = emails |> List.choose id
        }
      }
    // Inconclusive data coming from the server
    let person =
      """{"name": null, "age": 30, "emails": ["alice@name.com", "alice@age.com", null] }"""

    let result: Result<ReadmeExamples.ServerResponse, DecodeError> =
      // ServerResponse will decode automatically while Person will use the custom decoder
      Decoding.auto(
        $$"""{ "data": {{person}}, "message": "Success" }""",
        // Include your own decoder
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)
        |> Codec.useDecoder personDecoder
      )

    match result with
    | Ok { Data = data; Message = message } ->
      Assert.AreEqual<string>("Success", message)
      Assert.AreEqual<int>(30, data.Age)
      Assert.AreEqual<int>(2, data.Emails.Length)
      Assert.AreEqual<string>("alice@name.com", data.Emails[0])
      Assert.AreEqual<string>("alice@age.com", data.Emails[1])
    | Error err -> Assert.Fail(err.message)

namespace JDeck.Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck
open JDeck.Decode
open FsToolkit.ErrorHandling


[<TestClass>]
type RequiredTests() =

  [<TestMethod>]
  member _.``JDeck can decode strings``() =
    match Decode.fromString("\"This is a string\"", Required.string) with
    | Ok value -> Assert.AreEqual("This is a string", value)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode true as boolean``() =
    match Decode.fromString("true", Required.boolean) with
    | Ok value -> Assert.IsTrue value
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode false as boolean``() =
    match Decode.fromString("false", Required.boolean) with
    | Ok value -> Assert.IsFalse value
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode characters``() =
    match Decode.fromString("\"a\"", Required.char) with
    | Ok value -> Assert.AreEqual('a', value)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck fails to decode characters when strings are longer than 1``
    ()
    =
    match Decode.fromString("\"ab\"", Required.char) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.AreEqual("Expecting a char but got \"ab\" of size: 2", err)

  [<TestMethod>]
  member _.``JDeck can parse guids``() =
    let expected = Guid.NewGuid()

    match Decode.fromString($"\"{expected}\"", Required.guid) with
    | Ok actual -> Assert.AreEqual(expected, actual)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can parse nulls as unit``() =
    match Decode.fromString("null", Required.unit) with
    | Ok actual -> Assert.AreEqual((), actual)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode bytes``() =
    match Decode.fromString("10", Required.byte) with
    | Ok value -> Assert.AreEqual(10uy, value)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode ints``() =
    match Decode.fromString("10", Required.int) with
    | Ok value -> Assert.AreEqual(10, value)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode int64s``() =
    match Decode.fromString("1000", Required.int64) with
    | Ok value -> Assert.AreEqual(1000L, value)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode floats``() =
    match Decode.fromString("1000.50", Required.float) with
    | Ok value -> Assert.AreEqual(1000.50, value)
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode date time``() =
    match
      Decode.fromString("\"2024-11-17T05:35:11.147Z\"", Required.dateTime)
    with
    | Ok value ->
      Assert.AreEqual(
        DateTime(
          DateOnly(2024, 11, 17),
          TimeOnly(5, 35, 11, 147),
          DateTimeKind.Utc
        ),
        value
      )
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode date time offset``() =
    match
      Decode.fromString(
        "\"2024-11-17T05:35:11.147+00:00\"",
        Required.dateTimeOffset
      )
    with
    | Ok value ->
      Assert.AreEqual(
        DateTimeOffset(
          DateOnly(2024, 11, 17),
          TimeOnly(5, 35, 11, 147),
          TimeSpan.Zero
        ),
        value
      )
    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode objects``() =
    let json = """{ "Name": "John Doe", "Age": 30 }"""

    match
      Decode.fromString(
        json,
        (fun element -> validation {

          let! name = element |> Required.property "Name" Required.string
          and! age = element |> Required.property "Age" Required.int
          return {| name = name; age = age |}
        })
      )
    with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)

    | Error err -> Assert.Fail(err |> String.concat ", ")

  [<TestMethod>]
  member _.``JDeck can decode arrays``() =
    let json = """[1, 2, 3, 4, 5]"""

    match Decode.fromString(json, array Required.int) with
    | Ok value ->
      Assert.AreEqual(5, value.Length)
      Assert.AreEqual(1, value[0])
      Assert.AreEqual(2, value[1])
      Assert.AreEqual(3, value[2])
      Assert.AreEqual(4, value[3])
      Assert.AreEqual(5, value[4])

    | Error err -> Assert.Fail(err)

  [<TestMethod>]
  member _.``JDeck can decode nested objects``() =
    let json =
      """{ "Name": "John Doe", "Age": 30, "Address": { "City": "New York", "Country": "USA" } }"""

    match
      Decode.fromString(
        json,
        (fun element -> validation {

          let! name = element |> Required.property "Name" Required.string
          and! age = element |> Required.property "Age" Required.int

          and! address =
            element
            |> Required.property
              "Address"
              (fun address ->
                validation {
                  let! city =
                    address |> Required.property "City" Required.string

                  and! country =
                    address |> Required.property "Country" Required.string

                  return {| city = city; country = country |}
                }
                |> Result.mapError(String.concat ", ")
              )

          return {|
            name = name
            age = age
            address = address
          |}
        })
      )
    with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)
      Assert.AreEqual("New York", value.address.city)
      Assert.AreEqual("USA", value.address.country)
    | Error err -> Assert.Fail(err |> String.concat ", ")

  [<TestMethod>]
  member _.``JDeck can decode nested arrays``() =
    let json =
      """{ "Name": "John Doe", "Age": 30, "Addresses": [ { "City": "New York", "Country": "USA" }, { "City": "London", "Country": "UK" } ] }"""

    match
      Decode.fromString(
        json,
        (fun element -> validation {

          let! name = element |> Required.property "Name" Required.string
          and! age = element |> Required.property "Age" Required.int

          and! addresses =
            element
            |> Required.property
              "Addresses"
              (array
                (fun address ->
                  validation {
                    let! city =
                      address |> Required.property "City" Required.string

                    and! country =
                      address |> Required.property "Country" Required.string

                    return {| city = city; country = country |}
                  }
                  |> Result.mapError(String.concat ", ")
                )
              )

          return {|
            name = name
            age = age
            addresses = addresses
          |}
        })
      )
    with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)
      Assert.AreEqual(2, value.addresses.Length)
      Assert.AreEqual("New York", value.addresses[0].city)
      Assert.AreEqual("USA", value.addresses[0].country)
      Assert.AreEqual("London", value.addresses[1].city)
      Assert.AreEqual("UK", value.addresses[1].country)
    | Error err -> Assert.Fail(err |> String.concat ", ")

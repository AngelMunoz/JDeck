namespace JDeck.Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck
open JDeck.Decode
open FsToolkit.ErrorHandling

[<TestClass>]
type OptionalTests() =

  [<TestMethod>]
  member _.``JDeck does not fail on null strings``() =
    match Decode.fromString("null", Optional.string) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null booleans``() =
    match Decode.fromString("null", Optional.boolean) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null characters``() =
    match Decode.fromString("null", Optional.char) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null guids``() =
    match Decode.fromString("null", Optional.guid) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null units``() =
    match Decode.fromString("null", Optional.unit) with
    | Ok(Some value) -> Assert.AreEqual((), value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null bytes``() =
    match Decode.fromString("null", Optional.byte) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null ints``() =
    match Decode.fromString("null", Optional.int) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null int64s``() =
    match Decode.fromString("null", Optional.int64) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null floats``() =
    match Decode.fromString("null", Optional.float) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null DateTimes``() =
    match Decode.fromString("null", Optional.dateTime) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null DateTimeOffsets``() =
    match Decode.fromString("null", Optional.dateTimeOffset) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode false as boolean``() =
    match Decode.fromString("false", Optional.boolean) with
    | Ok(Some value) -> Assert.IsFalse value
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode characters``() =
    match Decode.fromString("\"a\"", Optional.char) with
    | Ok(Some value) -> Assert.AreEqual('a', value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck fails to decode characters when strings are longer than 1``
    ()
    =
    match Decode.fromString("\"ab\"", Optional.char) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.AreEqual(
        "Expecting a char but got a string of size: 2",
        err.message
      )

  [<TestMethod>]
  member _.``JDeck can parse guids``() =
    let expected = Guid.NewGuid()

    match Decode.fromString($"\"{expected}\"", Optional.guid) with
    | Ok(Some actual) -> Assert.AreEqual(expected, actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can parse nulls as unit``() =
    match Decode.fromString("null", Optional.unit) with
    | Ok(Some actual) -> Assert.AreEqual((), actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode bytes``() =
    match Decode.fromString("10", Optional.byte) with
    | Ok(Some value) -> Assert.AreEqual(10uy, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode ints``() =
    match Decode.fromString("10", Optional.int) with
    | Ok(Some value) -> Assert.AreEqual(10, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode int64s``() =
    match Decode.fromString("10", Optional.int64) with
    | Ok(Some value) -> Assert.AreEqual(10L, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode floats``() =
    match Decode.fromString("10.0", Optional.float) with
    | Ok(Some value) -> Assert.AreEqual(10.0, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode DateTimes``() =
    let expected = DateTime.Now

    match Decode.fromString($"\"{expected:O}\"", Optional.dateTime) with
    | Ok(Some actual) -> Assert.AreEqual(expected, actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode DateTimeOffsets``() =
    let expected = DateTimeOffset.Now

    match Decode.fromString($"\"{expected:O}\"", Optional.dateTimeOffset) with
    | Ok(Some actual) -> Assert.AreEqual(expected, actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null values in objects``() =
    let json = """{"value": null}"""

    let valueDecoder =
      fun element -> result {
        let! value = element |> Required.Property.get("value", Optional.int)

        return {| value = value |}
      }

    match Decode.fromString(json, valueDecoder) with
    | Ok value -> Assert.AreEqual(None, value.value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on undefined values in objects``() =
    let json = """{}"""

    let valueDecoder =
      fun element -> result {
        let! value = element |> Optional.Property.get("value", Required.int)

        return {| value = value |}
      }

    match Decode.fromString(json, valueDecoder) with
    | Ok value -> Assert.AreEqual(None, value.value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null values in arrays``() =
    let json = """[null]"""

    let valueDecoder =
      fun _ element -> result {
        let! value = element |> Optional.int

        return value
      }

    match Decode.fromString(json, Decode.array valueDecoder) with
    | Ok value -> Assert.AreEqual(None, value[0])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can produce an optional sequence of values``() =
    let json = """[1, null, 3, null, 5]"""

    let valueDecoder =
      fun _ element -> result {
        let! value = element |> Optional.int

        return value
      }

    match Decode.fromString(json, Decode.array valueDecoder) with
    | Ok value ->
      Assert.AreEqual(5, value.Length)
      Assert.AreEqual(1, value[0].Value)
      Assert.AreEqual(None, value[1])
      Assert.AreEqual(3, value[2].Value)
      Assert.AreEqual(None, value[3])
      Assert.AreEqual(5, value[4].Value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode nested objects with optional properties``() =
    let addressDecoder =
      fun _ address -> result {
        let! city = address |> Required.Property.get("city", Required.string)

        and! country =
          address |> Required.Property.get("country", Required.string)

        and! zipCode =
          address |> Optional.Property.get("zipCode", Required.string)

        return {|
          city = city
          country = country
          zipCode = zipCode
        |}
      }

    let decoder =
      fun element -> result {
        let! name = element |> Required.Property.get("name", Required.string)
        and! age = element |> Required.Property.get("age", Required.int)

        and! status =
          element |> Required.Property.get("status", Optional.string)

        and! addresses =
          element
          |> Required.Property.get("addresses", (Decode.array addressDecoder))

        return {|
          name = name
          age = age
          status = status
          addresses = addresses
        |}
      }

    let json =
      """{
  "name": "John Doe", "age": 30, "status": null,
  "addresses": [
    { "city": "New York", "country": "USA", "zipCode": "12345" },
    { "city": "London", "country": "UK" }
  ]
}"""

    let value = Decode.fromString(json, decoder)

    match value with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)
      Assert.AreEqual(None, value.status)
      Assert.AreEqual(2, value.addresses.Length)
      Assert.AreEqual("New York", value.addresses[0].city)
      Assert.AreEqual("USA", value.addresses[0].country)
      Assert.AreEqual("12345", value.addresses[0].zipCode.Value)
      Assert.AreEqual("London", value.addresses[1].city)
      Assert.AreEqual("UK", value.addresses[1].country)
      Assert.AreEqual(None, value.addresses[1].zipCode)
    | Error err -> Assert.Fail(err.message)

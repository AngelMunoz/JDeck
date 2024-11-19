namespace JDeck.Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck
open FsToolkit.ErrorHandling


[<TestClass>]
type RequiredTests() =

  [<TestMethod>]
  member _.``JDeck can decode strings``() =
    match Decoding.fromString("\"This is a string\"", Required.string) with
    | Ok value -> Assert.AreEqual("This is a string", value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode true as boolean``() =
    match Decoding.fromString("true", Required.boolean) with
    | Ok value -> Assert.IsTrue value
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode false as boolean``() =
    match Decoding.fromString("false", Required.boolean) with
    | Ok value -> Assert.IsFalse value
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode characters``() =
    match Decoding.fromString("\"a\"", Required.char) with
    | Ok value -> Assert.AreEqual('a', value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck fails to decode characters when strings are longer than 1``
    ()
    =
    match Decoding.fromString("\"ab\"", Required.char) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.AreEqual(
        "Expecting a char but got a string of size: 2",
        err.message
      )

  [<TestMethod>]
  member _.``JDeck can parse guids``() =
    let expected = Guid.NewGuid()

    match Decoding.fromString($"\"{expected}\"", Required.guid) with
    | Ok actual -> Assert.AreEqual(expected, actual)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can parse nulls as unit``() =
    match Decoding.fromString("null", Required.unit) with
    | Ok actual -> Assert.AreEqual((), actual)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode bytes``() =
    match Decoding.fromString("10", Required.byte) with
    | Ok value -> Assert.AreEqual(10uy, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode ints``() =
    match Decoding.fromString("10", Required.int) with
    | Ok value -> Assert.AreEqual(10, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode int64s``() =
    match Decoding.fromString("1000", Required.int64) with
    | Ok value -> Assert.AreEqual(1000L, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode floats``() =
    match Decoding.fromString("1000.50", Required.float) with
    | Ok value -> Assert.AreEqual(1000.50, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode date time``() =
    match
      Decoding.fromString("\"2024-11-17T05:35:11.147Z\"", Required.dateTime)
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
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode date time offset``() =
    match
      Decoding.fromString(
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
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode objects``() =
    let json = """{ "Name": "John Doe", "Age": 30 }"""

    match
      Decoding.fromStringCol(
        json,
        (fun element -> validation {

          let! name = element |> Required.Property.get("Name", Required.string)
          and! age = element |> Required.Property.get("Age", Required.int)

          return {| name = name; age = age |}
        })
      )
    with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)

    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

  [<TestMethod>]
  member _.``JDeck can decode arrays``() =
    let json = """[1, 2, 3, 4, 5]"""

    match Decoding.fromString(json, Decode.array(fun _ v -> Required.int v)) with
    | Ok value ->
      Assert.AreEqual(5, value.Length)
      Assert.AreEqual(1, value[0])
      Assert.AreEqual(2, value[1])
      Assert.AreEqual(3, value[2])
      Assert.AreEqual(4, value[3])
      Assert.AreEqual(5, value[4])

    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode nested objects``() =
    let json =
      """{ "Name": "John Doe", "Age": 30, "Address": { "City": "New York", "Country": "USA" } }"""

    let addressDecoder =
      fun address -> result {
        let! city = address |> Required.Property.get("City", Required.string)

        and! country =
          address |> Required.Property.get("Country", Required.string)

        return {| city = city; country = country |}
      }

    let decoder =
      fun element -> validation {

        let! name = element |> Required.Property.get("Name", Required.string)
        and! age = element |> Required.Property.get("Age", Required.int)

        and! address =
          element |> Required.Property.get("Address", addressDecoder)

        return {|
          name = name
          age = age
          address = address
        |}
      }

    match Decoding.fromStringCol(json, decoder) with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)
      Assert.AreEqual("New York", value.address.city)
      Assert.AreEqual("USA", value.address.country)
    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

  [<TestMethod>]
  member _.``JDeck can traverse with fail-first results of sequence properties``
    ()
    =

    let addressDecoder =
      fun address -> result {
        let! city = address |> Required.Property.get("city", Required.string)

        and! country =
          address |> Required.Property.get("country", Required.string)

        return {| city = city; country = country |}
      }

    let decoder =
      fun element -> validation {
        let! name = element |> Required.Property.get("name", Required.string)
        and! age = element |> Required.Property.get("age", Required.int)

        and! addresses =
          element |> Required.Property.array("addresses", addressDecoder)

        return {|
          name = name
          age = age
          addresses = addresses
        |}
      }

    let json =
      """{
  "name": "John Doe", "age": 30,
  "addresses": [
    { "city": "New York", "country": "USA" },
    { "city": "London", "country": "UK" }
  ]
}"""

    match Decoding.fromStringCol(json, decoder) with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)
      Assert.AreEqual(2, value.addresses.Length)
      Assert.AreEqual("New York", value.addresses[0].city)
      Assert.AreEqual("USA", value.addresses[0].country)
      Assert.AreEqual("London", value.addresses[1].city)
      Assert.AreEqual("UK", value.addresses[1].country)
    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

  [<TestMethod>]
  member _.``JDeck can traverse with traversable results of sequence properties``
    ()
    =

    let addressDecoder =
      fun address -> validation {
        let! city = address |> Required.Property.get("city", Required.string)

        and! country =
          address |> Required.Property.get("country", Required.string)

        return {| city = city; country = country |}
      }

    let decoder =
      fun element -> validation {
        let! name = element |> Required.Property.get("name", Required.string)
        and! age = element |> Required.Property.get("age", Required.int)

        and! addresses =
          element |> Required.Property.array("addresses", addressDecoder)

        return {|
          name = name
          age = age
          addresses = addresses
        |}
      }

    let json =
      """{
  "name": "John Doe", "age": 30,
  "addresses": [
    { "city": "New York", "country": "USA" },
    { "city": "London", "country": "UK" }
  ]
}"""

    match Decoding.fromStringCol(json, decoder) with
    | Ok value ->
      Assert.AreEqual("John Doe", value.name)
      Assert.AreEqual(30, value.age)
      Assert.AreEqual(2, value.addresses.Length)
      Assert.AreEqual("New York", value.addresses[0].city)
      Assert.AreEqual("USA", value.addresses[0].country)
      Assert.AreEqual("London", value.addresses[1].city)
      Assert.AreEqual("UK", value.addresses[1].country)
    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

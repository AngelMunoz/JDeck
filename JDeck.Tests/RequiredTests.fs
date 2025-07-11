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
    | Ok value -> Assert.AreEqual<string>("This is a string", value)
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
    | Ok value -> Assert.AreEqual<char>('a', value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck fails to decode characters when strings are longer than 1``
    ()
    =
    match Decoding.fromString("\"ab\"", Required.char) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.AreEqual<string>(
        "Expecting a char but got a string of size: 2",
        err.message
      )

  [<TestMethod>]
  member _.``JDeck can parse guids``() =
    let expected = Guid.NewGuid()

    match Decoding.fromString($"\"{expected}\"", Required.guid) with
    | Ok actual -> Assert.AreEqual<Guid>(expected, actual)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can parse nulls as unit``() =
    match Decoding.fromString("null", Required.unit) with
    | Ok actual -> Assert.AreEqual((), actual)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode bytes``() =
    match Decoding.fromString("10", Required.byte) with
    | Ok value -> Assert.AreEqual<byte>(10uy, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode ints``() =
    match Decoding.fromString("10", Required.int) with
    | Ok value -> Assert.AreEqual<int>(10, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode int64s``() =
    match Decoding.fromString("1000", Required.int64) with
    | Ok value -> Assert.AreEqual<int64>(1000L, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode floats``() =
    match Decoding.fromString("1000.50", Required.float) with
    | Ok value -> Assert.AreEqual<float>(1000.50, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode date time``() =
    match
      Decoding.fromString("\"2024-11-17T05:35:11.147Z\"", Required.dateTime)
    with
    | Ok value ->
      Assert.AreEqual<DateTime>(
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
      Assert.AreEqual<DateTimeOffset>(
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
      Assert.AreEqual<string>("John Doe", value.name)
      Assert.AreEqual<int>(30, value.age)

    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

  [<TestMethod>]
  member _.``JDeck can decode arrays``() =
    let json = """[1, 2, 3, 4, 5]"""

    match
      Decoding.fromString(json, Decode.array(fun _ v -> Required.int v))
    with
    | Ok value ->
      Assert.AreEqual<int>(5, value.Length)
      Assert.AreEqual<int>(1, value[0])
      Assert.AreEqual<int>(2, value[1])
      Assert.AreEqual<int>(3, value[2])
      Assert.AreEqual<int>(4, value[3])
      Assert.AreEqual<int>(5, value[4])

    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode nested objects``() =
    let json =
      """{ "Name": "John Doe", "Age": 30, "Address": { "City": "New York", "Country": "USA" } }"""

    let addressDecoder =
      fun address -> decode {
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
      Assert.AreEqual<string>("John Doe", value.name)
      Assert.AreEqual<int>(30, value.age)
      Assert.AreEqual<string>("New York", value.address.city)
      Assert.AreEqual<string>("USA", value.address.country)
    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

  [<TestMethod>]
  member _.``JDeck can traverse with fail-first results of sequence properties``
    ()
    =

    let addressDecoder =
      fun address -> decode {
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
      Assert.AreEqual<string>("John Doe", value.name)
      Assert.AreEqual<int>(30, value.age)
      Assert.AreEqual<int>(2, value.addresses.Length)
      Assert.AreEqual<string>("New York", value.addresses[0].city)
      Assert.AreEqual<string>("USA", value.addresses[0].country)
      Assert.AreEqual<string>("London", value.addresses[1].city)
      Assert.AreEqual<string>("UK", value.addresses[1].country)
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
      Assert.AreEqual<string>("John Doe", value.name)
      Assert.AreEqual<int>(30, value.age)
      Assert.AreEqual<int>(2, value.addresses.Length)
      Assert.AreEqual<string>("New York", value.addresses[0].city)
      Assert.AreEqual<string>("USA", value.addresses[0].country)
      Assert.AreEqual<string>("London", value.addresses[1].city)
      Assert.AreEqual<string>("UK", value.addresses[1].country)
    | Error err ->
      err |> List.fold (fun acc e -> acc + e.message + ", ") "" |> Assert.Fail

  [<TestMethod>]
  member _.``Required.Property.map decodes a map from an object property``() =
    let json = """{ "prop": { "a": 1, "b": 2, "c": 3 } }"""
    let decoder = Required.Property.map("prop", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok map ->
      Assert.AreEqual<int>(3, map.Count)
      Assert.AreEqual<int>(1, map.["a"])
      Assert.AreEqual<int>(2, map.["b"])
      Assert.AreEqual<int>(3, map.["c"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.Property.map returns error if a value is not decodable``
    ()
    =
    let json = """{ "prop": { "a": 1, "b": "oops" } }"""
    let decoder = Required.Property.map("prop", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.IsTrue(err.message.Contains "Expected 'Number' but got `String`")

  [<TestMethod>]
  member _.``Required.Property.dict decodes a dictionary from an object property``
    ()
    =
    let json = """{ "prop": { "x": 10, "y": 20 } }"""
    let decoder = Required.Property.dict("prop", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok dict ->
      Assert.AreEqual<int>(2, dict.Count)
      Assert.AreEqual<int>(10, dict.["x"])
      Assert.AreEqual<int>(20, dict.["y"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.Property.dict returns error if a value is not decodable``
    ()
    =
    let json = """{ "prop": { "x": 10, "y": "bad" } }"""
    let decoder = Required.Property.dict("prop", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.IsTrue(err.message.Contains("Expected 'Number' but got `String`"))

  [<TestMethod>]
  member _.``Required.Property.map collector overload decodes a map from an object property and collects errors``
    ()
    =
    let json = """{ "prop": { "a": 1, "b": 2, "c": 3 } }"""

    let decoder =
      Required.Property.map(
        "prop",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok map ->
      Assert.AreEqual<int>(3, map.Count)
      Assert.AreEqual<int>(1, map.["a"])
      Assert.AreEqual<int>(2, map.["b"])
      Assert.AreEqual<int>(3, map.["c"])
    | Error err -> Assert.Fail(err |> List.head |> (fun e -> e.message))

  [<TestMethod>]
  member _.``Required.Property.map collector overload collects multiple errors``
    ()
    =
    let json = """{ "prop": { "a": 1, "b": "oops", "c": 3, "d": "bad" } }"""

    let decoder =
      Required.Property.map(
        "prop",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok _ -> Assert.Fail("Expected errors but got success")
    | Error errors ->
      Assert.AreEqual<int>(2, errors.Length)

      Assert.IsTrue(
        errors
        |> List.exists(fun e ->
          e.message.Contains "Expected 'Number' but got `String`"
        )
      )

      Assert.IsTrue(errors |> List.exists(fun e -> e.property = Some "b"))
      Assert.IsTrue(errors |> List.exists(fun e -> e.property = Some "d"))

  [<TestMethod>]
  member _.``Required.Property.map collector overload returns error if property is missing``
    ()
    =
    let json = """{ "other": "value" }"""

    let decoder =
      Required.Property.map(
        "prop",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error errors ->
      Assert.AreEqual<int>(1, errors.Length)
      Assert.IsTrue(errors.Head.message.Contains "Property 'prop' not found")

  [<TestMethod>]
  member _.``Required.Property.dict collector overload decodes a dictionary from an object property and collects errors``
    ()
    =
    let json = """{ "prop": { "x": 10, "y": 20 } }"""

    let decoder =
      Required.Property.dict(
        "prop",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok dict ->
      Assert.AreEqual<int>(2, dict.Count)
      Assert.AreEqual<int>(10, dict.["x"])
      Assert.AreEqual<int>(20, dict.["y"])
    | Error err -> Assert.Fail(err |> List.head |> (fun e -> e.message))

  [<TestMethod>]
  member _.``Required.Property.dict collector overload collects multiple errors``
    ()
    =
    let json = """{ "prop": { "x": 10, "y": "bad", "z": 30, "w": "oops" } }"""

    let decoder =
      Required.Property.dict(
        "prop",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok _ -> Assert.Fail("Expected errors but got success")
    | Error errors ->
      Assert.AreEqual<int>(2, errors.Length)

      Assert.IsTrue(
        errors
        |> List.exists(fun e ->
          e.message.Contains "Expected 'Number' but got `String`"
        )
      )

      Assert.IsTrue(errors |> List.exists(fun e -> e.property = Some "y"))
      Assert.IsTrue(errors |> List.exists(fun e -> e.property = Some "w"))

  [<TestMethod>]
  member _.``Required.Property.dict collector overload returns error if property is missing``
    ()
    =
    let json = """{ "other": "value" }"""

    let decoder =
      Required.Property.dict(
        "prop",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error errors ->
      Assert.AreEqual<int>(1, errors.Length)
      Assert.IsTrue(errors.Head.message.Contains "Property 'prop' not found")

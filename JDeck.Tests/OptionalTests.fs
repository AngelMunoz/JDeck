namespace JDeck.Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open System.Text.Json

open JDeck
open FsToolkit.ErrorHandling

[<TestClass>]
type OptionalTests() =

  [<TestMethod>]
  member _.``JDeck does not fail on null strings``() =
    match Decoding.fromString("null", Optional.string) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null booleans``() =
    match Decoding.fromString("null", Optional.boolean) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null characters``() =
    match Decoding.fromString("null", Optional.char) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null guids``() =
    match Decoding.fromString("null", Optional.guid) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null units``() =
    match Decoding.fromString("null", Optional.unit) with
    | Ok(Some value) -> Assert.AreEqual((), value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null bytes``() =
    match Decoding.fromString("null", Optional.byte) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null ints``() =
    match Decoding.fromString("null", Optional.int) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null int64s``() =
    match Decoding.fromString("null", Optional.int64) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null floats``() =
    match Decoding.fromString("null", Optional.float) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null DateTimes``() =
    match Decoding.fromString("null", Optional.dateTime) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck does not fail on null DateTimeOffsets``() =
    match Decoding.fromString("null", Optional.dateTimeOffset) with
    | Ok(Some _) -> Assert.Fail("Expected None but got a value")
    | Ok None -> ()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode false as boolean``() =
    match Decoding.fromString("false", Optional.boolean) with
    | Ok(Some value) -> Assert.IsFalse value
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode characters``() =
    match Decoding.fromString("\"a\"", Optional.char) with
    | Ok(Some value) -> Assert.AreEqual<char>('a', value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck fails to decode characters when strings are longer than 1``
    ()
    =
    match Decoding.fromString("\"ab\"", Optional.char) with
    | Ok _ -> Assert.Fail()
    | Error err ->
      Assert.AreEqual<string>(
        "Expecting a char but got a string of size: 2",
        err.message
      )

  [<TestMethod>]
  member _.``JDeck can parse guids``() =
    let expected = Guid.NewGuid()

    match Decoding.fromString($"\"{expected}\"", Optional.guid) with
    | Ok(Some actual) -> Assert.AreEqual<Guid>(expected, actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can parse nulls as unit``() =
    match Decoding.fromString("null", Optional.unit) with
    | Ok(Some actual) -> Assert.AreEqual((), actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode bytes``() =
    match Decoding.fromString("10", Optional.byte) with
    | Ok(Some value) -> Assert.AreEqual<byte>(10uy, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode ints``() =
    match Decoding.fromString("10", Optional.int) with
    | Ok(Some value) -> Assert.AreEqual<int>(10, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode int64s``() =
    match Decoding.fromString("10", Optional.int64) with
    | Ok(Some value) -> Assert.AreEqual<int64>(10L, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode floats``() =
    match Decoding.fromString("10.0", Optional.float) with
    | Ok(Some value) -> Assert.AreEqual<float>(10.0, value)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode DateTimes``() =
    let expected = DateTime.Now

    match Decoding.fromString($"\"{expected:O}\"", Optional.dateTime) with
    | Ok(Some actual) -> Assert.AreEqual<DateTime>(expected, actual)
    | Ok None -> Assert.Fail("Expected a value but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode DateTimeOffsets``() =
    let expected = DateTimeOffset.Now

    match Decoding.fromString($"\"{expected:O}\"", Optional.dateTimeOffset) with
    | Ok(Some actual) -> Assert.AreEqual<DateTimeOffset>(expected, actual)
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

    match Decoding.fromString(json, valueDecoder) with
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

    match Decoding.fromString(json, valueDecoder) with
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

    match Decoding.fromString(json, Decode.array valueDecoder) with
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

    match Decoding.fromString(json, Decode.array valueDecoder) with
    | Ok value ->
      Assert.AreEqual<int>(5, value.Length)
      Assert.AreEqual<int>(1, value[0].Value)
      Assert.AreEqual<int option>(None, value[1])
      Assert.AreEqual<int>(3, value[2].Value)
      Assert.AreEqual<int option>(None, value[3])
      Assert.AreEqual<int>(5, value[4].Value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can decode nested objects with optional properties``() =
    let addressDecoder =
      fun _ address -> decode {
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
      fun element -> decode {
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

    let value = Decoding.fromString(json, decoder)

    match value with
    | Ok value ->
      Assert.AreEqual<string>("John Doe", value.name)
      Assert.AreEqual<int>(30, value.age)
      Assert.AreEqual<string option>(None, value.status)
      Assert.AreEqual<int>(2, value.addresses.Length)
      Assert.AreEqual<string>("New York", value.addresses[0].city)
      Assert.AreEqual<string>("USA", value.addresses[0].country)
      Assert.AreEqual<string>("12345", value.addresses[0].zipCode.Value)
      Assert.AreEqual<string>("London", value.addresses[1].city)
      Assert.AreEqual<string>("UK", value.addresses[1].country)
      Assert.AreEqual<string option>(None, value.addresses[1].zipCode)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.Property.map decodes a map from an object property if present``
    ()
    =
    let json = """{ "m": { "a": 1, "b": 2 } }"""
    let decoder = Optional.Property.map("m", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok(Some map) ->
      Assert.AreEqual<int>(2, map.Count)
      Assert.AreEqual<int>(1, map.["a"])
      Assert.AreEqual<int>(2, map.["b"])
    | Ok None -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.Property.map returns None if property is missing``() =
    let json = """{}"""
    let decoder = Optional.Property.map("m", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok None -> ()
    | Ok(Some _) -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.Property.dict decodes a dictionary from an object property if present``
    ()
    =
    let json = """{ "d": { "x": 10, "y": 20 } }"""
    let decoder = Optional.Property.dict("d", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok(Some dict) ->
      Assert.AreEqual<int>(2, dict.Count)
      Assert.AreEqual<int>(10, dict.["x"])
      Assert.AreEqual<int>(20, dict.["y"])
    | Ok None -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.Property.dict returns None if property is missing``() =
    let json = """{}"""
    let decoder = Optional.Property.dict("d", Required.int)

    match Decoding.fromString(json, decoder) with
    | Ok None -> ()
    | Ok(Some _) -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.Property.map collector overload decodes a map from an object property if present and collects errors``
    ()
    =
    let json = """{ "m": { "a": 1, "b": 2, "c": 3 } }"""

    let decoder =
      Optional.Property.map(
        "m",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok(Some map) ->
      Assert.AreEqual<int>(3, map.Count)
      Assert.AreEqual<int>(1, map.["a"])
      Assert.AreEqual<int>(2, map.["b"])
      Assert.AreEqual<int>(3, map.["c"])
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err |> List.head |> (fun e -> e.message))

  [<TestMethod>]
  member _.``Optional.Property.map collector overload collects multiple errors``
    ()
    =
    let json = """{ "m": { "a": 1, "b": "oops", "c": 3, "d": "bad" } }"""

    let decoder =
      Optional.Property.map(
        "m",
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
  member _.``Optional.Property.map collector overload returns None if property is missing``
    ()
    =
    let json = """{ "other": "value" }"""

    let decoder =
      Optional.Property.map(
        "m",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok None -> Assert.IsTrue(true)
    | Ok(Some _) -> Assert.Fail("Expected None but got Some")
    | Error err -> Assert.Fail(err |> List.head |> (fun e -> e.message))

  [<TestMethod>]
  member _.``Optional.Property.dict collector overload decodes a dictionary from an object property if present and collects errors``
    ()
    =
    let json = """{ "d": { "x": 10, "y": 20 } }"""

    let decoder =
      Optional.Property.dict(
        "d",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok(Some dict) ->
      Assert.AreEqual<int>(2, dict.Count)
      Assert.AreEqual<int>(10, dict.["x"])
      Assert.AreEqual<int>(20, dict.["y"])
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err |> List.head |> (fun e -> e.message))

  [<TestMethod>]
  member _.``Optional.Property.dict collector overload collects multiple errors``
    ()
    =
    let json = """{ "d": { "x": 10, "y": "bad", "z": 30, "w": "oops" } }"""

    let decoder =
      Optional.Property.dict(
        "d",
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
  member _.``Optional.Property.dict collector overload returns None if property is missing``
    ()
    =
    let json = """{ "other": "value" }"""

    let decoder =
      Optional.Property.dict(
        "d",
        fun _key element ->
          Required.int element |> Result.mapError List.singleton
      )

    match Decoding.fromStringCol(json, decoder) with
    | Ok None -> Assert.IsTrue(true)
    | Ok(Some _) -> Assert.Fail("Expected None but got Some")
    | Error err -> Assert.Fail(err |> List.head |> (fun e -> e.message))

  [<TestMethod>]
  member _.``Optional.map can decode a simple Map of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.map Required.string root

    match decoded with
    | Ok(Some result) ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<string>("postgres", result.["database"])
      Assert.AreEqual<string>("5432", result.["port"])
      Assert.AreEqual<string>("true", result.["ssl"])
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.map returns None for null``() =
    let payload = """null"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.map Required.string root

    match decoded with
    | Ok None -> Assert.IsTrue(true)
    | Ok(Some _) -> Assert.Fail("Expected None but got Some")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.map can decode a Map of integers``() =
    let payload = """{ "timeout": 30, "retries": 5, "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.map Required.int root

    match decoded with
    | Ok(Some result) ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<int>(30, result.["timeout"])
      Assert.AreEqual<int>(5, result.["retries"])
      Assert.AreEqual<int>(1, result.["debug"])
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.map can decode nested Maps``() =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432" }, "config2": { "database": "mysql", "port": "3306" } }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.map (Optional.map Required.string) root

    match decoded with
    | Ok(Some result) ->
      Assert.AreEqual<int>(2, result.Count)
      Assert.IsTrue(result.ContainsKey("config1"))
      Assert.IsTrue(result.ContainsKey("config2"))

      let config1 = result.["config1"]

      match config1 with
      | Some cfg1 ->
        Assert.AreEqual<string>("postgres", cfg1.["database"])
        Assert.AreEqual<string>("5432", cfg1.["port"])
      | None -> Assert.Fail("Expected Some config1 but got None")

      let config2 = result.["config2"]

      match config2 with
      | Some cfg2 ->
        Assert.AreEqual<string>("mysql", cfg2.["database"])
        Assert.AreEqual<string>("3306", cfg2.["port"])
      | None -> Assert.Fail("Expected Some config2 but got None")
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.map fails when encountering wrong type``() =
    let payload = """{ "timeout": 30, "retries": "invalid", "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.map Required.int root

    match decoded with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error err ->
      Assert.IsTrue(err.message.Contains("Expected 'Number' but got `String`"))

  [<TestMethod>]
  member _.``Optional.map can decode empty objects``() =
    let payload = """{}"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.map Required.string root

    match decoded with
    | Ok(Some result) -> Assert.AreEqual<int>(0, result.Count)
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.dict can decode a simple Dictionary of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.dict Required.string root

    match decoded with
    | Ok(Some result) ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<string>("postgres", result.["database"])
      Assert.AreEqual<string>("5432", result.["port"])
      Assert.AreEqual<string>("true", result.["ssl"])
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.dict returns None for null``() =
    let payload = """null"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.dict Required.string root

    match decoded with
    | Ok None -> Assert.IsTrue(true)
    | Ok(Some _) -> Assert.Fail("Expected None but got Some")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.dict can decode a Dictionary of integers``() =
    let payload = """{ "timeout": 30, "retries": 5, "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.dict Required.int root

    match decoded with
    | Ok(Some result) ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<int>(30, result.["timeout"])
      Assert.AreEqual<int>(5, result.["retries"])
      Assert.AreEqual<int>(1, result.["debug"])
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.dict can decode nested Dictionaries``() =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432" }, "config2": { "database": "mysql", "port": "3306" } }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.dict (Optional.dict Required.string) root

    match decoded with
    | Ok(Some result) ->
      Assert.AreEqual<int>(2, result.Count)
      Assert.IsTrue(result.ContainsKey("config1"))
      Assert.IsTrue(result.ContainsKey("config2"))

      let config1 = result.["config1"]

      match config1 with
      | Some cfg1 ->
        Assert.AreEqual<string>("postgres", cfg1.["database"])
        Assert.AreEqual<string>("5432", cfg1.["port"])
      | None -> Assert.Fail("Expected Some config1 but got None")

      let config2 = result.["config2"]

      match config2 with
      | Some cfg2 ->
        Assert.AreEqual<string>("mysql", cfg2.["database"])
        Assert.AreEqual<string>("3306", cfg2.["port"])
      | None -> Assert.Fail("Expected Some config2 but got None")
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.dict fails when encountering wrong type``() =
    let payload = """{ "timeout": 30, "retries": "invalid", "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.dict Required.int root

    match decoded with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error err ->
      Assert.IsTrue(err.message.Contains("Expected 'Number' but got `String`"))

  [<TestMethod>]
  member _.``Optional.dict can decode empty objects``() =
    let payload = """{}"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Optional.dict Required.string root

    match decoded with
    | Ok(Some result) -> Assert.AreEqual<int>(0, result.Count)
    | Ok None -> Assert.Fail("Expected Some but got None")
    | Error err -> Assert.Fail(err.message)

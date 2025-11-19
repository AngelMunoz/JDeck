namespace JDeck.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open System.Text.Json
open JDeck

type UnionSample =
  | A of string
  | B of int
  | C of bool


[<TestClass>]
type DecodingTests() =

  [<TestMethod>]
  member _.``JDeck sequence can decode sequences``() =
    match
      Decoding.fromString("[1,2,3]", Decode.array(fun _ el -> Required.int el))
    with
    | Ok value ->
      Seq.iteri (fun i (v: int) -> Assert.AreEqual<int>(i + 1, v)) value
    | Error err -> Assert.Fail err.message

  [<TestMethod>]
  member _.``JDeck sequence can decode sequences with null values``() =
    match
      Decoding.fromString(
        "[1,null,3]",
        Decode.array(fun _ el -> Optional.int el)
      )
    with
    | Ok value ->
      let values = Seq.toArray value
      Assert.AreEqual<int>(3, values.Length)
      Assert.AreEqual<int option>(Some 1, values[0])
      Assert.AreEqual<int option>(None, values[1])
      Assert.AreEqual<int option>(Some 3, values[2])
    | Error err -> Assert.Fail err.message

  [<TestMethod>]
  member _.``JDeck can use oneOf to decode unions``() =
    let aDecoder value =
      match Required.string value with
      | Ok value -> Ok(A value)
      | Error err -> Error err

    let bDecoder value =
      match Required.int value with
      | Ok value -> Ok(B value)
      | Error err -> Error err

    let cDecoder value =
      match Required.boolean value with
      | Ok value -> Ok(C value)
      | Error err -> Error err

    let unionDecoder =
      Required.Property.get(
        "value",
        Decode.oneOf [ cDecoder; bDecoder; aDecoder ]
      )

    match Decoding.fromString("""{ "value": "string" }""", unionDecoder) with
    | Ok(A value) -> Assert.AreEqual<string>("string", value)
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.Fail err.message

    let unionDecoder =
      Required.Property.get(
        "value",
        Decode.oneOf [ aDecoder; cDecoder; bDecoder ]
      )

    match Decoding.fromString("""{ "value": 1 }""", unionDecoder) with
    | Ok(B value) -> Assert.AreEqual<int>(1, value)
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.Fail err.message

    let unionDecoder =
      Required.Property.get(
        "value",
        (Decode.oneOf [ aDecoder; bDecoder; cDecoder ])
      )

    match Decoding.fromString("""{ "value": true }""", unionDecoder) with
    | Ok(C value) -> Assert.AreEqual<bool>(true, value)
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.Fail err.message

  [<TestMethod>]
  member _.``decodeAt can decode an array with multiple types``() =
    let decoder el = decode {
      let! number = Decode.decodeAt Required.int 0 el
      let! str = Decode.decodeAt Required.string 1 el
      let! boolean = Decode.decodeAt Required.boolean 2 el
      return (number, str, boolean)
    }

    match Decoding.fromString("[1, \"string\", true]", decoder) with
    | Ok(number, str, boolean) ->
      Assert.AreEqual<int>(1, number)
      Assert.AreEqual<string>("string", str)
      Assert.AreEqual<bool>(true, boolean)
    | Error err -> Assert.Fail err.message

  [<TestMethod>]
  member _.``decodeAt does not throw if the element is not an array``() =

    let decoder el = decode {
      let! number = Decode.decodeAt Required.int 0 el
      let! str = Decode.decodeAt Required.string 1 el
      let! boolean = Decode.decodeAt Required.boolean 2 el
      return (number, str, boolean)
    }

    match Decoding.fromString("{}", decoder) with
    | Ok _ -> Assert.Fail()
    | Error _ -> ()

  [<TestMethod>]
  member _.``decodeAtKey can extract a value by key from an object``() =
    let decoder el = decode {
      let! value = Decode.decodeAtKey Required.int "age" el
      return value
    }

    match Decoding.fromString("""{ "age": 42 }""", decoder) with
    | Ok value -> Assert.AreEqual<int>(42, value)
    | Error err -> Assert.Fail err.message

  [<TestMethod>]
  member _.``decodeAtKey throws if key is missing``() =
    let decoder el = decode {
      let! value = Decode.decodeAtKey Required.int "missing" el
      return value
    }

    Assert.ThrowsException<System.Collections.Generic.KeyNotFoundException>(fun
                                                                                () ->
      match Decoding.fromString("""{ "age": 42 }""", decoder) with
      | Ok _ -> ()
      | Error _ -> ()
    )
    |> ignore

  [<TestMethod>]
  member _.``tryDecodeAtKey returns Some if key exists``() =
    let decoder el = decode {
      let! value = Decode.tryDecodeAtKey Required.int "age" el
      return value
    }

    match Decoding.fromString("""{ "age": 42 }""", decoder) with
    | Ok(Some value) -> Assert.AreEqual<int>(42, value)
    | Ok None -> Assert.Fail()
    | Error err -> Assert.Fail err.message

  [<TestMethod>]
  member _.``tryDecodeAtKey returns Error if key is missing``() =
    let decoder el = decode {
      let! value = Decode.tryDecodeAtKey Required.int "missing" el
      return value
    }

    match Decoding.fromString("""{ "age": 42 }""", decoder) with
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.IsTrue(err.message.Contains "Key missing not found")

  [<TestMethod>]
  member _.``Decode.map can decode a simple Map of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Decode.map (fun _ -> Required.string) root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<string>("postgres", result.["database"])
      Assert.AreEqual<string>("5432", result.["port"])
      Assert.AreEqual<string>("true", result.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Decode.map can decode nested Maps of strings``() =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432", "ssl": "true" }, "config2": { "database": "mysql", "port": "3306", "ssl": "false" } }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement

    let decoded =
      Decode.map (fun _ -> Decode.map(fun _ -> Required.string)) root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(2, result.Count)
      Assert.IsTrue(result.ContainsKey("config1"))
      Assert.IsTrue(result.ContainsKey("config2"))

      let config1 = result.["config1"]
      Assert.AreEqual<int>(3, config1.Count)
      Assert.AreEqual<string>("postgres", config1.["database"])
      Assert.AreEqual<string>("5432", config1.["port"])
      Assert.AreEqual<string>("true", config1.["ssl"])

      let config2 = result.["config2"]
      Assert.AreEqual<int>(3, config2.Count)
      Assert.AreEqual<string>("mysql", config2.["database"])
      Assert.AreEqual<string>("3306", config2.["port"])
      Assert.AreEqual<string>("false", config2.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.Property.get can decode nested Maps from a property``() =
    let payload =
      """{ "configurations": { "config1": { "database": "postgres", "port": "5432", "ssl": "true" }, "config2": { "database": "mysql", "port": "3306", "ssl": "false" } } }"""

    let decoder =
      Required.Property.get(
        "configurations",
        fun el -> Decode.map (fun _ -> Decode.map(fun _ -> Required.string)) el
      )

    match Decoding.fromString(payload, decoder) with
    | Ok result ->
      Assert.AreEqual<int>(2, result.Count)
      Assert.IsTrue(result.ContainsKey("config1"))
      Assert.IsTrue(result.ContainsKey("config2"))

      let config1 = result.["config1"]
      Assert.AreEqual<int>(3, config1.Count)
      Assert.AreEqual<string>("postgres", config1.["database"])
      Assert.AreEqual<string>("5432", config1.["port"])
      Assert.AreEqual<string>("true", config1.["ssl"])

      let config2 = result.["config2"]
      Assert.AreEqual<int>(3, config2.Count)
      Assert.AreEqual<string>("mysql", config2.["database"])
      Assert.AreEqual<string>("3306", config2.["port"])
      Assert.AreEqual<string>("false", config2.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Decode.map can decode Maps with integer values``() =
    let payload = """{ "timeout": 30, "retries": 5, "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Decode.map (fun _ -> Required.int) root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<int>(30, result.["timeout"])
      Assert.AreEqual<int>(5, result.["retries"])
      Assert.AreEqual<int>(1, result.["debug"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Decode.map fails when encountering wrong type``() =
    let payload = """{ "timeout": 30, "retries": "invalid", "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Decode.map (fun _ -> Required.int) root

    match decoded with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error err ->
      Assert.IsTrue(err.message.Contains("Expected 'Number' but got `String`"))

  [<TestMethod>]
  member _.``Decode.map can decode empty objects``() =
    let payload = """{}"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Decode.map (fun _ -> Required.string) root

    match decoded with
    | Ok result -> Assert.AreEqual<int>(0, result.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.Property.map can decode simple Maps of strings``() =
    let payload =
      """{ "config": { "database": "postgres", "port": "5432", "ssl": "true" } }"""

    let decoder = Required.Property.map("config", Required.string)

    match Decoding.fromString(payload, decoder) with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<string>("postgres", result.["database"])
      Assert.AreEqual<string>("5432", result.["port"])
      Assert.AreEqual<string>("true", result.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.map can decode a simple Map of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.map Required.string root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<string>("postgres", result.["database"])
      Assert.AreEqual<string>("5432", result.["port"])
      Assert.AreEqual<string>("true", result.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.map can decode a Map of integers``() =
    let payload = """{ "timeout": 30, "retries": 5, "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.map Required.int root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<int>(30, result.["timeout"])
      Assert.AreEqual<int>(5, result.["retries"])
      Assert.AreEqual<int>(1, result.["debug"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.map can decode nested Maps``() =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432" }, "config2": { "database": "mysql", "port": "3306" } }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.map (Required.map Required.string) root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(2, result.Count)
      Assert.IsTrue(result.ContainsKey("config1"))
      Assert.IsTrue(result.ContainsKey("config2"))

      let config1 = result.["config1"]
      Assert.AreEqual<string>("postgres", config1.["database"])
      Assert.AreEqual<string>("5432", config1.["port"])

      let config2 = result.["config2"]
      Assert.AreEqual<string>("mysql", config2.["database"])
      Assert.AreEqual<string>("3306", config2.["port"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.map fails when encountering wrong type``() =
    let payload = """{ "timeout": 30, "retries": "invalid", "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.map Required.int root

    match decoded with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error err ->
      Assert.IsTrue(err.message.Contains("Expected 'Number' but got `String`"))

  [<TestMethod>]
  member _.``Required.map can decode empty objects``() =
    let payload = """{}"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.map Required.string root

    match decoded with
    | Ok result -> Assert.AreEqual<int>(0, result.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dict can decode a simple Dictionary of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.dict Required.string root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<string>("postgres", result.["database"])
      Assert.AreEqual<string>("5432", result.["port"])
      Assert.AreEqual<string>("true", result.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dict can decode a Dictionary of integers``() =
    let payload = """{ "timeout": 30, "retries": 5, "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.dict Required.int root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(3, result.Count)
      Assert.AreEqual<int>(30, result.["timeout"])
      Assert.AreEqual<int>(5, result.["retries"])
      Assert.AreEqual<int>(1, result.["debug"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dict can decode nested Dictionaries``() =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432" }, "config2": { "database": "mysql", "port": "3306" } }"""

    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.dict (Required.dict Required.string) root

    match decoded with
    | Ok result ->
      Assert.AreEqual<int>(2, result.Count)
      Assert.IsTrue(result.ContainsKey("config1"))
      Assert.IsTrue(result.ContainsKey("config2"))

      let config1 = result.["config1"]
      Assert.AreEqual<string>("postgres", config1.["database"])
      Assert.AreEqual<string>("5432", config1.["port"])

      let config2 = result.["config2"]
      Assert.AreEqual<string>("mysql", config2.["database"])
      Assert.AreEqual<string>("3306", config2.["port"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dict fails when encountering wrong type``() =
    let payload = """{ "timeout": 30, "retries": "invalid", "debug": 1 }"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.dict Required.int root

    match decoded with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error err ->
      Assert.IsTrue(err.message.Contains("Expected 'Number' but got `String`"))

  [<TestMethod>]
  member _.``Required.dict can decode empty objects``() =
    let payload = """{}"""
    use doc = JsonDocument.Parse(payload)
    let root = doc.RootElement
    let decoded = Required.dict Required.string root

    match decoded with
    | Ok result -> Assert.AreEqual<int>(0, result.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.timeSpan can decode a TimeSpan``() =
    let payload = """{ "duration": "01:02:03" }"""
    let decoder = Required.Property.get("duration", Required.timeSpan)
    match Decoding.fromString(payload, decoder) with
    | Ok value -> Assert.AreEqual<TimeSpan>(TimeSpan(1, 2, 3), value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Optional.timeSpan can decode a TimeSpan``() =
    let payload = """{ "duration": "01:02:03" }"""
    let decoder = Optional.Property.get("duration", Optional.timeSpan)
    match Decoding.fromString(payload, decoder) with
    | Ok(Some(Some value)) -> Assert.AreEqual<TimeSpan>(TimeSpan(1, 2, 3), value)
    | Ok _ -> Assert.Fail("Expected Some(Some value)")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``VOptional.string can decode a string``() =
    let payload = """{ "name": "John" }"""
    let decoder = Required.Property.get("name", VOptional.string)
    match Decoding.fromString(payload, decoder) with
    | Ok(ValueSome value) -> Assert.AreEqual<string>("John", value)
    | Ok ValueNone -> Assert.Fail("Expected ValueSome but got ValueNone")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``VOptional.timeSpan can decode a TimeSpan``() =
    let payload = """{ "duration": "01:02:03" }"""
    let decoder = Required.Property.get("duration", VOptional.timeSpan)
    match Decoding.fromString(payload, decoder) with
    | Ok(ValueSome value) -> Assert.AreEqual<TimeSpan>(TimeSpan(1, 2, 3), value)
    | Ok ValueNone -> Assert.Fail("Expected ValueSome but got ValueNone")
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dateTimeExact can decode a DateTime with custom format``() =
    let payload = """{ "date": "2023/01/02" }"""
    let decoder = Required.Property.get("date", Required.dateTimeExact "yyyy/MM/dd")
    match Decoding.fromString(payload, decoder) with
    | Ok value -> Assert.AreEqual<DateTime>(DateTime(2023, 1, 2), value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dateTimeOffsetExact can decode a DateTimeOffset with custom format``() =
    let payload = """{ "date": "2023/01/02 +00:00" }"""
    let decoder = Required.Property.get("date", Required.dateTimeOffsetExact "yyyy/MM/dd zzz")
    match Decoding.fromString(payload, decoder) with
    | Ok value -> Assert.AreEqual<DateTimeOffset>(DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero), value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.timeSpanExact can decode a TimeSpan with custom format``() =
    let payload = """{ "duration": "01:02:03" }"""
    let decoder = Required.Property.get("duration", Required.timeSpanExact "hh\:mm\:ss")
    match Decoding.fromString(payload, decoder) with
    | Ok value -> Assert.AreEqual<TimeSpan>(TimeSpan(1, 2, 3), value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Required.dateTimeExactWith can decode a DateTime with custom culture``() =
    let payload = """{ "date": "19 novembre 2023" }"""
    let culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR")
    let decoder = Required.Property.get("date", Required.dateTimeExactWith "dd MMMM yyyy" culture System.Globalization.DateTimeStyles.None)
    match Decoding.fromString(payload, decoder) with
    | Ok value -> Assert.AreEqual<DateTime>(DateTime(2023, 11, 19), value)
    | Error err -> Assert.Fail(err.message)

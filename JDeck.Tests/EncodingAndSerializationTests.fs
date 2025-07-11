namespace JDeck.Tests

open System
open System.Text.Json
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck


[<TestClass>]
type EncodingTests() =


  [<TestMethod>]
  member _.``Encode can encode a string``() =
    let encoded = Encode.string "Hello, World!"
    let expected = "\"Hello, World!\""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a boolean``() =
    let encoded = Encode.boolean true
    let expected = "true"

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a char``() =
    let encoded = Encode.char 'a'
    let expected = "\"a\""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a guid``() =
    let guid = Guid.NewGuid()
    let encoded = Encode.guid guid
    let expected = $"\"{guid.ToString()}\""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a byte``() =
    let encoded = Encode.byte 255uy
    let expected = "255"

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode an int``() =
    let encoded = Encode.int 42
    let expected = "42"

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode an int64``() =
    let encoded = Encode.int64 42L
    let expected = "42"

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a float``() =
    let encoded = Encode.float 42.5
    let expected = "42.5"

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a DateTime``() =
    let dateTime = DateTime.Now
    let encoded = Encode.dateTime dateTime
    let expected = $"\"{dateTime:o}\""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode can encode a DateTimeOffset``() =
    let dateTimeOffset = DateTimeOffset.Now
    let encoded = Encode.dateTimeOffset dateTimeOffset
    let expected = $"\"{dateTimeOffset:o}\""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Encode an object``() =
    let encoded =
      Json.object [ ("name", Encode.string "John"); ("age", Encode.int 30) ]
      |> _.ToJsonString()

    let expected = "{\"name\":\"John\",\"age\":30}"

    Assert.AreEqual<string>(expected, encoded)


  [<TestMethod>]

  member _.``Encode an object pipe style``() =

    let encoded =
      Json.empty()
      |> Encode.property("name", Encode.string "John")
      |> Encode.property("age", Encode.int 30)
      |> Encode.property("isAlive", Encode.boolean true)
      |> Encode.property(
        "address",
        Json.empty()
        |> Encode.property("street", Encode.string "21 2nd Street")
        |> Encode.property("city", Encode.string "New York")
        |> Encode.property("state", Encode.string "NY")
        |> Encode.property("postalCode", Encode.string "10021")
      )
      |> _.ToJsonString()

    let expected =
      "{\"name\":\"John\",\"age\":30,\"isAlive\":true,\"address\":{\"street\":\"21 2nd Street\",\"city\":\"New York\",\"state\":\"NY\",\"postalCode\":\"10021\"}}"

    Assert.AreEqual<string>(expected, encoded)

type T1 =
  | A of int
  | B of string
  | C of bool

[<TestClass>]
type SerializationTests() =

  [<TestMethod>]
  member _.``Codec useEncoder registers the encoder``() =
    let options =
      JsonSerializerOptions()
      |> Codec.useEncoder(fun (x: T1) ->
        match x with
        | A a -> Encode.int a
        | B b -> Encode.string b
        | C c -> Encode.boolean c
      )

    let encodedA = JsonSerializer.Serialize(T1.A 42, options)
    let encodedB = JsonSerializer.Serialize(T1.B "Hello, World!", options)
    let encodedC = JsonSerializer.Serialize(T1.C true, options)

    Assert.AreEqual<string>("42", encodedA)
    Assert.AreEqual<string>("\"Hello, World!\"", encodedB)
    Assert.AreEqual<string>("true", encodedC)

  [<TestMethod>]
  member _.``encoding fails when encoder is not registered``() =

    Assert.ThrowsException<System.NotSupportedException>(fun _ ->
      JsonSerializer.Serialize(T1.A 42) |> ignore
    )
    |> ignore


  [<TestMethod>]
  member _.``Codec useDecoder registers the decoder``() =
    let options =
      JsonSerializerOptions()
      |> Codec.useDecoder(fun el ->
        Decode.oneOf
          [
            fun el -> Required.int el |> Result.map A
            fun el -> Required.string el |> Result.map B
            fun el -> Required.boolean el |> Result.map C
          ]
          el
      )

    let decodedA = JsonSerializer.Deserialize<T1>("42", options)
    let decodedB = JsonSerializer.Deserialize<T1>("\"Hello, World!\"", options)
    let decodedC = JsonSerializer.Deserialize<T1>("true", options)

    Assert.AreEqual<T1>(T1.A 42, decodedA)
    Assert.AreEqual<T1>(T1.B "Hello, World!", decodedB)
    Assert.AreEqual<T1>(T1.C true, decodedC)

  [<TestMethod>]
  member _.``decoding fails when decoder is not registered``() =

    Assert.ThrowsException<System.NotSupportedException>(fun _ ->
      JsonSerializer.Deserialize<T1>("42") |> ignore
    )
    |> ignore

  [<TestMethod>]
  member _.``Codec useCodec works to serialize and deserialize when registered``
    ()
    =
    let encoder =
      fun (x: T1) ->
        match x with
        | A a -> Encode.int a
        | B b -> Encode.string b
        | C c -> Encode.boolean c

    let decoder =
      Decode.oneOf [
        fun el -> Required.int el |> Result.map A
        fun el -> Required.string el |> Result.map B
        fun el -> Required.boolean el |> Result.map C
      ]

    let options = JsonSerializerOptions() |> Codec.useCodec(encoder, decoder)

    let encodedA = JsonSerializer.Serialize(T1.A 42, options)
    let encodedB = JsonSerializer.Serialize(T1.B "Hello, World!", options)
    let encodedC = JsonSerializer.Serialize(T1.C true, options)

    Assert.AreEqual<string>("42", encodedA)
    Assert.AreEqual<string>("\"Hello, World!\"", encodedB)
    Assert.AreEqual<string>("true", encodedC)

    let decodedA = JsonSerializer.Deserialize<T1>("42", options)
    let decodedB = JsonSerializer.Deserialize<T1>("\"Hello, World!\"", options)
    let decodedC = JsonSerializer.Deserialize<T1>("true", options)

    Assert.AreEqual<T1>(T1.A 42, decodedA)
    Assert.AreEqual<T1>(T1.B "Hello, World!", decodedB)
    Assert.AreEqual<T1>(T1.C true, decodedC)

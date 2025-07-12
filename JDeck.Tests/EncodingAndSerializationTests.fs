namespace JDeck.Tests

open System
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck

type Department = { Name: string; Location: string }

type Organization = {
  OrgName: string
  Departments: Map<string, Department>
  YearFounded: int
}

type Person = { Name: string; Age: int }

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

  [<TestMethod>]
  member _.``Encode.map can encode IDictionary<'K,'V> with the right encoder``() =
    let map = System.Collections.Generic.Dictionary<string, int>()
    map.Add("one", 1)
    map.Add("two", 2)
    map.Add("three", 3)

    let encoder = fun (key, value) -> key, Encode.int value

    let encoded = Encode.map(map, encoder) |> _.ToJsonString()

    let expected = "{\"one\":1,\"two\":2,\"three\":3}"

    Assert.AreEqual<string>(expected, encoded)

  [<TestMethod>]
  member _.``Encode.map can encode an F# map with the right encoder``() =
    let map = Map.ofList [ ("one", 1); ("two", 2); ("three", 3) ]

    let encoder = fun (key, value) -> key, Encode.int value

    let encoded = Encode.map(map, encoder) |> _.ToJsonString()

    let expected = "{\"one\":1,\"three\":3,\"two\":2}"

    Assert.AreEqual<string>(expected, encoded)

  [<TestMethod>]
  member _.``Encode.map can encode maps with complex objects``() =

    let people = System.Collections.Generic.Dictionary<string, Person>()
    people.Add("person1", { Name = "John"; Age = 30 })
    people.Add("person2", { Name = "Jane"; Age = 25 })

    let personEncoder =
      fun (key, person) ->
        key,
        Json.empty()
        |> Encode.property("name", Encode.string person.Name)
        |> Encode.property("age", Encode.int person.Age)
        :> JsonNode

    let encoded = Encode.map(people, personEncoder) |> _.ToJsonString()

    let expected =
      "{\"person1\":{\"name\":\"John\",\"age\":30},\"person2\":{\"name\":\"Jane\",\"age\":25}}"

    Assert.AreEqual<string>(expected, encoded)

  [<TestMethod>]
  member _.``Pipeline style can encode maps within complex objects``() =
    // Create test data
    let departments =
      Map.ofList [
        ("engineering",
         {
           Name = "Engineering"
           Location = "Building A"
         })
        ("marketing",
         {
           Name = "Marketing"
           Location = "Building B"
         })
      ]

    let organization = {
      OrgName = "Acme Corp"
      Departments = departments
      YearFounded = 1985
    }

    // Create department encoder using pipeline style
    let departmentEncoder =
      fun (key, dept: Department) ->
        key,
        Json.empty()
        |> Encode.property("name", Encode.string dept.Name)
        |> Encode.property("location", Encode.string dept.Location)
        :> JsonNode

    // Encode the entire organization using pipeline style
    let encoded =
      Json.empty()
      |> Encode.property("name", Encode.string organization.OrgName)
      |> Encode.property("yearFounded", Encode.int organization.YearFounded)
      |> Encode.property(
        "departments",
        Encode.map(organization.Departments, departmentEncoder)
      )
      |> _.ToJsonString()

    let expected =
      "{\"name\":\"Acme Corp\",\"yearFounded\":1985,\"departments\":{\"engineering\":{\"name\":\"Engineering\",\"location\":\"Building A\"},\"marketing\":{\"name\":\"Marketing\",\"location\":\"Building B\"}}}"

    Assert.AreEqual<string>(expected, encoded)

  [<TestMethod>]
  member _.``Object list style can encode maps within complex objects``() =
    // Create test data
    let departments =
      Map.ofList [
        ("engineering",
         {
           Name = "Engineering"
           Location = "Building A"
         })
        ("marketing",
         {
           Name = "Marketing"
           Location = "Building B"
         })
      ]

    let organization = {
      OrgName = "Acme Corp"
      Departments = departments
      YearFounded = 1985
    }
    // Encode the entire organization using the object list style
    let encoded =
      Json.object [
        "name", Encode.string organization.OrgName
        "yearFounded", Encode.int organization.YearFounded
        "departments",
        Encode.map(
          organization.Departments,
          fun (key, dept: Department) ->
            key,
            Json.object [
              "name", Encode.string dept.Name
              "location", Encode.string dept.Location
            ]
            :> JsonNode
        )
      ]
      |> _.ToJsonString()

    let expected =
      "{\"name\":\"Acme Corp\",\"yearFounded\":1985,\"departments\":{\"engineering\":{\"name\":\"Engineering\",\"location\":\"Building A\"},\"marketing\":{\"name\":\"Marketing\",\"location\":\"Building B\"}}}"

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

    Assert.ThrowsException<NotSupportedException>(fun _ ->
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

    Assert.ThrowsException<NotSupportedException>(fun _ ->
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

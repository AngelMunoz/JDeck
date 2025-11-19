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
  member _.``Encode can encode a TimeSpan``() =
    let timeSpan = TimeSpan(1, 2, 3)
    let encoded = Encode.timeSpan timeSpan
    let expected = "\"01:02:03\""

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
  member _.``Encode.map can encode IDictionary<'K,'V> with the right encoder``
    ()
    =
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

module OptionsAwareHelpers =
  // For the options-aware decoder test
  type Comp =
    | Str of string
    | Num of int

  let compDecoder: Decoder<Comp> =
    fun el ->
      Decode.oneOf
        [
          (Required.string >> Result.map Str)
          (Required.int >> Result.map Num)
        ]
        el

  type Container = { Name: string; Components: Comp array }

  let containerDecoderFactory
    (opts: JsonSerializerOptions)
    : Decoder<Container> =
    fun el -> decode {
      let! name = el |> Required.Property.get("name", Required.string)

      and! comps =
        el |> Required.Property.array("components", Decode.autoJsonOptions opts)

      return { Name = name; Components = comps }
    }

  // For the options-aware codec test
  type W = { V: int }

  let wEncFactory (_opts: JsonSerializerOptions) : Encoder<W> =
    fun w -> Json.empty() |> Encode.property("v", Encode.int w.V) :> JsonNode

  let wDecFactory (_opts: JsonSerializerOptions) : Decoder<W> =
    fun el -> decode {
      let! v = el |> Required.Property.get("v", Required.int)
      return { V = v }
    }

  [<TestClass>]
  type OptionsAwareTests() =
    [<TestMethod>]
    member _.``useDecoderWithOptions allows nested autoJsonOptions to see registered decoders``
      ()
      =
      let options =
        JsonSerializerOptions()
        |> Codec.useDecoder compDecoder
        |> Codec.useDecoderWithOptions containerDecoderFactory

      let json = """{"name":"box","components":["a",1,"b",2]}"""
      let decoded = JsonSerializer.Deserialize<Container>(json, options)

      Assert.AreEqual<string>("box", decoded.Name)
      Assert.AreEqual<int>(4, decoded.Components.Length)

      match decoded.Components with
      | [| Str "a"; Num 1; Str "b"; Num 2 |] -> ()
      | other -> Assert.Fail($"Unexpected components: %A{other}")

    [<TestMethod>]
    member _.``useCodecWithOptions registers both factory sides``() =
      let options =
        JsonSerializerOptions()
        |> Codec.useCodecWithOptions(wEncFactory, wDecFactory)

      let str = JsonSerializer.Serialize({ V = 7 }, options)
      Assert.AreEqual<string>("{\"v\":7}", str)

      let round = JsonSerializer.Deserialize<W>(str, options)
      Assert.AreEqual<int>(7, round.V)

  // Deeply nested decoders scenario similar to the user report
  module DeepNested =
    type Cover = { Id: string }
    type Deduct = { Amount: int }
    type Feed = { Port: string }
    type Component = { Kind: string; Value: int option }

    type Raceway = {
      RunNumber: int
      Components: Component array
      VerticalRuns: Map<string, Component>
    }

    type WallboxFitting = {
      Sequence: int
      ItemId: string
      Covers: Cover array
      LeftDeducts: Deduct array
      RightDeducts: Deduct array
      IsUp: bool
      Feeds: Feed array
    }

    // Leaf decoders (registered individually)
    let coverDecoder: Decoder<Cover> =
      fun el -> decode {
        let! id = el |> Required.Property.get("id", Required.string)
        return { Id = id }
      }

    let deductDecoder: Decoder<Deduct> =
      fun el -> decode {
        let! amt = el |> Required.Property.get("amount", Required.int)
        return { Amount = amt }
      }

    let feedDecoder: Decoder<Feed> =
      fun el -> decode {
        let! p = el |> Required.Property.get("port", Required.string)
        return { Port = p }
      }

    let componentDecoder: Decoder<Component> =
      fun el -> decode {
        let! kind = el |> Required.Property.get("kind", Required.string)
        and! value = el |> Optional.Property.get("value", Required.int)
        return { Kind = kind; Value = value }
      }

    // Factories that compose via options
    let racewayDecoderFactory (opts: JsonSerializerOptions) : Decoder<Raceway> =
      fun el -> decode {
        let! run = el |> Required.Property.get("RunNumber", Required.int)

        and! comps =
          el |> Required.Property.get("Components", Decode.autoJsonOptions opts)

        and! vRuns =
          el
          |> Optional.Property.get("VerticalRuns", Decode.autoJsonOptions opts)

        return {
          RunNumber = run
          Components = comps
          VerticalRuns = vRuns |> Option.defaultValue Map.empty
        }
      }

    let wallboxFittingDecoderFactory
      (opts: JsonSerializerOptions)
      : Decoder<WallboxFitting> =
      fun el -> decode {
        let! seq = el |> Required.Property.get("Sequence", Required.int)
        and! item = el |> Required.Property.get("ItemId", Required.string)

        and! covers =
          el |> Required.Property.array("Covers", Decode.autoJsonOptions opts)

        and! left =
          el
          |> Required.Property.array("LeftDeducts", Decode.autoJsonOptions opts)

        and! right =
          el
          |> Required.Property.array(
            "RightDeducts",
            Decode.autoJsonOptions opts
          )

        and! isUp = el |> Required.Property.get("IsUp", Required.boolean)

        and! feeds =
          el |> Optional.Property.array("Feeds", Decode.autoJsonOptions opts)

        return {
          Sequence = seq
          ItemId = item
          Covers = covers
          LeftDeducts = left
          RightDeducts = right
          IsUp = isUp
          Feeds = feeds |> Option.defaultValue [||]
        }
      }

    [<TestClass>]
    type DeepNestedTests() =
      [<TestMethod>]
      member _.``Factory decoder composes multiple registered decoders for arrays and maps``
        ()
        =
        // Register leaf decoders first
        let options =
          JsonSerializerOptions()
          |> Codec.useDecoder coverDecoder
          |> Codec.useDecoder deductDecoder
          |> Codec.useDecoder feedDecoder
          |> Codec.useDecoder componentDecoder
          |> Codec.useDecoderWithOptions racewayDecoderFactory

        let json =
          """{
            "RunNumber": 7,
            "Components": [ { "kind": "k1", "value": 10 }, { "kind": "k2" } ],
            "VerticalRuns": { "VR1": { "kind": "vkind", "value": 1 } }
          }"""

        let raceway = JsonSerializer.Deserialize<Raceway>(json, options)
        Assert.AreEqual<int>(7, raceway.RunNumber)
        Assert.AreEqual<int>(2, raceway.Components.Length)

        match raceway.Components with
        | [| { Kind = "k1"; Value = Some 10 }; { Kind = "k2"; Value = None } |] ->
          ()
        | other -> Assert.Fail($"Unexpected components: %A{other}")

        Assert.AreEqual<int>(1, raceway.VerticalRuns.Count)
        let vr1 = raceway.VerticalRuns["VR1"]
        Assert.AreEqual<string>("vkind", vr1.Kind)
        Assert.AreEqual<int option>(Some 1, vr1.Value)

      [<TestMethod>]
      member _.``Factory decoder composes nested decoders with required and optional arrays``
        ()
        =
        let options =
          JsonSerializerOptions()
          |> Codec.useDecoder coverDecoder
          |> Codec.useDecoder deductDecoder
          |> Codec.useDecoder feedDecoder
          |> Codec.useDecoder componentDecoder
          |> Codec.useDecoderWithOptions wallboxFittingDecoderFactory

        let json =
          """{
            "Sequence": 1,
            "ItemId": "WB-1",
            "Covers": [ { "id": "C1" }, { "id": "C2" } ],
            "LeftDeducts": [ { "amount": 2 } ],
            "RightDeducts": [ { "amount": 3 } ],
            "IsUp": true,
            "Feeds": [ { "port": "P1" } ]
          }"""

        let wb = JsonSerializer.Deserialize<WallboxFitting>(json, options)
        Assert.AreEqual<int>(1, wb.Sequence)
        Assert.AreEqual<string>("WB-1", wb.ItemId)
        Assert.AreEqual<int>(2, wb.Covers.Length)
        Assert.AreEqual<string>("C1", wb.Covers.[0].Id)
        Assert.AreEqual<int>(1, wb.LeftDeducts.Length)
        Assert.AreEqual<int>(2, wb.LeftDeducts.[0].Amount)
        Assert.AreEqual<int>(1, wb.RightDeducts.Length)
        Assert.AreEqual<int>(3, wb.RightDeducts.[0].Amount)
        Assert.IsTrue(wb.IsUp)
        Assert.AreEqual<int>(1, wb.Feeds.Length)
        Assert.AreEqual<string>("P1", wb.Feeds.[0].Port)

  [<TestClass>]
  type SerializationTests_Continuation() =
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

      let decodedB =
        JsonSerializer.Deserialize<T1>("\"Hello, World!\"", options)

      let decodedC = JsonSerializer.Deserialize<T1>("true", options)

      Assert.AreEqual<T1>(T1.A 42, decodedA)
      Assert.AreEqual<T1>(T1.B "Hello, World!", decodedB)
      Assert.AreEqual<T1>(T1.C true, decodedC)

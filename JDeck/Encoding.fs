namespace JDeck

open System
open System.Collections.Generic
open System.Text.Json.Nodes

type Encoder<'T> = 'T -> JsonNode
type MapEntryEncoder<'Key, 'Value> = 'Key * 'Value -> string  * JsonNode

[<AutoOpen>]
module Encoding =

  module Encode =
    let inline Null(): JsonNode = null

    let inline string (value: string) = JsonValue.Create(value) :> JsonNode

    let inline boolean (value: bool) = JsonValue.Create(value) :> JsonNode

    let inline char (value: char) = JsonValue.Create(value) :> JsonNode

    let inline guid (value: Guid) = JsonValue.Create(value) :> JsonNode

    let inline byte (value: byte) = JsonValue.Create(value) :> JsonNode

    let inline int (value: int) = JsonValue.Create(value) :> JsonNode

    let inline int64 (value: int64) = JsonValue.Create(value) :> JsonNode

    let inline float (value: float) = JsonValue.Create(value) :> JsonNode

    let inline dateTime (value: DateTime) =
      JsonValue.Create(value.ToString("o")) :> JsonNode

    let inline dateTimeOffset (value: DateTimeOffset) =
      JsonValue.Create(value.ToString("o")) :> JsonNode

    let inline timeSpan (value: TimeSpan) =
      JsonValue.Create(value.ToString()) :> JsonNode

    let inline dateTimeExact (format: string) (value: DateTime) =
      JsonValue.Create(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)) :> JsonNode

    let inline dateTimeOffsetExact (format: string) (value: DateTimeOffset) =
      JsonValue.Create(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)) :> JsonNode

    let inline timeSpanExact (format: string) (value: TimeSpan) =
      JsonValue.Create(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)) :> JsonNode

    let inline dateTimeExactWith (format: string) (provider: IFormatProvider) (value: DateTime) =
      JsonValue.Create(value.ToString(format, provider)) :> JsonNode

    let inline dateTimeOffsetExactWith (format: string) (provider: IFormatProvider) (value: DateTimeOffset) =
      JsonValue.Create(value.ToString(format, provider)) :> JsonNode

    let inline timeSpanExactWith (format: string) (provider: IFormatProvider) (value: TimeSpan) =
      JsonValue.Create(value.ToString(format, provider)) :> JsonNode

    let inline property
      (name: string, value: JsonNode)
      (jsonObject: JsonObject)
      =
      jsonObject.Add(name, value)
      jsonObject

    let inline sequence
      (values: 'T seq, encoder: Encoder<'T>)
      (jsonArray: JsonArray)
      =
      values |> Seq.iter(fun value -> jsonArray.Add(encoder value))
      jsonArray :> JsonNode

    let inline map<'Key, 'Value>
      (values: IDictionary<'Key, 'Value>, encoder: MapEntryEncoder<'Key, 'Value>)
      =
      let obj = JsonObject()
      for KeyValue(key, value) in values do
        let key, value = encoder (key, value)
        obj.Add(key, value)

      obj


  type Json =
    static member inline empty() = JsonObject.Parse("{}").AsObject()

    static member inline object(values: (string * JsonNode) seq) =
      let node = JsonObject.Parse("{}").AsObject()
      values |> Seq.iter(node.Add)
      node

    static member inline object(values: KeyValuePair<string, JsonNode> seq) =
      let node = JsonObject.Parse("{}").AsObject()
      values |> Seq.iter(fun (KeyValue(key, value)) -> node.Add(key, value))
      node

    static member inline sequence(values: 'T seq, encoder: Encoder<'T>) =
      (Encode.sequence (values, encoder) (JsonArray()))

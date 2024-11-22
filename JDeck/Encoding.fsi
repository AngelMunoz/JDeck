namespace JDeck

open System
open System.Collections.Generic
open System.Text.Json.Nodes

type Encoder<'T> = 'T -> JsonNode

[<AutoOpen>]
module Encoding =
  /// <summary>Provides functions for encoding values to JSON nodes.</summary>
  module Encode =
    val inline Null: unit -> JsonNode
    val inline string: value: string -> JsonNode
    val inline boolean: value: bool -> JsonNode
    val inline char: value: char -> JsonNode
    val inline guid: value: Guid -> JsonNode
    val inline byte: value: byte -> JsonNode
    val inline int: value: int -> JsonNode
    val inline int64: value: int64 -> JsonNode
    val inline float: value: float -> JsonNode
    val inline dateTime: value: DateTime -> JsonNode
    val inline dateTimeOffset: value: DateTimeOffset -> JsonNode
    val inline property: name: string * value: JsonNode -> jsonObject: JsonObject -> JsonObject
    val inline sequence: values: 'T seq * encoder: Encoder<'T> -> jsonArray: JsonArray -> JsonNode

  /// <summary>Provides functions for creating JSON nodes.</summary>
  [<Class>]
  type Json =
    /// <summary>Creates an empty JSON object.</summary>
    static member inline empty: unit -> JsonObject
    /// <summary>Creates a JSON object with the provided sequence.</summary>
    static member inline object: values: (string * JsonNode) seq -> JsonObject
    /// <summary>Creates a JSON object from a sequence of key-value pairs.</summary>
    static member inline object: values: KeyValuePair<string, JsonNode> seq -> JsonObject
    /// <summary>Creates a JsonArray from a sequence of values and an encoder.</summary>
    static member inline sequence: values: 'T seq * encoder: Encoder<'T> -> JsonNode

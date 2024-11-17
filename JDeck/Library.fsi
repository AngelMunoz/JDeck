namespace JDeck

open System
open System.IO
open System.Text.Json

type DecodeError =
  { value: JsonElement
    kind: JsonValueKind
    rawValue: string
    targetType: Type
    message: string
    exn: exn option
    index: int option
    property: string option }

module DecodeError =
  val inline ofError<'TResult> : el: JsonElement * message: string -> DecodeError
  val inline ofIndexed<'TResult> : el: JsonElement * index: int * message: string -> DecodeError
  val withIndex: i: int -> error: DecodeError -> DecodeError
  val withProperty: name: string -> error: DecodeError -> DecodeError
  val withException: ex: exn -> error: DecodeError -> DecodeError
  val withMessage: message: string -> error: DecodeError -> DecodeError

type Decoder<'TResult> = JsonElement -> Result<'TResult, DecodeError>
type IndexedDecoder<'TResult> = int -> JsonElement -> Result<'TResult, DecodeError>
type ValidationDecoder<'TResult> = JsonElement -> Result<'TResult, DecodeError list>

module Decode =
  val inline sequence:
    [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult seq, DecodeError>

  val inline seqTraverse:
    [<InlineIfLambda>] decoder: (int -> Decoder<'TResult>) -> el: JsonElement -> Result<'TResult seq, DecodeError list>

  val inline array:
    [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult array, DecodeError>

  val inline arrayTraverse:
    [<InlineIfLambda>] decoder: (int -> Decoder<'TResult>) ->
    el: JsonElement ->
      Result<'TResult array, DecodeError list>

  val inline list:
    [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult list, DecodeError>

  val inline listTraverse:
    [<InlineIfLambda>] decoder: (int -> Decoder<'TResult>) -> el: JsonElement -> Result<'TResult list, DecodeError list>

  module Required =

    val string: Decoder<string>
    val boolean: Decoder<bool>
    val char: Decoder<char>
    val guid: Decoder<Guid>
    val unit: Decoder<unit>
    val byte: Decoder<byte>
    val int: Decoder<int>
    val int64: Decoder<int64>
    val float: Decoder<float>
    val dateTime: Decoder<DateTime>
    val dateTimeOffset: Decoder<DateTimeOffset>

    val inline property:
      name: string ->
      [<InlineIfLambda>] decoder: (JsonElement -> Result<'TResult, DecodeError>) ->
      element: JsonElement ->
        Result<'TResult, DecodeError>

  module Optional =

    val string: Decoder<string option>
    val boolean: Decoder<bool option>
    val char: Decoder<char option>
    val guid: Decoder<Guid option>
    val unit: Decoder<unit option>
    val byte: Decoder<byte option>
    val int: Decoder<int option>
    val int64: Decoder<int64 option>
    val float: Decoder<float option>
    val dateTime: Decoder<DateTime option>
    val dateTimeOffset: Decoder<DateTimeOffset option>

    val inline property:
      name: string ->
      [<InlineIfLambda>] decoder: Decoder<'TResult> ->
      element: JsonElement ->
        Result<'TResult voption, DecodeError>

[<Class>]
type Decode =
  static member inline fromString:
    value: string * options: JsonDocumentOptions * decoder: Decoder<'TResult> -> Result<'TResult, DecodeError>

  static member inline fromString: value: string * decoder: Decoder<'TResult> -> Result<'TResult, DecodeError>

  static member inline fromBytes:
    value: byte array * options: JsonDocumentOptions * decoder: (JsonElement -> 'TResult) -> 'TResult

  static member inline fromBytes: value: byte array * decoder: Decoder<'TResult> -> Result<'TResult, DecodeError>

  static member inline fromStream:
    value: Stream * options: JsonDocumentOptions * decoder: (JsonElement -> 'TResult) -> Threading.Tasks.Task<'TResult>

  static member inline fromStream:
    value: Stream * decoder: Decoder<'TResult> -> Threading.Tasks.Task<Result<'TResult, DecodeError>>

  static member inline validateFromString:
    value: string * options: JsonDocumentOptions * decoder: ValidationDecoder<'TResult> ->
      Result<'TResult, DecodeError list>

  static member inline validateFromString:
    value: string * decoder: ValidationDecoder<'TResult> -> Result<'TResult, DecodeError list>

  static member inline validateFromBytes:
    value: byte array * options: JsonDocumentOptions * decoder: ValidationDecoder<'TResult> ->
      Result<'TResult, DecodeError list>

  static member inline validateFromBytes:
    value: byte array * decoder: ValidationDecoder<'TResult> -> Result<'TResult, DecodeError list>

  static member inline validateFromStream:
    value: Stream * options: JsonDocumentOptions * decoder: ValidationDecoder<'TResult> ->
      Threading.Tasks.Task<Result<'TResult, DecodeError list>>

  static member inline validateFromStream:
    value: Stream * decoder: ValidationDecoder<'TResult> -> Threading.Tasks.Task<Result<'TResult, DecodeError list>>

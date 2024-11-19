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
type CollectErrorsDecoder<'TResult> = JsonElement -> Result<'TResult, DecodeError list>
type IndexedCollectErrorsDecoder<'TResult> = int -> JsonElement -> Result<'TResult, DecodeError list>

[<AutoOpen>]
module Decode =
  module Decode =
    val inline sequence:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult seq, DecodeError>

    val inline sequenceCol:
      [<InlineIfLambda>] decoder: IndexedCollectErrorsDecoder<'a> -> el: JsonElement -> Result<'a seq, DecodeError list>

    val inline array:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult array, DecodeError>

    val inline list:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult list, DecodeError>

    val oneOf: decoders: Decoder<'TResult> seq -> Decoder<'TResult>

    val collectOneOf: decoders: Decoder<'TResult> seq -> element: JsonElement -> Result<'TResult, DecodeError list>

    val inline auto: el: JsonElement -> Result<'TResult, DecodeError>

    val inline autoJsonOptions: options: JsonSerializerOptions -> el: JsonElement -> Result<'TResult, DecodeError>

    val useDecoder: decoder: Decoder<'TResult> -> options: JsonSerializerOptions -> JsonSerializerOptions

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

    [<Class>]
    type Property =
      static member inline get:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult, DecodeError>)

      static member inline get:
        name: string * decoder: CollectErrorsDecoder<'TResult> -> (JsonElement -> Result<'TResult, DecodeError list>)

      static member inline seq:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult seq, DecodeError>)

      static member inline seq:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult seq, DecodeError list>)

      static member inline list:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult list, DecodeError>)

      static member inline list:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult list, DecodeError list>)

      static member inline array:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult array, DecodeError>)

      static member inline array:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult array, DecodeError list>)

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

    [<Class>]
    type Property =
      static member inline get:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult option, DecodeError>)

      static member inline get:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult option, DecodeError list>)

      static member inline seq:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult seq option, DecodeError>)

      static member inline seq:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult seq option, DecodeError list>)

      static member inline list:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult list option, DecodeError>)

      static member inline list:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult list option, DecodeError list>)

      static member inline array:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult array option, DecodeError>)

      static member inline array:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult array option, DecodeError list>)

[<AutoOpen>]
module Builders =
  [<Class>]
  type DecodeBuilder =
    member inline Return: value: 'TResult -> Result<'TResult, DecodeError>

    member inline ReturnFrom: value: Result<'TResult, DecodeError> -> Result<'TResult, DecodeError>

    member inline Bind:
      value: Result<'TValue, DecodeError> * [<InlineIfLambda>] f: ('TValue -> Result<'TResult, DecodeError>) ->
        Result<'TResult, DecodeError>

    member inline Zero: unit -> Result<unit, DecodeError>

    member inline Delay:
      [<InlineIfLambda>] generator: (unit -> Result<'TValue, DecodeError>) -> (unit -> Result<'TValue, DecodeError>)

    member inline Run:
      [<InlineIfLambda>] generator: (unit -> Result<'TResult, DecodeError>) -> Result<'TResult, DecodeError>

    member inline Combine:
      value: Result<'TValue, DecodeError> * [<InlineIfLambda>] f: ('TValue -> Result<'TResult, DecodeError>) ->
        Result<'TResult, DecodeError>

    member inline TryFinally:
      [<InlineIfLambda>] generator: (unit -> Result<'TResult, DecodeError>) *
      [<InlineIfLambda>] compensation: (unit -> unit) ->
        Result<'TResult, DecodeError>

    member inline Using<'disposable, 'TResult when 'disposable :> IDisposable> :
      resource: 'disposable * [<InlineIfLambda>] binder: ('disposable -> Result<'TResult, DecodeError>) ->
        Result<'TResult, DecodeError>

    member inline BindReturn:
      value: Result<'TValue, DecodeError> * [<InlineIfLambda>] f: ('TValue -> 'TResult) -> Result<'TResult, DecodeError>

    member inline MergeSources:
      r1: Result<'TValue1, 'error> * r2: Result<'TValue2, 'error> -> Result<'TValue1 * 'TValue2, 'error>

    member inline MergeSources3:
      r1: Result<'TValue1, 'error> * r2: Result<'TValue2, 'error> * r3: Result<'TValue3, 'error> ->
        Result<'TValue1 * 'TValue2 * 'TValue3, 'error>

    member inline MergeSources4:
      r1: Result<'TValue1, 'error> *
      r2: Result<'TValue2, 'error> *
      r3: Result<'TValue3, 'error> *
      r4: Result<'TValue4, 'error> ->
        Result<'TValue1 * 'TValue2 * 'TValue3 * 'TValue4, 'error>

    member inline MergeSources5:
      r1: Result<'TValue1, 'error> *
      r2: Result<'TValue2, 'error> *
      r3: Result<'TValue3, 'error> *
      r4: Result<'TValue4, 'error> *
      r5: Result<'TValue5, 'error> ->
        Result<'TValue1 * 'TValue2 * 'TValue3 * 'TValue4 * 'TValue5, 'error>

    member inline Source: result: Result<'TResult, DecodeError> -> Result<'TResult, DecodeError>

  val decode: DecodeBuilder

[<Class>]
type Decode =
  static member inline auto: json: string * ?docOptions: JsonDocumentOptions -> Result<'TResult, DecodeError>

  static member inline auto:
    json: string * options: JsonSerializerOptions * ?docOptions: JsonDocumentOptions -> Result<'TResult, DecodeError>

  static member inline auto: json: byte array * ?docOptions: JsonDocumentOptions -> Result<'TResult, DecodeError>

  static member inline auto:
    json: byte array * options: JsonSerializerOptions * ?docOptions: JsonDocumentOptions ->
      Result<'TResult, DecodeError>

  static member inline auto:
    json: Stream * ?docOptions: JsonDocumentOptions -> System.Threading.Tasks.Task<Result<'TResult, DecodeError>>

  static member inline auto:
    json: Stream * options: JsonSerializerOptions * ?docOptions: JsonDocumentOptions ->
      System.Threading.Tasks.Task<Result<'TResult, DecodeError>>

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

  static member inline fromStringCol:
    value: string * options: JsonDocumentOptions * decoder: CollectErrorsDecoder<'TResult> ->
      Result<'TResult, DecodeError list>

  static member inline fromStringCol:
    value: string * decoder: CollectErrorsDecoder<'TResult> -> Result<'TResult, DecodeError list>

  static member inline fromBytesCol:
    value: byte array * options: JsonDocumentOptions * decoder: CollectErrorsDecoder<'TResult> ->
      Result<'TResult, DecodeError list>

  static member inline fromBytesCol:
    value: byte array * decoder: CollectErrorsDecoder<'TResult> -> Result<'TResult, DecodeError list>

  static member inline fromStreamCol:
    value: Stream * options: JsonDocumentOptions * decoder: CollectErrorsDecoder<'TResult> ->
      Threading.Tasks.Task<Result<'TResult, DecodeError list>>

  static member inline fromStreamCol:
    value: Stream * decoder: CollectErrorsDecoder<'TResult> -> Threading.Tasks.Task<Result<'TResult, DecodeError list>>

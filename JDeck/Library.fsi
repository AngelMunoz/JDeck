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

[<AutoOpen>]
module Decode =
  module Decode =
    val inline sequence:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult seq, DecodeError>

    val inline array:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult array, DecodeError>

    val inline list:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult list, DecodeError>

    val oneOf: decoders: Decoder<'TResult> seq -> Decoder<'TResult>

    val collectOneOf: decoders: Decoder<'TResult> seq -> element: JsonElement -> Result<'TResult, DecodeError list>

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

    val inline seqProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError>) ->
      JsonElement ->
        Result<'TResult seq, DecodeError>

    val inline listProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError>) ->
      JsonElement ->
        Result<'TResult list, DecodeError>

    val inline arrayProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError>) ->
      JsonElement ->
        Result<'TResult array, DecodeError>

    val inline collectSeqProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError list>) ->
      JsonElement ->
        Result<'TResult seq, DecodeError list>

    val inline collectListProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError list>) ->
      JsonElement ->
        Result<'TResult list, DecodeError list>

    val inline collectArrayProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError list>) ->
      JsonElement ->
        Result<'TResult array, DecodeError list>

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
        Result<'TResult option, DecodeError>

    val inline seqProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError>) ->
      JsonElement ->
        Result<'TResult seq option, DecodeError>

    val inline listProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError>) ->
      JsonElement ->
        Result<'TResult list option, DecodeError>

    val inline arrayProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError>) ->
      JsonElement ->
        Result<'TResult array option, DecodeError>

    val inline collectSeqProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError list>) ->
      JsonElement ->
        Result<'TResult seq option, DecodeError list>

    val inline collectListProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError list>) ->
      JsonElement ->
        Result<'TResult list option, DecodeError list>

    val inline collectArrayProperty:
      name: string ->
      [<InlineIfLambda>] decoder: (int -> JsonElement -> Result<'TResult, DecodeError list>) ->
      JsonElement ->
        Result<'TResult array option, DecodeError list>

module Builders =
  [<Class>]
  type DecodeBuilder =
    member inline Bind:
      value: Result<'TValue, DecodeError> * [<InlineIfLambda>] f: ('TValue -> Result<'TResult, DecodeError>) ->
        Result<'TResult, DecodeError>

    member inline Source: result: Result<'TResult, DecodeError> -> Result<'TResult, DecodeError>

    member inline Return: value: 'TResult -> Result<'TResult, DecodeError>

    member inline ReturnFrom: value: Result<'TResult, DecodeError> -> Result<'TResult, DecodeError>

    member inline BindReturn:
      value: Result<'TValue, DecodeError> * [<InlineIfLambda>] f: ('TValue -> 'TResult) -> Result<'TResult, DecodeError>

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

  type ResultCollect<'ok, 'error> = Result<'ok, 'error list>

  [<Class>]
  type DecodeCollectBuilder =
    member inline Bind:
      value: ResultCollect<'TValue, DecodeError> *
      [<InlineIfLambda>] f: ('TValue -> ResultCollect<'TResult, DecodeError>) ->
        ResultCollect<'TResult, DecodeError>

    member inline Source: result: ResultCollect<'TResult, DecodeError> -> ResultCollect<'TResult, DecodeError>

    member inline Return: value: 'TResult -> ResultCollect<'TResult, DecodeError>

    member inline ReturnFrom: value: ResultCollect<'TResult, DecodeError> -> ResultCollect<'TResult, DecodeError>

    member inline BindReturn:
      value: ResultCollect<'TValue, DecodeError> * [<InlineIfLambda>] f: ('TValue -> 'TResult) ->
        ResultCollect<'TResult, DecodeError>

    member inline Zero: unit -> ResultCollect<unit, DecodeError>

    member inline Delay:
      [<InlineIfLambda>] generator: (unit -> ResultCollect<'TValue, DecodeError>) ->
        (unit -> ResultCollect<'TValue, DecodeError>)

    member inline Run:
      [<InlineIfLambda>] generator: (unit -> ResultCollect<'TResult, DecodeError>) ->
        ResultCollect<'TResult, DecodeError>

    member inline Combine:
      value: ResultCollect<'TValue, DecodeError> *
      [<InlineIfLambda>] f: ('TValue -> ResultCollect<'TResult, DecodeError>) ->
        ResultCollect<'TResult, DecodeError>

    member inline TryFinally:
      [<InlineIfLambda>] generator: (unit -> ResultCollect<'TResult, DecodeError>) *
      [<InlineIfLambda>] compensation: (unit -> unit) ->
        ResultCollect<'TResult, DecodeError>

    member inline Using<'disposable, 'TResult when 'disposable :> IDisposable> :
      resource: 'disposable * [<InlineIfLambda>] binder: ('disposable -> ResultCollect<'TResult, DecodeError>) ->
        ResultCollect<'TResult, DecodeError>

    member inline MergeSources:
      r1: ResultCollect<'TValue1, 'error> * r2: ResultCollect<'TValue2, 'error> ->
        ResultCollect<'TValue1 * 'TValue2, 'error>

    member inline MergeSources3:
      r1: ResultCollect<'TValue1, 'error> * r2: ResultCollect<'TValue2, 'error> * r3: ResultCollect<'TValue3, 'error> ->
        ResultCollect<'TValue1 * 'TValue2 * 'TValue3, 'error>

    member inline MergeSources4:
      r1: ResultCollect<'TValue1, 'error> *
      r2: ResultCollect<'TValue2, 'error> *
      r3: ResultCollect<'TValue3, 'error> *
      r4: ResultCollect<'TValue4, 'error> ->
        ResultCollect<'TValue1 * 'TValue2 * 'TValue3 * 'TValue4, 'error>

    member inline MergeSources5:
      r1: ResultCollect<'TValue1, 'error> *
      r2: ResultCollect<'TValue2, 'error> *
      r3: ResultCollect<'TValue3, 'error> *
      r4: ResultCollect<'TValue4, 'error> *
      r5: ResultCollect<'TValue5, 'error> ->
        ResultCollect<'TValue1 * 'TValue2 * 'TValue3 * 'TValue4 * 'TValue5, 'error>

  val decode: DecodeBuilder
  val decodeCollect: DecodeCollectBuilder

  [<AutoOpen>]
  module BuilderExtensions =

    type DecodeCollectBuilder with
      member inline Source: result: Result<'TResult, DecodeError> -> ResultCollect<'TResult, DecodeError>

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

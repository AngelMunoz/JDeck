namespace JDeck

open System
open System.IO
open System.Text.Json

/// In case of failure when a type is being decoded,
/// this type is meant to contain the relevant information about the error.
type DecodeError =
  {
    /// <summary>
    /// The value that was being decoded when the error occurred.
    /// </summary>
    /// <remarks>
    /// Please be sure to "clone" this value by calling `JsonElement.Clone()` before using it.
    /// otherwise you could run into the issue of trying to access an already disposed value.
    /// </remarks>
    value: JsonElement
    kind: JsonValueKind
    rawValue: string
    targetType: Type
    message: string
    exn: exn option
    index: int option
    property: string option
  }

module DecodeError =
  val inline ofError<'TResult> : el: JsonElement * message: string -> DecodeError
  val inline ofIndexed<'TResult> : el: JsonElement * index: int * message: string -> DecodeError
  val withIndex: i: int -> error: DecodeError -> DecodeError
  val withProperty: name: string -> error: DecodeError -> DecodeError
  val withException: ex: exn -> error: DecodeError -> DecodeError
  val withMessage: message: string -> error: DecodeError -> DecodeError

type Decoder<'TResult> = JsonElement -> Result<'TResult, DecodeError>
type IndexedDecoder<'TResult> = int -> JsonElement -> Result<'TResult, DecodeError>

type IndexedMapDecoder<'TValue> = string -> JsonElement -> Result<'TValue, DecodeError>
type CollectErrorsDecoder<'TResult> = JsonElement -> Result<'TResult, DecodeError list>
type IndexedCollectErrorsDecoder<'TResult> = int -> JsonElement -> Result<'TResult, DecodeError list>
type IndexedMapCollectErrorsDecoder<'TValue> = string -> JsonElement -> Result<'TValue, DecodeError list>

[<AutoOpen>]
module Decode =
  module Decode =
    /// <summary>
    /// Decodes a JSON array element into a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// If a failure is encountered in the decoding process,
    /// the decoding stops there and the error is returned, the rest of the array is not decoded.
    /// </remarks>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline sequence:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult seq, DecodeError>

    /// <summary>
    /// Decodes a JSON array element into the value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// If a failure is encountered in the decoding process, the error is collected and the decoding continues however,
    /// the result will be an error containing a list of all the errors that occurred during the decoding process.
    /// </remarks>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline sequenceCol:
      [<InlineIfLambda>] decoder: IndexedCollectErrorsDecoder<'a> -> el: JsonElement -> Result<'a seq, DecodeError list>

    /// <summary>
    /// Decodes a JSON object into an FSharpMap where the keys are strings and the value is of type <typeparamref name="TValue"/>
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline map:
      [<InlineIfLambda>] decoder: IndexedMapDecoder<'TValue> ->
      el: JsonElement ->
        Result<Map<string, 'TValue>, DecodeError>

    /// <summary>
    /// Decodes a JSON object into an FSharpMap where the keys are strings and the value is of type <typeparamref name="TValue"/>
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline mapCol:
      [<InlineIfLambda>] decoder: IndexedMapCollectErrorsDecoder<'TValue> ->
      el: JsonElement ->
        Result<Map<string, 'TValue>, DecodeError seq>

    /// <summary>
    /// Decodes a JSON object into a BCL Dictionary where the keys are strings and the value is of type <typeparamref name="TValue"/>
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline dict:
      [<InlineIfLambda>] decoder: IndexedMapDecoder<'TValue> ->
      el: JsonElement ->
        Result<System.Collections.Generic.Dictionary<string, 'TValue>, DecodeError>

    /// <summary>
    /// Decodes a JSON object into a BCL Dictionary where the keys are strings and the value is of type <typeparamref name="TValue"/>
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline dictCol:
      [<InlineIfLambda>] decoder: IndexedMapCollectErrorsDecoder<'TValue> ->
      el: JsonElement ->
        Result<System.Collections.Generic.Dictionary<string, 'TValue>, DecodeError seq>

    /// <summary>
    /// Decodes a JSON array element into a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// If a failure is encountered in the decoding process,
    /// the decoding stops there, and the error is returned, the rest of the array is not decoded.
    /// </remarks>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline array:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult array, DecodeError>

    /// <summary>
    /// Decodes a JSON array element into a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// If a failure is encountered in the decoding process,
    /// the decoding stops there and the error is returned, the rest of the array is not decoded.
    /// </remarks>
    /// <param name="decoder"></param>
    /// <param name="el"></param>
    val inline list:
      [<InlineIfLambda>] decoder: IndexedDecoder<'TResult> -> el: JsonElement -> Result<'TResult list, DecodeError>

    /// <summary>
    /// Takes a list of possible decoders and tries to decode the JSON element with each one of them.
    /// </summary>
    /// <remarks>
    /// This is useful to decode JSON elements into discriminated unions
    /// </remarks>
    /// <param name="decoders"></param>
    val oneOf: decoders: Decoder<'TResult> seq -> Decoder<'TResult>

    /// <summary>
    /// Takes a list of possible decoders and tries to decode the JSON element with each one of them.
    /// </summary>
    /// <remarks>
    /// This is useful to decode JSON elements into discriminated unions
    /// </remarks>
    /// <param name="decoders"></param>
    /// <param name="element"></param>
    val collectOneOf: decoders: Decoder<'TResult> seq -> element: JsonElement -> Result<'TResult, DecodeError list>

    /// <summary>
    /// Attempts to decode a JSON element that is living inside an array at the given index.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="index"></param>
    /// <param name="el"></param>
    val inline decodeAt:
      [<InlineIfLambda>] decoder: Decoder<'TResult> -> index: int -> el: JsonElement -> Result<'TResult, DecodeError>

    /// <summary>
    /// Attempts to decode a JSON element that is living inside an array at the given index.
    /// </summary>
    /// <remarks>
    /// If the element is not found, this will not fail but return an option type.
    /// </remarks>
    /// <param name="decoder"></param>
    /// <param name="index"></param>
    /// <param name="el"></param>
    val inline tryDecodeAt:
      [<InlineIfLambda>] decoder: Decoder<'TResult> ->
      index: int ->
      el: JsonElement ->
        Result<'TResult option, DecodeError>

    /// <summary>
    /// Attempts to decode a JSON element that is living inside an array at the given key.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="key"></param>
    /// <param name="el"></param>
    val inline decodeAtKey:
      [<InlineIfLambda>] decoder: Decoder<'TResult> -> key: string -> el: JsonElement -> Result<'TResult, DecodeError>

    /// <summary>
    /// Attempts to decode a JSON element that is living inside an array at the given key.
    /// </summary>
    /// <remarks>
    /// If the element is not found, this will not fail but return an option type.
    /// </remarks>
    /// <param name="decoder"></param>
    /// <param name="key"></param>
    /// <param name="el"></param>
    val inline tryDecodeAtKey:
      [<InlineIfLambda>] decoder: Decoder<'TResult> ->
      key: string ->
      el: JsonElement ->
        Result<'TResult option, DecodeError>

    /// <summary>
    /// Uses the standard System.Text.Json deserialization means to deserialize a JSON element into a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// For the most part it is recommended that you use this function unless you have a "F# types based" object like discriminated unions,
    /// Which are not supported by the standard deserialization means.
    /// </remarks>
    /// <param name="el"></param>
    val inline auto: el: JsonElement -> Result<'TResult, DecodeError>

    /// <summary>
    /// Uses the standard System.Text.Json deserialization means to deserialize a JSON element into a value of type <typeparamref name="TResult"/>.
    /// You can pass JsonSerializerOptions to customize the deserialization process and even include your decoders in the process.
    /// </summary>
    /// <remarks>
    /// For the most part it is recommended that you use this function unless you have a "F# types based" object like discriminated unions,
    /// Which are not supported by the standard deserialization means.
    /// </remarks>
    /// <param name="el"></param>
    /// <param name="options"></param>
    val inline autoJsonOptions: options: JsonSerializerOptions -> el: JsonElement -> Result<'TResult, DecodeError>

    /// <summary>
    /// Customizes the serialization options and includes the given decoder.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="options"></param>
    [<Obsolete "This structure will be removed prior the v1.0 release, please use JDeck.Codec.useDecoder or JDeck.Codec.useCodec">]
    val useDecoder: decoder: Decoder<'TResult> -> options: JsonSerializerOptions -> JsonSerializerOptions

  /// <summary>
  /// Contains a set of decoders that are required to decode to the particular type otherwise the decoding will fail.
  /// </summary>
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

    /// <summary>
    /// This type containes methods that are particularly useful to decode properties from JSON elements.
    /// They can be primitive properties, objects, arrays, etc.
    /// </summary>
    /// <remarks>
    /// If the property is not found in the JSON element, the decoding will fail.
    /// </remarks>
    [<Class>]
    type Property =
      /// <summary>
      /// Takes the name of a property and a decoder and returns a function that can be used to decode the property.
      /// </summary>
      /// <remarks>
      /// The decoding process will stop at the first failure and the error will be returned.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline get:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult, DecodeError>)

      /// <summary>
      /// Takes the name of a property and a decoder and returns a function that can be used to decode the property.
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline get:
        name: string * decoder: CollectErrorsDecoder<'TResult> -> (JsonElement -> Result<'TResult, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// The decoding process will stop at the first failure and the error will be returned.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline seq:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult seq, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline seq:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult seq, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// The decoding process will stop at the first failure and the error will be returned.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline list:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult list, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline list:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult list, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// The decoding process will stop at the first failure and the error will be returned.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline array:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult array, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline array:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult array, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <remarks>
      /// The decoding process will stop at the first failure, and the error will be returned.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline map:
        name: string * decoder: Decoder<'TValue> -> (JsonElement -> Result<Map<string, 'TValue>, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline map:
        name: string * decoder: IndexedMapCollectErrorsDecoder<'TValue> ->
          (JsonElement -> Result<Map<string, 'TValue>, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      /// <remarks>The decoding process will stop at the first failure, and the error will be returned.</remarks>
      static member inline dict:
        name: string * decoder: Decoder<'TValue> ->
          (JsonElement -> Result<System.Collections.Generic.Dictionary<string, 'TValue>, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline dict:
        name: string * decoder: IndexedMapCollectErrorsDecoder<'TValue> ->
          (JsonElement -> Result<System.Collections.Generic.Dictionary<string, 'TValue>, DecodeError list>)

  /// <summary>
  /// Contains a set of decoders that are not required to decode to the particular type and will not fail.
  /// These decoders will return an option type. even if the value is null or is absent from the JSON element.
  /// </summary>
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

    /// <summary>
    /// This type containes methods that are particularly useful to decode properties from JSON elements.
    /// They can be primitive properties, objects, arrays, etc.
    /// </summary>
    /// <remarks>
    /// If the property is not found or is null in the JSON element, the decoding will return an option type.
    /// </remarks>
    [<Class>]
    type Property =
      /// <summary>
      /// Takes the name of a property and a decoder and returns a function that can be used to decode the property.
      /// </summary>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline get:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult option, DecodeError>)

      /// <summary>
      /// Takes the name of a property and a decoder and returns a function that can be used to decode the property.
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline get:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult option, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline seq:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult seq option, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline seq:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult seq option, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline list:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult list option, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline list:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult list option, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline array:
        name: string * decoder: Decoder<'TResult> -> (JsonElement -> Result<'TResult array option, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to each element in the property as if it was a JSON array
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline array:
        name: string * decoder: CollectErrorsDecoder<'TResult> ->
          (JsonElement -> Result<'TResult array option, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <remarks>
      /// The decoding process will stop at the first failure, and the error will be returned.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline map:
        name: string * decoder: Decoder<'TValue> -> (JsonElement -> Result<Map<string, 'TValue> option, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline map:
        name: string * decoder: IndexedMapCollectErrorsDecoder<'TValue> ->
          (JsonElement -> Result<Map<string, 'TValue> option, DecodeError list>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      /// <remarks>The decoding process will stop at the first failure, and the error will be returned.</remarks>
      static member inline dict:
        name: string * decoder: Decoder<'TValue> ->
          (JsonElement -> Result<System.Collections.Generic.Dictionary<string, 'TValue> option, DecodeError>)

      /// <summary>
      /// Takes a property name and applies the given decoder to the values on the properties of the object
      /// </summary>
      /// <remarks>
      /// This method will attempt to decode the type and collect all the errors that occur during the decoding process.
      /// If there's an error in the decoding process, the decoding will continue until there are no more,
      /// the returned error will contain a list of all the errors that occurred.
      /// </remarks>
      /// <remarks>
      /// The decoding process will fail only if the property is found, it matches the underlying type and the decoding fails.
      /// If the property is not found or is null, the decoding will return an option type.
      /// </remarks>
      /// <param name="name"></param>
      /// <param name="decoder"></param>
      static member inline dict:
        name: string * decoder: IndexedMapCollectErrorsDecoder<'TValue> ->
          (JsonElement -> Result<System.Collections.Generic.Dictionary<string, 'TValue> option, DecodeError list>)

/// <summary>
/// Provides an in-the-box computation expression that can be used to decode JSON elements.
/// Ideally you should use <see cref="https://github.com/demystifyfp/FsToolkit.ErrorHandling">FsToolkit.ErrorHandling</see> instead of this as it is much more complete
/// and would allow you to handle decoding workflows in a more robust way.
/// However, if you don't want to take a dependency on FsToolkit.ErrorHandling, this should be just enough for you.
/// </summary>
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

  /// <summary>
  /// Computation expression to seamlessly decode JSON elements.
  /// </summary>
  /// <example>
  /// <code lang="fsharp">
  ///   type Person = { Name: string; Age: int }
  ///   let PersonDecoder = decode {
  ///     let! name = Property.get "name" Decode.Required.string
  ///     and! age = Property.get "age" Decode.Required.int
  ///     return { Name = name; Age = age }
  ///   }
  /// </code>
  /// </example>
  val decode: DecodeBuilder

[<Class>]
type Decoding =
  /// <summary>
  /// Attempts to decode a JSON string using a <see cref="System.Text.Json.JsonDocument">JsonDocument</see> instance.
  /// Without any decoder, works just like `JsonSerializer.Deserialize(json)`.
  /// </summary>
  /// <param name="json"></param>
  /// <param name="docOptions"></param>
  static member inline auto: json: string * ?docOptions: JsonDocumentOptions -> Result<'TResult, DecodeError>

  /// <summary>
  /// Attempts to decode a JSON string using a <see cref="System.Text.Json.JsonDocument">JsonDocument</see> instance.
  /// Without any decoder, works just like `JsonSerializer.Deserialize(json)`.
  /// </summary>
  /// <remarks>
  /// You can pass JsonSerializerOptions to customize the deserialization process and even include your decoders.
  /// </remarks>
  /// <param name="json"></param>
  /// <param name="options"></param>
  /// <param name="docOptions"></param>
  static member inline auto:
    json: string * options: JsonSerializerOptions * ?docOptions: JsonDocumentOptions -> Result<'TResult, DecodeError>

  /// <summary>
  /// Attempts to decode a JSON byte array using a <see cref="System.Text.Json.JsonDocument">JsonDocument</see> instance.
  /// Without any decoder, works just like `JsonSerializer.Deserialize(json)`.
  /// </summary>
  /// <param name="json"></param>
  /// <param name="docOptions"></param>
  static member inline auto: json: byte array * ?docOptions: JsonDocumentOptions -> Result<'TResult, DecodeError>

  /// <summary>
  /// Attempts to decode a JSON byte array using a <see cref="System.Text.Json.JsonDocument">JsonDocument</see> instance.
  /// Without any decoder, works just like `JsonSerializer.Deserialize(json)`.
  /// </summary>
  /// <remarks>
  /// You can pass JsonSerializerOptions to customize the deserialization process and even include your decoders.
  /// </remarks>
  /// <param name="json"></param>
  /// <param name="options"></param>
  /// <param name="docOptions"></param>
  static member inline auto:
    json: byte array * options: JsonSerializerOptions * ?docOptions: JsonDocumentOptions ->
      Result<'TResult, DecodeError>

  /// <summary>
  /// Attempts to decode a JSON stream using a <see cref="System.Text.Json.JsonDocument">JsonDocument</see> instance.
  /// Without any decoder, works just like `JsonSerializer.Deserialize(json)`.
  /// </summary>
  /// <param name="json"></param>
  /// <param name="docOptions"></param>
  static member inline auto:
    json: Stream * ?docOptions: JsonDocumentOptions -> System.Threading.Tasks.Task<Result<'TResult, DecodeError>>

  /// <summary>
  /// Attempts to decode a JSON stream using a <see cref="System.Text.Json.JsonDocument">JsonDocument</see> instance.
  /// Without any decoder, works just like `JsonSerializer.Deserialize(json)`.
  /// </summary>
  /// <remarks>
  /// You can pass JsonSerializerOptions to customize the deserialization process and even include your decoders.
  /// </remarks>
  /// <param name="json"></param>
  /// <param name="options"></param>
  /// <param name="docOptions"></param>
  static member inline auto:
    json: Stream * options: JsonSerializerOptions * ?docOptions: JsonDocumentOptions ->
      System.Threading.Tasks.Task<Result<'TResult, DecodeError>>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="options"></param>
  /// <param name="decoder"></param>
  static member inline fromString:
    value: string * options: JsonDocumentOptions * decoder: Decoder<'TResult> -> Result<'TResult, DecodeError>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="decoder"></param>
  static member inline fromString: value: string * decoder: Decoder<'TResult> -> Result<'TResult, DecodeError>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="options"></param>
  /// <param name="decoder"></param>
  static member inline fromBytes:
    value: byte array * options: JsonDocumentOptions * decoder: (JsonElement -> 'TResult) -> 'TResult

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="decoder"></param>
  static member inline fromBytes: value: byte array * decoder: Decoder<'TResult> -> Result<'TResult, DecodeError>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="options"></param>
  /// <param name="decoder"></param>
  static member inline fromStream:
    value: Stream * options: JsonDocumentOptions * decoder: (JsonElement -> 'TResult) -> Threading.Tasks.Task<'TResult>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="decoder"></param>
  static member inline fromStream:
    value: Stream * decoder: Decoder<'TResult> -> Threading.Tasks.Task<Result<'TResult, DecodeError>>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <remarks>
  /// This method will take a result that collects all the errors that occur during the decoding process.
  /// </remarks>
  /// <param name="value"></param>
  /// <param name="options"></param>
  /// <param name="decoder"></param>
  static member inline fromStringCol:
    value: string * options: JsonDocumentOptions * decoder: CollectErrorsDecoder<'TResult> ->
      Result<'TResult, DecodeError list>

  /// <summary>
  /// Takes a string, a decoder and attempts to decode the string into the desired type.
  /// </summary>
  /// <remarks>
  /// This method will take a result that collects all the errors that occur during the decoding process.
  /// </remarks>
  /// <param name="value"></param>
  /// <param name="decoder"></param>
  static member inline fromStringCol:
    value: string * decoder: CollectErrorsDecoder<'TResult> -> Result<'TResult, DecodeError list>

  /// <summary>
  /// Takes a byte array, a decoder and attempts to decode the byte array into the desired type.
  /// </summary>
  /// <remarks>
  /// This method will take a result that collects all the errors that occur during the decoding process.
  /// </remarks>
  /// <param name="value"></param>
  /// <param name="options"></param>
  /// <param name="decoder"></param>
  static member inline fromBytesCol:
    value: byte array * options: JsonDocumentOptions * decoder: CollectErrorsDecoder<'TResult> ->
      Result<'TResult, DecodeError list>

  /// <summary>
  /// Takes a byte array, a decoder and attempts to decode the byte array into the desired type.
  /// </summary>
  /// <remarks>
  /// This method will take a result that collects all the errors that occur during the decoding process.
  /// </remarks>
  /// <param name="value"></param>
  /// <param name="decoder"></param>
  static member inline fromBytesCol:
    value: byte array * decoder: CollectErrorsDecoder<'TResult> -> Result<'TResult, DecodeError list>

  /// <summary>
  /// Takes a stream, a decoder and attempts to decode the stream into the desired type.
  /// </summary>
  /// <remarks>
  /// This method will take a result that collects all the errors that occur during the decoding process.
  /// </remarks>
  /// <param name="value"></param>
  /// <param name="options"></param>
  /// <param name="decoder"></param>
  static member inline fromStreamCol:
    value: Stream * options: JsonDocumentOptions * decoder: CollectErrorsDecoder<'TResult> ->
      Threading.Tasks.Task<Result<'TResult, DecodeError list>>

  /// <summary>
  /// Takes a stream, a decoder and attempts to decode the stream into the desired type.
  /// </summary>
  /// <remarks>
  /// This method will take a result that collects all the errors that occur during the decoding process.
  /// </remarks>
  /// <param name="value"></param>
  /// <param name="decoder"></param>
  static member inline fromStreamCol:
    value: Stream * decoder: CollectErrorsDecoder<'TResult> -> Threading.Tasks.Task<Result<'TResult, DecodeError list>>

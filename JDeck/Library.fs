namespace JDeck

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization


type DecodeError = {
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
  let inline ofError<'TResult> (el: JsonElement, message) : DecodeError = {
    value = el
    kind = el.ValueKind
    rawValue = el.GetRawText()
    targetType = typeof<'TResult>
    message = message
    exn = None
    index = None
    property = None
  }

  let inline ofIndexed<'TResult>
    (el: JsonElement, index, message)
    : DecodeError =
    {
      value = el
      kind = el.ValueKind
      rawValue = el.GetRawText()
      targetType = typeof<'TResult>
      message = message
      exn = None
      index = Some index
      property = None
    }

  let withIndex i (error: DecodeError) = { error with index = Some i }

  let withProperty name (error: DecodeError) = {
    error with
        property = Some name
  }

  let withException ex (error: DecodeError) = {
    error with
        exn = Some ex
        message = ex.Message
  }

  let withMessage message (error: DecodeError) = {
    error with
        message = message
  }

type Decoder<'TResult> = JsonElement -> Result<'TResult, DecodeError>

type IndexedDecoder<'TResult> =
  int -> JsonElement -> Result<'TResult, DecodeError>

type IndexedMapDecoder<'TValue> =
  string -> JsonElement -> Result<'TValue, DecodeError>

type CollectErrorsDecoder<'TResult> =
  JsonElement -> Result<'TResult, DecodeError list>

type IndexedCollectErrorsDecoder<'TResult> =
  int -> JsonElement -> Result<'TResult, DecodeError list>

type IndexedMapCollectErrorsDecoder<'TValue> =
  string -> JsonElement -> Result<'TValue, DecodeError list>


module Seq =
  let inline collectErrors
    ([<InlineIfLambda>] f:
      int -> JsonElement -> Result<'TValue, DecodeError list>)
    xs
    =
    let values = ResizeArray<_>()
    let errors = ResizeArray<_>()

    for i, x in Seq.indexed xs do
      match f i x with
      | Ok value -> values.Add value
      | Error error -> errors.AddRange(error)

    if errors.Count > 0 then
      Error(errors |> List.ofSeq)
    else
      Ok(values :> seq<_>)

  let inline collectUntilError ([<InlineIfLambda>] f: int -> Decoder<_>) xs =
    let errors = ResizeArray<_>()
    let values = ResizeArray<_>()
    use enumerator = (Seq.indexed xs).GetEnumerator()

    while errors.Count = 0 && enumerator.MoveNext() do
      let i, x = enumerator.Current

      match f i x with
      | Ok value -> values.Add value
      | Error error -> errors.Add(error |> DecodeError.withIndex i)

    if errors.Count > 0 then
      Error(errors[0])
    else
      Ok(values :> seq<_>)

  let inline collectProperties
    ([<InlineIfLambda>] valueDecoder: IndexedMapCollectErrorsDecoder<'TValue>)
    (el: JsonElement)
    =
    use mutable xs = el.EnumerateObject()
    let errors = ResizeArray<DecodeError>()
    let collected = ResizeArray<string * 'TValue>()

    while xs.MoveNext() do
      let key = xs.Current.Name

      match valueDecoder key xs.Current.Value with
      | Ok value -> collected.Add(key, value)
      | Error err ->
        errors.AddRange(
          err |> List.map(fun e -> e |> DecodeError.withProperty key)
        )

    if errors.Count > 0 then
      Error(errors :> seq<_>)
    else
      Ok(collected :> seq<_>)

  let inline collectPropertiesUntilError
    ([<InlineIfLambda>] valueDecoder: IndexedMapDecoder<'TValue>)
    (el: JsonElement)
    =
    use mutable xs = el.EnumerateObject()
    let collected = ResizeArray<string * 'TValue>()
    let mutable error = None

    while error.IsNone && xs.MoveNext() do
      let key = xs.Current.Name

      match valueDecoder key xs.Current.Value with
      | Ok value -> collected.Add(key, value)
      | Error err -> error <- Some(err |> DecodeError.withProperty key)

    match error with
    | Some error -> Error error
    | None -> Ok(collected :> seq<_>)

[<AutoOpen>]
module Decode =
  module Decode =
    let inline sequence
      ([<InlineIfLambda>] decoder: IndexedDecoder<_>)
      (el: JsonElement)
      =
      el.EnumerateArray() |> Seq.collectUntilError decoder

    let inline sequenceCol
      ([<InlineIfLambda>] decoder: IndexedCollectErrorsDecoder<_>)
      (el: JsonElement)
      =
      el.EnumerateArray() |> Seq.collectErrors decoder

    let inline map
      ([<InlineIfLambda>] decoder: IndexedMapDecoder<'TValue>)
      (el: JsonElement)
      =
      match el.ValueKind with
      | JsonValueKind.Object ->
        try
          el |> Seq.collectPropertiesUntilError decoder |> Result.map Map.ofSeq
        with ex ->
          DecodeError.ofError(el.Clone(), "")
          |> DecodeError.withException ex
          |> Error
      | kind ->
        DecodeError.ofError(el.Clone(), $"Expected 'Object' but got `{kind}`")
        |> Error

    let inline mapCol
      ([<InlineIfLambda>] decoder: IndexedMapCollectErrorsDecoder<'TValue>)
      (el: JsonElement)
      =
      el |> Seq.collectProperties decoder |> Result.map Map.ofSeq

    let inline dict
      ([<InlineIfLambda>] decoder: IndexedMapDecoder<'TValue>)
      (el: JsonElement)
      =
      match el.ValueKind with
      | JsonValueKind.Object ->
        try
          el
          |> Seq.collectPropertiesUntilError decoder
          |> Result.map(fun kv ->
            let dict = System.Collections.Generic.Dictionary<string, 'TValue>()

            for k, v in kv do
              dict.Add(k, v)

            dict
          )
        with ex ->
          DecodeError.ofError(el.Clone(), "")
          |> DecodeError.withException ex
          |> Error
      | kind ->
        DecodeError.ofError(el.Clone(), $"Expected 'Object' but got `{kind}`")
        |> Error

    let inline dictCol
      ([<InlineIfLambda>] decoder: IndexedMapCollectErrorsDecoder<'TValue>)
      (el: JsonElement)
      =
      el
      |> Seq.collectProperties decoder
      |> Result.map(fun kv ->
        let dict = System.Collections.Generic.Dictionary<string, 'TValue>()

        for k, v in kv do
          dict.Add(k, v)

        dict
      )

    let oneOf (decoders: Decoder<_> seq) element =
      let mutable resolvedValue = None
      let mutable error = None

      use enumerator = decoders.GetEnumerator()

      while resolvedValue.IsNone && enumerator.MoveNext() do
        match enumerator.Current element with
        | Ok v -> resolvedValue <- Some v
        | Error e -> error <- Some e

      match resolvedValue, error with
      | Some v, _ -> Ok v
      | _, Some error -> Error(error)
      | None, None -> failwith "todo"

    let collectOneOf (decoders: Decoder<'TResult> seq) element =
      let mutable resolvedValue = None
      let mutable errors = ResizeArray()

      use enumerator = decoders.GetEnumerator()

      while resolvedValue.IsNone && errors.Count = 0 && enumerator.MoveNext() do
        match enumerator.Current element with
        | Ok v -> resolvedValue <- Some v
        | Error e -> errors.Add e

      if errors.Count > 0 then
        Error(errors |> List.ofSeq)
      else
        Ok resolvedValue.Value

    let inline array ([<InlineIfLambda>] decoder) (el: JsonElement) =
      sequence decoder el |> Result.map Array.ofSeq

    let inline list ([<InlineIfLambda>] decoder) (el: JsonElement) =
      sequence decoder el |> Result.map List.ofSeq

    let inline decodeAt
      ([<InlineIfLambda>] decoder: Decoder<_>)
      index
      (el: JsonElement)
      =
      try
        use xs = el.EnumerateArray()

        match Seq.tryItem index xs with
        | Some x -> decoder x
        | None ->
          DecodeError.ofError(el.Clone(), $"Index {index} not found") |> Error
      with ex ->
        DecodeError.ofError(el.Clone(), "")
        |> DecodeError.withException ex
        |> Error

    let inline tryDecodeAt
      ([<InlineIfLambda>] decoder: Decoder<_>)
      index
      (el: JsonElement)
      =
      try
        use xs = el.EnumerateArray()

        match Seq.tryItem index xs with
        | Some x -> decoder x |> Result.map Some
        | None -> Ok None
      with ex ->
        DecodeError.ofError(el.Clone(), "")
        |> DecodeError.withException ex
        |> Error

    let inline decodeAtKey
      ([<InlineIfLambda>] decoder: Decoder<_>)
      (key: string)
      (el: JsonElement)
      =
      el.GetProperty(key) |> decoder

    let inline tryDecodeAtKey
      ([<InlineIfLambda>] decoder: Decoder<_>)
      (key: string)
      (el: JsonElement)
      =
      match el.TryGetProperty key with
      | true, x -> decoder x |> Result.map Some
      | false, _ ->
        DecodeError.ofError(el.Clone(), $"Key {key} not found")
        |> DecodeError.withProperty key
        |> Error

    let inline auto<'TResult> (el: JsonElement) =
      try
        el.Deserialize<'TResult>() |> Ok
      with ex ->
        DecodeError.ofError<'TResult>(el.Clone(), "")
        |> DecodeError.withException ex
        |> Error

    let inline autoJsonOptions
      (options: JsonSerializerOptions)
      (el: JsonElement)
      =
      try
        el.Deserialize<'TResult>(options) |> Ok
      with ex ->
        DecodeError.ofError<'TResult>(el.Clone(), "")
        |> DecodeError.withException ex
        |> Error

    [<AutoOpen>]
    module JsonConverter =
      type DecoderConverter<'T>(decoder: JsonElement -> Result<'T, DecodeError>)
        =
        inherit JsonConverter<'T>()

        override this.CanConvert(typeToConvert: Type) =
          typeToConvert = typeof<'T>

        override this.Read(reader: byref<Utf8JsonReader>, _: Type, _) =
          use json = JsonDocument.ParseValue(&reader)

          match decoder json.RootElement with
          | Ok value -> value
          | Error err -> raise(JsonException(err.message))

        override this.Write
          (writer: Utf8JsonWriter, value, options: JsonSerializerOptions)
          =
          JsonSerializer.Serialize(writer, value, options)

    [<Obsolete "This structure will be removed prior the v1.0 release, please use JDeck.Codec.useDecoder or JDeck.Codec.useCodec">]
    let useDecoder decoder (options: JsonSerializerOptions) =
      options.Converters.Insert(0, DecoderConverter<'T>(decoder))
      options

  module Required =

    let inline internal shell
      (valueKind: JsonValueKind)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      try
        match element.ValueKind with
        | kind when kind = valueKind -> decoder element
        | kind ->
          DecodeError.ofError(
            element.Clone(),
            $"Expected '{valueKind}' but got `{kind}`"
          )
          |> Error
      with ex ->
        DecodeError.ofError(element.Clone(), "")
        |> DecodeError.withException ex
        |> Error

    let string =
      shell JsonValueKind.String (fun element -> Ok(element.GetString()))

    let boolean =
      fun (element: JsonElement) ->
        try
          match element.ValueKind with
          | JsonValueKind.True
          | JsonValueKind.False -> Ok(element.GetBoolean())
          | kind ->
            DecodeError.ofError(
              element.Clone(),
              $"Expected a boolean but got `{kind}`"
            )
            |> Error
        with ex ->
          DecodeError.ofError(element.Clone(), "")
          |> DecodeError.withException ex
          |> Error

    let char =
      shell
        JsonValueKind.String
        (fun element ->
          let value = element.GetString()

          if value.Length > 1 then
            DecodeError.ofError<char>(
              element.Clone(),
              $"Expecting a char but got a string of size: %i{value.Length}"
            )
            |> Error
          else
            Ok value[0]
        )

    let guid =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetGuid() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let unit = shell JsonValueKind.Null (fun _ -> Ok())

    let byte =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetByte() with
          | true, byte -> Ok byte
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to get byte from the current value"
            )
            |> Error
        )

    let int =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt32() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to get an int from the current value"
            )
            |> Error
        )

    let int64 =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt64() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to get an int64 from the current value"
            )
            |> Error
        )

    let float =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetDouble() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to get a float from the current value"
            )
            |> Error
        )

    let dateTime =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTime() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to get a DateTime from the current value"
            )
            |> Error
        )

    let dateTimeOffset =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTimeOffset() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to get a DateTimeOffset from the current value"
            )
            |> Error
        )

    [<Class>]
    type Property =
      static member inline get(name: string, decoder) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> decoder el
          | false, _ ->
            DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
            |> DecodeError.withProperty name
            |> Error

      static member inline get(name: string, decoder) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> decoder el
          | false, _ ->
            [
              DecodeError.ofError(
                element.Clone(),
                $"Property '{name}' not found"
              )
              |> DecodeError.withProperty name
            ]
            |> Error

      static member inline seq(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.sequence (fun _ -> decoder) el
          | false, _ ->
            DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
            |> DecodeError.withProperty name
            |> Error

      static member inline seq(name: string, decoder: CollectErrorsDecoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.sequenceCol (fun _ -> decoder) el
          | false, _ ->
            [
              DecodeError.ofError(
                element.Clone(),
                $"Property '{name}' not found"
              )
              |> DecodeError.withProperty name
            ]
            |> Error

      static member inline seqAt(name: string, index, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty(name) with
          | true, el -> decoder el
          | false, _ ->
            DecodeError.ofIndexed(
              element.Clone(),
              index,
              $"Property '{name}' not found"
            )
            |> DecodeError.withProperty name
            |> Error

      static member inline list(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          element |> Property.seq(name, decoder) |> Result.map List.ofSeq

      static member inline list
        (name: string, decoder: CollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          element |> Property.seq(name, decoder) |> Result.map List.ofSeq

      static member inline array(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          element |> Property.seq(name, decoder) |> Result.map Array.ofSeq

      static member inline array
        (name: string, decoder: CollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          element |> Property.seq(name, decoder) |> Result.map Array.ofSeq

      static member inline map(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.map (fun _ -> decoder) el
          | false, _ ->
            DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
            |> DecodeError.withProperty name
            |> Error

      static member inline map
        (name: string, decoder: IndexedMapCollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.mapCol decoder el |> Result.mapError List.ofSeq
          | false, _ ->
            [
              DecodeError.ofError(
                element.Clone(),
                $"Property '{name}' not found"
              )
              |> DecodeError.withProperty name
            ]
            |> Error

      static member inline dict(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.dict (fun _ -> decoder) el
          | false, _ ->
            DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
            |> DecodeError.withProperty name
            |> Error

      static member inline dict
        (name: string, decoder: IndexedMapCollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.dictCol decoder el |> Result.mapError List.ofSeq
          | false, _ ->
            [
              DecodeError.ofError(
                element.Clone(),
                $"Property '{name}' not found"
              )
              |> DecodeError.withProperty name
            ]
            |> Error

  module Optional =

    let inline internal shell
      (valueKind: JsonValueKind)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      try
        match element.ValueKind with
        | kind when kind = valueKind -> decoder element |> Result.map Some
        | JsonValueKind.Null
        | JsonValueKind.Undefined -> Ok None
        | kind ->
          DecodeError.ofError(
            element.Clone(),
            $"Expected '{valueKind}' but got `{kind}`"
          )
          |> Error
      with ex ->
        DecodeError.ofError(element.Clone(), "")
        |> DecodeError.withException ex
        |> Error


    let string =
      shell JsonValueKind.String (fun element -> Ok(element.GetString()))

    let boolean (element: JsonElement) =
      try
        match element.ValueKind with
        | JsonValueKind.True
        | JsonValueKind.False -> Ok(Some(element.GetBoolean()))
        | JsonValueKind.Undefined
        | JsonValueKind.Null -> Ok None
        | kind ->
          DecodeError.ofError(
            element.Clone(),
            $"Expected a boolean but got `{kind}`"
          )
          |> Error
      with ex ->
        DecodeError.ofError(element.Clone(), "")
        |> DecodeError.withException ex
        |> Error

    let char =
      shell
        JsonValueKind.String
        (fun element ->
          let value = element.GetString()

          if value.Length > 1 then
            DecodeError.ofError(
              element.Clone(),
              $"Expecting a char but got a string of size: %i{value.Length}"
            )
            |> Error
          else
            Ok value[0]
        )

    let guid =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetGuid() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let unit = shell JsonValueKind.Null (fun _ -> Ok())

    let byte =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetByte() with
          | true, byte -> Ok byte
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let int =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt32() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let int64 =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt64() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let float =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetDouble() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let dateTime =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTime() with
          | true, value -> Ok value
          | _ ->

            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    let dateTimeOffset =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTimeOffset() with
          | true, value -> Ok value
          | _ ->
            DecodeError.ofError(
              element.Clone(),
              "Unable to decode a guid from the current value"
            )
            |> Error
        )

    [<Class>]
    type Property =
      static member inline get(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> decoder el |> Result.map Some
          | false, _ -> Ok None

      static member inline get(name: string, decoder: CollectErrorsDecoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> decoder el |> Result.map Some
          | false, _ -> Ok None

      static member inline seq(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.sequence (fun _ -> decoder) el |> Result.map Some
          | false, _ -> Ok None

      static member inline seq(name: string, decoder: CollectErrorsDecoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el ->
            Decode.sequenceCol (fun _ -> decoder) el |> Result.map Some
          | false, _ -> Ok None

      static member inline list(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          element
          |> Property.seq(name, decoder)
          |> Result.map(
            function
            | Some v -> v |> List.ofSeq |> Some
            | None -> None
          )

      static member inline list
        (name: string, decoder: CollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          element
          |> Property.seq(name, decoder)
          |> Result.map(
            function
            | Some v -> v |> List.ofSeq |> Some
            | None -> None
          )

      static member inline array(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          element
          |> Property.seq(name, decoder)
          |> Result.map(
            function
            | Some v -> v |> Array.ofSeq |> Some
            | None -> None
          )

      static member inline array
        (name: string, decoder: CollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          element
          |> Property.seq(name, decoder)
          |> Result.map(
            function
            | Some v -> v |> Array.ofSeq |> Some
            | None -> None
          )

      static member inline map(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.map (fun _ -> decoder) el |> Result.map Some
          | false, _ -> Ok None

      static member inline map
        (name: string, decoder: IndexedMapCollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el ->
            Decode.mapCol decoder el
            |> Result.mapError List.ofSeq
            |> Result.map Some
          | false, _ -> Ok None

      static member inline dict(name: string, decoder: Decoder<_>) =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el -> Decode.dict (fun _ -> decoder) el |> Result.map Some
          | false, _ -> Ok None

      static member inline dict
        (name: string, decoder: IndexedMapCollectErrorsDecoder<_>)
        =
        fun (element: JsonElement) ->
          match element.TryGetProperty name with
          | true, el ->
            Decode.dictCol decoder el
            |> Result.mapError List.ofSeq
            |> Result.map Some
          | false, _ -> Ok None

[<AutoOpen>]
module Builders =
  type DecodeBuilder() =

    member inline _.Bind
      (
        value: Result<'TValue, DecodeError>,
        [<InlineIfLambda>] f: 'TValue -> Result<'TResult, DecodeError>
      ) =
      match value with
      | Ok value -> f value
      | Error error -> Error error

    member inline _.Source(result: Result<'TValue, DecodeError>) = result

    member inline _.Return(value: 'TValue) : Result<'TValue, DecodeError> =
      Ok value

    member inline _.ReturnFrom(value: Result<'TValue, DecodeError>) = value

    member inline _.BindReturn
      (
        value: Result<'TValue, DecodeError>,
        [<InlineIfLambda>] f: 'TValue -> 'TResult
      ) =
      Result.map f value

    member inline this.Zero() = this.Return()

    member inline _.Delay
      ([<InlineIfLambda>] generator)
      : unit -> Result<'TValue, DecodeError> =
      generator

    member inline _.Run
      ([<InlineIfLambda>] generator: unit -> Result<'TValue, DecodeError>)
      =
      generator()

    member inline this.Combine
      (value, [<InlineIfLambda>] f: 'TValue -> Result<'TResult, DecodeError>)
      =
      this.Bind(value, f)

    member inline this.TryFinally
      (
        [<InlineIfLambda>] generator: unit -> Result<'TValue, DecodeError>,
        [<InlineIfLambda>] compensation: unit -> unit
      ) =
      try
        this.Run generator
      finally
        compensation()

    member inline this.Using
      (
        resource: 'disposable :> IDisposable,
        [<InlineIfLambda>] binder: 'disposable -> Result<'TResult, DecodeError>
      ) =
      this.TryFinally(
        (fun () -> binder resource),
        (fun () ->
          if not(obj.ReferenceEquals(resource, null)) then
            resource.Dispose()
        )
      )

    member inline _.MergeSources(r1, r2) =
      match r1, r2 with
      | Ok v1, Ok v2 -> Ok(v1, v2)
      | Error e1, _ -> Error e1
      | _, Error e2 -> Error e2

    member inline _.MergeSources3(r1, r2, r3) =
      match r1, r2, r3 with
      | Ok v1, Ok v2, Ok v3 -> Ok(v1, v2, v3)
      | Error e1, _, _ -> Error e1
      | _, Error e2, _ -> Error e2
      | _, _, Error e3 -> Error e3

    member inline _.MergeSources4(r1, r2, r3, r4) =
      match r1, r2, r3, r4 with
      | Ok v1, Ok v2, Ok v3, Ok v4 -> Ok(v1, v2, v3, v4)
      | Error e1, _, _, _ -> Error e1
      | _, Error e2, _, _ -> Error e2
      | _, _, Error e3, _ -> Error e3
      | _, _, _, Error e4 -> Error e4

    member inline _.MergeSources5(r1, r2, r3, r4, r5) =

      match r1, r2, r3, r4, r5 with
      | Ok v1, Ok v2, Ok v3, Ok v4, Ok v5 -> Ok(v1, v2, v3, v4, v5)
      | Error e1, _, _, _, _ -> Error e1
      | _, Error e2, _, _, _ -> Error e2
      | _, _, Error e3, _, _ -> Error e3
      | _, _, _, Error e4, _ -> Error e4
      | _, _, _, _, Error e5 -> Error e5

  let decode = DecodeBuilder()

type Decoding =
  static member inline fromString(value: string, options, decoder: Decoder<_>) =
    use doc = JsonDocument.Parse(value, options = options)
    let root = doc.RootElement
    decoder root

  static member inline fromString(value: string, decoder: Decoder<_>) =
    use doc = JsonDocument.Parse(value)
    let root = doc.RootElement
    decoder root

  static member inline fromBytes(value: byte array, options, decoder) =
    use doc = JsonDocument.Parse(value, options = options)
    let root = doc.RootElement
    decoder root

  static member inline fromBytes(value: byte array, decoder: Decoder<_>) =
    use doc = JsonDocument.Parse(value)
    let root = doc.RootElement
    decoder root

  static member inline fromStream(value: Stream, options, decoder) = task {
    use! doc = JsonDocument.ParseAsync(value, options = options)
    let root = doc.RootElement
    return decoder root
  }

  static member inline fromStream(value: Stream, decoder: Decoder<_>) = task {
    use! doc = JsonDocument.ParseAsync(value)
    let root = doc.RootElement
    return decoder root
  }

  static member inline fromStringCol
    (value: string, options, decoder: CollectErrorsDecoder<_>)
    =
    use doc = JsonDocument.Parse(value, options = options)
    let root = doc.RootElement
    decoder root

  static member inline fromStringCol
    (value: string, decoder: CollectErrorsDecoder<_>)
    =
    use doc = JsonDocument.Parse(value)
    let root = doc.RootElement
    decoder root

  static member inline fromBytesCol
    (value: byte array, options, decoder: CollectErrorsDecoder<_>)
    =
    use doc = JsonDocument.Parse(value, options = options)
    let root = doc.RootElement
    decoder root

  static member inline fromBytesCol
    (value: byte array, decoder: CollectErrorsDecoder<_>)
    =
    use doc = JsonDocument.Parse(value)
    let root = doc.RootElement
    decoder root

  static member inline fromStreamCol
    (value: Stream, options, decoder: CollectErrorsDecoder<_>)
    =
    task {
      use! doc = JsonDocument.ParseAsync(value, options = options)
      let root = doc.RootElement
      return decoder root
    }

  static member inline fromStreamCol
    (value: Stream, decoder: CollectErrorsDecoder<_>)
    =
    task {
      use! doc = JsonDocument.ParseAsync(value)
      let root = doc.RootElement
      return decoder root
    }

  static member inline auto(json: string, ?docOptions) =
    use doc = JsonDocument.Parse(json, ?options = docOptions)
    Decode.auto doc.RootElement

  static member inline auto(json: string, options, ?docOptions) =
    use doc = JsonDocument.Parse(json, ?options = docOptions)
    Decode.autoJsonOptions options doc.RootElement

  static member inline auto(json: byte array, ?docOptions) =
    use doc = JsonDocument.Parse(json, ?options = docOptions)
    Decode.auto doc.RootElement

  static member inline auto(json: byte array, options, ?docOptions) =
    use doc = JsonDocument.Parse(json, ?options = docOptions)
    Decode.autoJsonOptions options doc.RootElement

  static member inline auto(json: Stream, ?docOptions) = task {
    use! doc = JsonDocument.ParseAsync(json, ?options = docOptions)
    return Decode.auto doc.RootElement
  }

  static member inline auto(json: Stream, options, ?docOptions) = task {
    use! doc = JsonDocument.ParseAsync(json, ?options = docOptions)
    return Decode.autoJsonOptions options doc.RootElement
  }

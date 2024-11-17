namespace JDeck

open System
open System.IO
open System.Text.Json


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

type ValidationDecoder<'TResult> =
  JsonElement -> Result<'TResult, DecodeError list>


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

module Decode =
  module Decode =
    let inline sequence
      ([<InlineIfLambda>] decoder: IndexedDecoder<_>)
      (el: JsonElement)
      =
      el.EnumerateArray() |> Seq.collectUntilError decoder

    let inline seqTraverse
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError list>)
      (el: JsonElement)
      =
      el.EnumerateArray() |> Seq.collectErrors decoder

    let inline array ([<InlineIfLambda>] decoder) (el: JsonElement) =
      sequence decoder el |> Result.map Array.ofSeq

    let inline list ([<InlineIfLambda>] decoder) (el: JsonElement) =
      sequence decoder el |> Result.map List.ofSeq

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
            $"Expected '{Enum.GetName valueKind}' but got `%s{Enum.GetName kind}`"
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
              $"Expected a boolean but got `%s{Enum.GetName kind}`"
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

    let inline property
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> decoder el
      | false, _ ->
        DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
        |> DecodeError.withProperty name
        |> Error

    let inline seqProperty
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> Decode.seqTraverse decoder el
      | false, _ ->
        [
          DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
          |> DecodeError.withProperty name
        ]
        |> Error

    let inline listProperty
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      seqProperty name decoder element |> Result.map List.ofSeq

    let inline arrayProperty
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      seqProperty name decoder element |> Result.map Array.ofSeq

    let inline collectSeqProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError>)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el ->
        let errors = ResizeArray<_>()
        let values = ResizeArray<_>()

        for i, x in el.EnumerateArray() |> Seq.indexed do
          match decoder i x with
          | Ok value -> values.Add value
          | Error error -> errors.Add error

        if errors.Count > 0 then
          Error(errors |> List.ofSeq)
        else
          Ok(values :> seq<_>)
      | false, _ ->
        [
          DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
          |> DecodeError.withProperty name
        ]
        |> Error

    let inline collectArrayProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError>)
      (element: JsonElement)
      =
      collectSeqProperty name decoder element |> Result.map Array.ofSeq

    let inline collectListProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError>)
      (element: JsonElement)
      =
      collectSeqProperty name decoder element |> Result.map List.ofSeq


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
            $"Expected '{Enum.GetName valueKind}' but got `%s{Enum.GetName kind}`"
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
            $"Expected a boolean but got `%s{Enum.GetName kind}`"
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

    let inline property
      (name: string)
      ([<InlineIfLambda>] decoder: Decoder<_>)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> decoder el |> Result.map Some
      | false, _ -> Ok None

    let inline seqProperty
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> Decode.seqTraverse decoder el |> Result.map Some
      | false, _ -> Ok None

    let inline listProperty
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      seqProperty name decoder element
      |> Result.map(
        function
        | Some v -> List.ofSeq v |> Some
        | None -> None
      )

    let inline arrayProperty
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      seqProperty name decoder element
      |> Result.map(
        function
        | Some v -> Array.ofSeq v |> Some
        | None -> None
      )

    let inline collectSeqProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError>)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el ->
        let errors = ResizeArray<_>()
        let values = ResizeArray<_>()

        for i, x in el.EnumerateArray() |> Seq.indexed do
          match decoder i x with
          | Ok value -> values.Add value
          | Error error -> errors.Add error

        if errors.Count > 0 then
          Error(errors |> List.ofSeq)
        else
          Ok(values :> seq<_> |> Some)
      | false, _ ->
        [
          DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
          |> DecodeError.withProperty name
        ]
        |> Error

    let inline collectArrayProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError>)
      (element: JsonElement)
      =
      collectSeqProperty name decoder element
      |> Result.map(
        function
        | Some v -> Array.ofSeq v |> Some
        | None -> None
      )

    let inline collectListProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError>)
      (element: JsonElement)
      =
      collectSeqProperty name decoder element
      |> Result.map(
        function
        | Some v -> List.ofSeq v |> Some
        | None -> None
      )

type Decode =
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

  static member inline validateFromString
    (value: string, options, decoder: ValidationDecoder<_>)
    =
    use doc = JsonDocument.Parse(value, options = options)
    let root = doc.RootElement
    decoder root

  static member inline validateFromString
    (value: string, decoder: ValidationDecoder<_>)
    =
    use doc = JsonDocument.Parse(value)
    let root = doc.RootElement
    decoder root

  static member inline validateFromBytes
    (value: byte array, options, decoder: ValidationDecoder<_>)
    =
    use doc = JsonDocument.Parse(value, options = options)
    let root = doc.RootElement
    decoder root

  static member inline validateFromBytes
    (value: byte array, decoder: ValidationDecoder<_>)
    =
    use doc = JsonDocument.Parse(value)
    let root = doc.RootElement
    decoder root

  static member inline validateFromStream
    (value: Stream, options, decoder: ValidationDecoder<_>)
    =
    task {
      use! doc = JsonDocument.ParseAsync(value, options = options)
      let root = doc.RootElement
      return decoder root
    }

  static member inline validateFromStream
    (value: Stream, decoder: ValidationDecoder<_>)
    =
    task {
      use! doc = JsonDocument.ParseAsync(value)
      let root = doc.RootElement
      return decoder root
    }

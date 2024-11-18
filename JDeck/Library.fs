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

[<AutoOpen>]
module Decode =
  module Decode =
    let inline sequence
      ([<InlineIfLambda>] decoder: IndexedDecoder<_>)
      (el: JsonElement)
      =
      el.EnumerateArray() |> Seq.collectUntilError decoder

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
      | true, el -> Decode.sequence decoder el
      | false, _ ->
        DecodeError.ofError(element.Clone(), $"Property '{name}' not found")
        |> DecodeError.withProperty name
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
        int -> JsonElement -> Result<'TValue, DecodeError list>)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el ->
        let errors = ResizeArray<_>()
        let values = ResizeArray<_>()

        for i, x in el.EnumerateArray() |> Seq.indexed do
          match decoder i x with
          | Ok value -> values.Add value
          | Error error -> errors.AddRange error

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
        int -> JsonElement -> Result<'TValue, DecodeError list>)
      (element: JsonElement)
      =
      collectSeqProperty name decoder element |> Result.map Array.ofSeq

    let inline collectListProperty
      (name: string)
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TValue, DecodeError list>)
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
      ([<InlineIfLambda>] decoder:
        int -> JsonElement -> Result<'TResult, DecodeError>)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> Decode.sequence decoder el |> Result.map Some
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
        int -> JsonElement -> Result<'TValue, DecodeError list>)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el ->
        let errors = ResizeArray<_>()
        let values = ResizeArray<_>()

        for i, x in el.EnumerateArray() |> Seq.indexed do
          match decoder i x with
          | Ok value -> values.Add value
          | Error error -> errors.AddRange error

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
        int -> JsonElement -> Result<'TValue, DecodeError list>)
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
        int -> JsonElement -> Result<'TValue, DecodeError list>)
      (element: JsonElement)
      =
      collectSeqProperty name decoder element
      |> Result.map(
        function
        | Some v -> List.ofSeq v |> Some
        | None -> None
      )

/// <summary>
/// Provides a Result (fail-first) computation expression for decoding JSON values,
/// and a ResultCollect (gather all errors) computation expression for decoding JSON values.
/// </summary>
/// <remarks>
/// Please only use this if for some reason you're not already using FsToolkit.ErrorHandling
/// These computation expressions while may work with arbitrary Result instances, it is not meant
/// to be used for other purposes other than decoding JSON values.
/// </remarks>
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
      (value, [<InlineIfLambda>] f: 'TValue -> Result<'TResult, DecodeError>) =
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

  type ResultCollect<'ok, 'error> = Result<'ok, 'error list>

  type DecodeCollectBuilder() =

    member inline _.Bind
      (
        value: ResultCollect<'TValue, DecodeError>,
        [<InlineIfLambda>] f: 'TValue -> ResultCollect<'TResult, DecodeError>
      ) =
      match value with
      | Ok value -> f value
      | Error error -> Error error

    member inline _.Bind
      (
        value: ResultCollect<'TValue, DecodeError>,
        [<InlineIfLambda>] f: 'TValue -> Result<'TResult, DecodeError>
      ) : ResultCollect<'TResult, DecodeError> =
      match value with
      | Ok value -> f value |> Result.mapError(fun e -> [ e ])
      | Error error -> Error error

    member inline _.Source(result: ResultCollect<'TValue, DecodeError>) = result

    member inline _.Return
      (value: 'TValue)
      : ResultCollect<'TValue, DecodeError> =
      Ok value

    member inline _.ReturnFrom(value: ResultCollect<'TValue, DecodeError>) =
      value

    member inline _.BindReturn
      (
        value: ResultCollect<'TValue, DecodeError>,
        [<InlineIfLambda>] f: 'TValue -> 'TResult
      ) =
      Result.map f value

    member inline this.Zero() = this.Return()

    member inline _.Delay
      ([<InlineIfLambda>] generator)
      : unit -> ResultCollect<'TValue, DecodeError> =
      generator

    member inline _.Run
      ([<InlineIfLambda>] generator: unit -> ResultCollect<'TValue, DecodeError>)
      =
      generator()

    member inline this.Combine
      (
        value,
        [<InlineIfLambda>] f: 'TValue -> ResultCollect<'TResult, DecodeError>
      ) =
      this.Bind(value, f)

    member inline this.TryFinally
      (
        [<InlineIfLambda>] generator:
          unit -> ResultCollect<'TValue, DecodeError>,
        [<InlineIfLambda>] compensation: unit -> unit
      ) =
      try
        this.Run generator
      finally
        compensation()

    member inline this.Using
      (
        resource: 'disposable :> IDisposable,
        [<InlineIfLambda>] binder:
          'disposable -> ResultCollect<'TValue, DecodeError>
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
      | Error e1, Error e2 -> Error [ yield! e1; yield! e2 ]
      | Error e1, _ -> Error e1
      | _, Error e2 -> Error e2


    member inline _.MergeSources3(r1, r2, r3) =

      match r1, r2, r3 with
      | Ok v1, Ok v2, Ok v3 -> Ok(v1, v2, v3)
      | Error e1, Error e2, Error e3 ->
        Error [ yield! e1; yield! e2; yield! e3 ]
      | Error e1, Error e2, _ -> Error [ yield! e1; yield! e2 ]
      | Error e1, _, Error e3 -> Error [ yield! e1; yield! e3 ]
      | _, Error e2, Error e3 -> Error [ yield! e2; yield! e3 ]
      | Error e1, _, _ -> Error e1
      | _, Error e2, _ -> Error e2
      | _, _, Error e3 -> Error e3


    member inline _.MergeSources4(r1, r2, r3, r4) =

      match r1, r2, r3, r4 with
      | Ok v1, Ok v2, Ok v3, Ok v4 -> Ok(v1, v2, v3, v4)
      | Error e1, Error e2, Error e3, Error e4 ->
        Error [ yield! e1; yield! e2; yield! e3; yield! e4 ]
      | Error e1, Error e2, Error e3, _ ->
        Error [ yield! e1; yield! e2; yield! e3 ]
      | Error e1, Error e2, _, Error e4 ->
        Error [ yield! e1; yield! e2; yield! e4 ]
      | Error e1, _, Error e3, Error e4 ->
        Error [ yield! e1; yield! e3; yield! e4 ]
      | _, Error e2, Error e3, Error e4 ->
        Error [ yield! e2; yield! e3; yield! e4 ]
      | Error e1, _, _, _ -> Error e1
      | _, Error e2, _, _ -> Error e2
      | _, _, Error e3, _ -> Error e3
      | _, _, _, Error e4 -> Error e4


    member inline _.MergeSources5(r1, r2, r3, r4, r5) =

      match r1, r2, r3, r4, r5 with
      | Ok v1, Ok v2, Ok v3, Ok v4, Ok v5 -> Ok(v1, v2, v3, v4, v5)
      | Error e1, Error e2, Error e3, Error e4, Error e5 ->
        Error [ yield! e1; yield! e2; yield! e3; yield! e4; yield! e5 ]
      | Error e1, Error e2, Error e3, Error e4, _ ->
        Error [ yield! e1; yield! e2; yield! e3; yield! e4 ]
      | Error e1, Error e2, Error e3, _, Error e5 ->
        Error [ yield! e1; yield! e2; yield! e3; yield! e5 ]
      | Error e1, Error e2, _, Error e4, Error e5 ->
        Error [ yield! e1; yield! e2; yield! e4; yield! e5 ]
      | Error e1, _, Error e3, Error e4, Error e5 ->
        Error [ yield! e1; yield! e3; yield! e4; yield! e5 ]
      | _, Error e2, Error e3, Error e4, Error e5 ->
        Error [ yield! e2; yield! e3; yield! e4; yield! e5 ]
      | Error e1, Error e2, Error e3, _, _ ->
        Error [ yield! e1; yield! e2; yield! e3 ]
      | Error e1, Error e2, _, Error e4, _ ->
        Error [ yield! e1; yield! e2; yield! e4 ]
      | Error e1, Error e2, _, _, Error e5 ->
        Error [ yield! e1; yield! e2; yield! e5 ]
      | Error e1, _, Error e3, Error e4, _ ->
        Error [ yield! e1; yield! e3; yield! e4 ]
      | Error e1, _, Error e3, _, Error e5 ->
        Error [ yield! e1; yield! e3; yield! e5 ]
      | Error e1, _, _, Error e4, Error e5 ->
        Error [ yield! e1; yield! e4; yield! e5 ]
      | _, Error e2, Error e3, Error e4, _ ->
        Error [ yield! e2; yield! e3; yield! e4 ]
      | _, Error e2, Error e3, _, Error e5 ->
        Error [ yield! e2; yield! e3; yield! e5 ]
      | _, Error e2, _, Error e4, Error e5 ->
        Error [ yield! e2; yield! e4; yield! e5 ]
      | Error e1, _, _, _, _ -> Error e1
      | _, Error e2, _, _, _ -> Error e2
      | _, _, Error e3, _, _ -> Error e3
      | _, _, _, Error e4, _ -> Error e4
      | _, _, _, _, Error e5 -> Error e5


  [<AutoOpen>]
  module BuilderExtensions =

    type DecodeCollectBuilder with

      member inline _.Source(result: Result<'TResult, DecodeError>) =
        match result with
        | Ok value -> Ok value
        | Error err -> Error [ err ]

  let decode = DecodeBuilder()
  let decodeCollect = DecodeCollectBuilder()

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

namespace JDeck

open System
open System.IO
open System.Text.Json



type Decoder<'T> = JsonElement -> Result<'T, string>
type DecodeValidated<'T> = JsonElement -> Result<'T, string list>

module Decode =
  // Grabbed these from FsToolkit.ErrorHandling
  module Seq =
    /// <summary>
    /// Applies a function to each element of a sequence and returns a single result
    /// </summary>
    /// <param name="state">The initial state</param>
    /// <param name="f">The function to apply to each element</param>
    /// <param name="xs">The input sequence</param>
    /// <returns>A result with the ok elements in a sequence or a sequence of all errors occuring in the original sequence</returns>
    let inline traverseResultA'
      state
      ([<InlineIfLambda>] f: 'okInput -> Result<'okOutput, 'error>)
      xs
      =
      let folder state x =
        match state, f x with
        | Error errors, Error e -> Seq.append errors (Seq.singleton e) |> Error
        | Ok oks, Ok ok -> Seq.append oks (Seq.singleton ok) |> Ok
        | Ok _, Error e -> Seq.singleton e |> Error
        | Error _, Ok _ -> state

      Seq.fold folder state xs

    /// <summary>
    /// Applies a function to each element of a sequence and returns a single result
    /// </summary>
    /// <param name="f">The function to apply to each element</param>
    /// <param name="xs">The input sequence</param>
    /// <returns>A result with the ok elements in a sequence or a sequence of all errors occuring in the original sequence</returns>
    /// <remarks>This function is equivalent to <see cref="traverseResultA'"/> but applying and initial state of 'Seq.empty'</remarks>
    let traverseResultA f xs = traverseResultA' (Ok Seq.empty) f xs

  let inline sequence
    ([<InlineIfLambda>] decoder: Decoder<_>)
    (el: JsonElement)
    =
    el.EnumerateArray()
    |> Seq.traverseResultA decoder
    |> Result.mapError(String.concat ", ")

  let inline array ([<InlineIfLambda>] decoder: Decoder<_>) (el: JsonElement) =
    sequence decoder el |> Result.map Array.ofSeq

  let inline list ([<InlineIfLambda>] decoder: Decoder<_>) (el: JsonElement) =
    sequence decoder el |> Result.map List.ofSeq

  module Required =

    let inline internal shell<'T>
      (valueKind: JsonValueKind)
      ([<InlineIfLambda>] decoder: Decoder<'T>)
      (element: JsonElement)
      =
      try
        match element.ValueKind with
        | kind when kind = valueKind -> decoder element
        | kind ->
          Error
            $"Expected '{Enum.GetName valueKind}' but got `%s{Enum.GetName kind}`"
      with ex ->
        Error ex.Message


    let string =
      shell JsonValueKind.String (fun element -> Ok(element.GetString()))

    let boolean (element: JsonElement) =
      try
        match element.ValueKind with
        | JsonValueKind.True
        | JsonValueKind.False -> Ok(element.GetBoolean())
        | kind -> Error $"Expected a boolean but got `%s{Enum.GetName kind}`"
      with ex ->
        Error ex.Message

    let char =
      shell
        JsonValueKind.String
        (fun element ->
          let value = element.GetString()

          if value.Length > 1 then
            Error
              $"Expecting a char but got %s{element.GetRawText()} of size: %i{value.Length}"
          else
            Ok value[0]
        )

    let guid =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetGuid() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get a guid from '%s{element.GetRawText()}'"
        )

    let unit = shell JsonValueKind.Null (fun _ -> Ok())

    let byte =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetByte() with
          | true, byte -> Ok byte
          | _ -> Error $"Unable to get a byte from '%s{element.GetRawText()}'"
        )

    let int =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt32() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get an int from '%s{element.GetRawText()}"
        )

    let int64 =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt64() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get an int64 from '%s{element.GetRawText()}"
        )

    let float =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetDouble() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get a float from '%s{element.GetRawText()}"
        )

    let dateTime =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTime() with
          | true, value -> Ok value
          | _ ->
            Error $"Unable to get a DateTime from '%s{element.GetRawText()}"
        )

    let dateTimeOffset =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTimeOffset() with
          | true, value -> Ok value
          | _ ->
            Error
              $"Unable to get a DateTimeOffset from '%s{element.GetRawText()}"
        )

    let inline property
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> decoder el
      | false, _ ->
        Error $"Property '%s{name}' not found in: {element.GetRawText()}"

  module Optional =

    let inline internal shell<'T>
      (valueKind: JsonValueKind)
      ([<InlineIfLambda>] decoder: Decoder<'T>)
      (element: JsonElement)
      =
      try
        match element.ValueKind with
        | kind when kind = valueKind -> decoder element |> Result.map ValueSome
        | JsonValueKind.Null
        | JsonValueKind.Undefined -> Ok ValueNone
        | kind ->
          Error
            $"Expected '{Enum.GetName valueKind}' but got `%s{Enum.GetName kind}`"
      with ex ->
        Error ex.Message


    let string =
      shell JsonValueKind.String (fun element -> Ok(element.GetString()))

    let boolean (element: JsonElement) =
      try
        match element.ValueKind with
        | JsonValueKind.True
        | JsonValueKind.False -> Ok(ValueSome(element.GetBoolean()))
        | JsonValueKind.Undefined
        | JsonValueKind.Null -> Ok ValueNone
        | kind -> Error $"Expected a boolean but got `%s{Enum.GetName kind}`"
      with ex ->
        Error ex.Message

    let char =
      shell
        JsonValueKind.String
        (fun element ->
          let value = element.GetString()

          if value.Length > 1 then
            Error
              $"Expecting a char but got a string of size: %i{value.Length}"
          else
            Ok value[0]
        )

    let guid =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetGuid() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get a guid from '%s{element.GetRawText()}'"
        )

    let unit = shell JsonValueKind.Null (fun _ -> Ok())

    let byte =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetByte() with
          | true, byte -> Ok byte
          | _ -> Error $"Unable to get a byte from '%s{element.GetRawText()}'"
        )

    let int =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt32() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get an int from '%s{element.GetRawText()}"
        )

    let int64 =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetInt64() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get an int64 from '%s{element.GetRawText()}"
        )

    let float =
      shell
        JsonValueKind.Number
        (fun element ->
          match element.TryGetDouble() with
          | true, value -> Ok value
          | _ -> Error $"Unable to get a float from '%s{element.GetRawText()}"
        )

    let dateTime =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTime() with
          | true, value -> Ok value
          | _ ->
            Error $"Unable to get a DateTime from '%s{element.GetRawText()}"
        )

    let dateTimeOffset =
      shell
        JsonValueKind.String
        (fun element ->
          match element.TryGetDateTimeOffset() with
          | true, value -> Ok value
          | _ ->
            Error
              $"Unable to get a DateTimeOffset from '%s{element.GetRawText()}"
        )

    let inline property
      (name: string)
      ([<InlineIfLambda>] decoder)
      (element: JsonElement)
      =
      match element.TryGetProperty name with
      | true, el -> decoder el |> Result.map ValueSome
      | false, _ -> Ok ValueNone

type Decode =
  static member fromString(value: string, options, decoder) =
    try
      use doc = JsonDocument.Parse(value, options = options)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error ex.Message

  static member fromString(value: string, decoder: Decoder<_>) =
    try
      use doc = JsonDocument.Parse(value)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error ex.Message

  static member fromBytes(value: byte array, options, decoder) =
    try
      use doc = JsonDocument.Parse(value, options = options)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error ex.Message

  static member fromBytes(value: byte array, decoder: Decoder<_>) =
    try
      use doc = JsonDocument.Parse(value)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error ex.Message

  static member fromStream(value: Stream, options, decoder) = task {
    try
      use! doc = JsonDocument.ParseAsync(value, options = options)
      let root = doc.RootElement
      return decoder root
    with ex ->
      return Error ex.Message
  }

  static member fromStream(value: Stream, decoder: Decoder<_>) = task {
    try
      use! doc = JsonDocument.ParseAsync(value)
      let root = doc.RootElement
      return decoder root
    with ex ->
      return Error ex.Message
  }

  // Allow validations too

  static member fromString
    (value: string, options, decoder: DecodeValidated<_>)
    =
    try
      use doc = JsonDocument.Parse(value, options = options)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error [ ex.Message ]

  static member fromString(value: string, decoder: DecodeValidated<_>) =
    try
      use doc = JsonDocument.Parse(value)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error [ ex.Message ]

  static member fromBytes
    (value: byte array, options, decoder: DecodeValidated<_>)
    =
    try
      use doc = JsonDocument.Parse(value, options = options)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error [ ex.Message ]

  static member fromBytes(value: byte array, decoder: DecodeValidated<_>) =
    try
      use doc = JsonDocument.Parse(value)
      let root = doc.RootElement
      decoder root
    with ex ->
      Error [ ex.Message ]

  static member fromStream
    (value: Stream, options, decoder: DecodeValidated<_>)
    =
    task {
      try
        use! doc = JsonDocument.ParseAsync(value, options = options)
        let root = doc.RootElement
        return decoder root
      with ex ->
        return Error [ ex.Message ]
    }

  static member fromStream(value: Stream, decoder: DecodeValidated<_>) = task {
    try
      use! doc = JsonDocument.ParseAsync(value)
      let root = doc.RootElement
      return decoder root
    with ex ->
      return Error [ ex.Message ]
  }

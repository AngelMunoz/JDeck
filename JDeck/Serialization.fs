namespace JDeck

open System
open System.Text.Json
open System.Text.Json.Serialization
open JDeck


exception DecodingException of DecodeError

type private JDeckConverter<'T>
  (
    ?encoder: Encoder<'T>,
    ?decoder: Decoder<'T>,
    ?encoderFactory: JsonSerializerOptions -> Encoder<'T>,
    ?decoderFactory: JsonSerializerOptions -> Decoder<'T>
  ) =
  inherit JsonConverter<'T>()

  override _.CanConvert(typeToConvert: Type) = typeToConvert = typeof<'T>

  override _.Read(reader: byref<Utf8JsonReader>, _: Type, options: JsonSerializerOptions) =
    // Prefer a decoder created with the current options if available
    let activeDecoder =
      match decoderFactory with
      | Some factory -> Some (factory options)
      | None -> decoder

    match activeDecoder with
    | Some dec ->
      use json = JsonDocument.ParseValue(&reader)

      match dec json.RootElement with
      | Ok value -> value
      | Error err -> raise(DecodingException(err))
    | None -> JsonSerializer.Deserialize<'T>(&reader)

  override _.Write
    (writer: Utf8JsonWriter, value, options: JsonSerializerOptions)
    =
    // Prefer an encoder created with the current options if available
    let activeEncoder =
      match encoderFactory with
      | Some factory -> Some (factory options)
      | None -> encoder

    match activeEncoder with
    | Some enc -> enc value |> _.WriteTo(writer)
    | None -> JsonSerializer.Serialize(writer, value, options)

module Codec =
  let useEncoder (encoder: Encoder<'T>) (options: JsonSerializerOptions) =
    options.Converters.Insert(0, JDeckConverter<'T>(encoder = encoder))
    options

  let useDecoder (decoder: Decoder<'T>) (options: JsonSerializerOptions) =
    options.Converters.Insert(0, JDeckConverter<'T>(decoder = decoder))
    options

  let useCodec
    (encoder: Encoder<'T>, decoder: Decoder<'T>)
    (options: JsonSerializerOptions)
    =
    options.Converters.Insert(0,
      JDeckConverter<'T>(encoder = encoder, decoder = decoder)
    )

    options

  /// Registers a decoder factory that receives the active JsonSerializerOptions.
  /// Useful when your decoder composes other decoders via options.Converters or
  /// needs to call Decode.autoJsonOptions with the same options instance.
  let useDecoderWithOptions
    (decoderFactory: JsonSerializerOptions -> Decoder<'T>)
    (options: JsonSerializerOptions)
    =
    options.Converters.Insert(0,
      JDeckConverter<'T>(decoderFactory = decoderFactory)
    )
    options

  /// Registers an encoder factory that receives the active JsonSerializerOptions.
  /// Useful when your encoder wants to honor other registered converters.
  let useEncoderWithOptions
    (encoderFactory: JsonSerializerOptions -> Encoder<'T>)
    (options: JsonSerializerOptions)
    =
    options.Converters.Insert(0,
      JDeckConverter<'T>(encoderFactory = encoderFactory)
    )
    options

  /// Registers both an encoder and a decoder that depend on the active options.
  let useCodecWithOptions
    (
      encoderFactory: JsonSerializerOptions -> Encoder<'T>,
      decoderFactory: JsonSerializerOptions -> Decoder<'T>
    )
    (options: JsonSerializerOptions)
    =
    options.Converters.Insert(0,
      JDeckConverter<'T>(
        encoderFactory = encoderFactory,
        decoderFactory = decoderFactory
      )
    )
    options

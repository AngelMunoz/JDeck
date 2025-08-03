namespace JDeck

open System
open System.Text.Json
open System.Text.Json.Serialization
open JDeck


exception DecodingException of DecodeError

type private JDeckConverter<'T>(?encoder: Encoder<'T>, ?decoder: Decoder<'T>) =
  inherit JsonConverter<'T>()

  override _.CanConvert(typeToConvert: Type) = typeToConvert = typeof<'T>

  override _.Read(reader: byref<Utf8JsonReader>, _: Type, _) =
    match decoder with
    | Some decoder ->
      use json = JsonDocument.ParseValue(&reader)

      match decoder json.RootElement with
      | Ok value -> value
      | Error err -> raise(DecodingException(err))
    | None -> JsonSerializer.Deserialize<'T>(&reader)

  override _.Write
    (writer: Utf8JsonWriter, value, options: JsonSerializerOptions)
    =
    match encoder with
    | Some encoder -> encoder value |> _.WriteTo(writer)
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

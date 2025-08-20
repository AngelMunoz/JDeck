namespace JDeck

open System.Text.Json
open JDeck

exception DecodingException of DecodeError

module Codec =
  val useEncoder: encoder: Encoder<'T> -> options: JsonSerializerOptions -> JsonSerializerOptions
  val useDecoder: decoder: Decoder<'T> -> options: JsonSerializerOptions -> JsonSerializerOptions
  val useCodec: encoder: Encoder<'T> * decoder: Decoder<'T> -> options: JsonSerializerOptions -> JsonSerializerOptions

  val useDecoderWithOptions:
    decoderFactory: (JsonSerializerOptions -> Decoder<'T>) -> options: JsonSerializerOptions -> JsonSerializerOptions

  val useEncoderWithOptions:
    encoderFactory: (JsonSerializerOptions -> Encoder<'T>) -> options: JsonSerializerOptions -> JsonSerializerOptions

  val useCodecWithOptions:
    encoderFactory: (JsonSerializerOptions -> Encoder<'T>) * decoderFactory: (JsonSerializerOptions -> Decoder<'T>) ->
      options: JsonSerializerOptions ->
        JsonSerializerOptions

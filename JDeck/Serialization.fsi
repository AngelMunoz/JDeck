﻿namespace JDeck

open System.Text.Json
open JDeck

module Codec =
  val useEncoder: encoder: Encoder<'T> -> options: JsonSerializerOptions -> JsonSerializerOptions
  val useDecoder: decoder: Decoder<'T> -> options: JsonSerializerOptions -> JsonSerializerOptions
  val useCodec: encoder: Encoder<'T> * decoder: Decoder<'T> -> options: JsonSerializerOptions -> JsonSerializerOptions
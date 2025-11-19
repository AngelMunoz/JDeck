(**
# Codecs and Serialization

> ***Note:*** Please refer to the [Encoding] and [Decoding] guides for more information on the encoding and decoding process.

The main point of JDeck is to avoid the need for manual serialization and deserialization where possible but, it isn't limited to that, you can go the full codec route if that's what you feel like.
In the end this is just a thin wrapper around the `System.Text.Json` serialization library.

While We could provide an interface somewhat like:
*)

(***hide***)

#r "nuget: JDeck, 1.0.0"

open System
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization
open JDeck

module Encode =
  let inline Null () : JsonNode = null

(***show***)

type ICodec<'T> =
  abstract member Encoder: Encoder<'T>
  abstract member Decoder: Decoder<'T>

(**
I think that would be pushing a bit too much wha the library is about. Instead, we provide a few helper functions to make it easier to work and these are located in the Codec module.


Internally JDeck has the following converter:
*)

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

(**
Meaning that you can register coders and decoders *Just* for the type you're having trouble with, you don't really need to create a codec
for each F# type you have in your project. using them is quite simple:
*)

type ContactForm =
  | NoContact
  | Email of string
  | Phone of string


  (** Our Discriminated union has different shapes, two of them match the other doesn't so we have to come up with a format that can handle all of them *)

  static member Encoder: Encoder<ContactForm> =
    fun (contactForm: ContactForm) ->
      Json.object [
        // Let's settle on the { "type": <string>, "value": <string> | null } format
        match contactForm with
        | NoContact ->
          "type", Encode.string "no-contact"
          "value", Encode.Null()
        | Email email ->
          "type", Encode.string "email"
          "value", Encode.string email
        | Phone phone ->
          "type", Encode.string "phone"
          "value", Encode.string phone
      ]
  (***hide***)

  (** Our decoder will also have to accomodate to that, since we control both the serialization and deserialization we can be sure that the format will be consistent *)

  (***show***)
  static member Decoder: Decoder<ContactForm> =
    fun json -> decode {
      let! type' = json |> Required.Property.get("type", Required.string)
      let! value = json |> Required.Property.get("value", Optional.string)

      match type', value with
      | "email", Some value -> return Email value
      | "phone", Some value -> return Phone value
      | "no-contact", None -> return NoContact
      | _ ->
        return!
          DecodeError.ofError(json.Clone(), "Invalid contact form type")
          |> Error
    }

(** Once we have our type and codec in place *)

type Person = {
  name: string
  age: int
  contactForms: ContactForm list
}

let person = {
  name = "John Doe"
  age = 30
  // Keep in mind that business rules may dictate
  // that a person can only have multiple contact forms but,
  // it shouldn't have NoContact alongside Email or Phone.
  // We'll add it here for demonstration purposes.
  contactForms = [ Email "abc@def.com"; Phone "123456789090"; NoContact ]
}

(** While using `Decoding.fromString` is tempting to use here we'll demonstrate that we can use the default JsonSerializer.(De)Serialize methods *)

let json =
  JsonSerializerOptions(PropertyNameCaseInsensitive = true)
  |> Codec.useCodec(ContactForm.Encoder, ContactForm.Decoder)

let str = JsonSerializer.Serialize(person, json)
let person' = JsonSerializer.Deserialize<Person>(str, json)

printfn $"%s{str}"


(**
The output will be (albeit minified, but we'll format it for demonstration purposes):
```json
{
  "name": "John Doe",
  "age": 30,
  "contactForms": [
    { "type": "email", "value": "abc@def.com" },
    { "type": "phone", "value": "123456789090" },
    { "type": "no-contact", "value": null }
  ]
}
```
In the case of the person' assignation, it should look like this:
*)

printfn $"%A{person'}"

{
  name = "John Doe"
  age = 30
  contactForms = [ Email "abc@def.com"; Phone "123456789090"; NoContact ]
}

(**
There's also `Codec.useEncoder` and `Codec.useDecoder` for when you only need to encode or decode a type, respectively.

If your encoder/decoder needs access to the active JsonSerializerOptions (for composing with other converters or calling Decode.autoJsonOptions options), register factories instead:
*)

(***hide***)
let nestedDecoder (opts: JsonSerializerOptions) : Decoder<ContactForm> =
  fun el -> ContactForm.Decoder el // You could call Decode.autoJsonOptions opts here

let nestedEncoder (_opts: JsonSerializerOptions) : Encoder<ContactForm> =
  ContactForm.Encoder
(***show***)

let options =
  JsonSerializerOptions()
  |> Codec.useCodecWithOptions(nestedEncoder, nestedDecoder)

// Now JsonSerializer will use codecs that can leverage the same options instance during (de)serialization.

(**
## Full Codec Way

If for whatever reason you'd prefer to go more manual than automatic, you can use the helper functions in the Decoding type.

```fsharp

// Equivalent to JsonSerializer.Deserialize("""{"id": "1234abcd"}""")
Decoding.auto("""{"id": "1234abcd"}""")

Decoding.fromString("""{"id": "1234abcd"}""", fun json -> decode {
  let! id = json |> Required.Property.get("id", Required.string)
  return { id }
})

Decoding.fromBytes(byteArray,
  fun json -> decode {
    let! id = json |> Required.Property.get("id", Required.string)
    return { id }
  }
)
```
And also `Decoding.fromStream`.
Except from `Decoding.auto` all of these methods require a decoder to be passed in, which most likely means that you already know the shape of the json object you're trying to decode and also supplied the required codecs for each property and its type.
*)

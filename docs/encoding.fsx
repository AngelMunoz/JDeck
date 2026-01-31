(**
# Encoding Guide

While encoding is not really a big concern for <abbr title="System.Text.JSON">STJ</abbr> there's still types that are not supported out of the box.
As usual... Discriminated unions are at the center of the stage here.

That being said, JDeck provides a way to encode values to JSON strings in a similar fashion to the decoders.

An Encoder is defined as:

*)

(***hide***)
#r "../JDeck/bin/Release/netstandard2.0/JDeck.dll"

open System
open JDeck
open System.Text.Json.Nodes

(***show***)
type Encoder<'T> = 'T -> JsonNode

(**

There's two styles offered currently by this library:

- Property list style
- Pipeline style

## Available Encoders

JDeck provides encoders for a wide range of primitive types:

- **Strings**: `Encode.string`
- **Booleans**: `Encode.boolean`
- **Characters**: `Encode.char`
- **Guids**: `Encode.guid`
- **Numeric Types**:
  - `Encode.int` (int32)
  - `Encode.int64` (int64)
  - `Encode.single` (float32)
  - `Encode.float` (float/double)
  - `Encode.int16` (int16)
  - `Encode.uint16` (uint16)
  - `Encode.uint32` (uint32)
  - `Encode.uint64` (uint64)
  - `Encode.byte` (byte)
  - `Encode.sbyte` (sbyte)
  - `Encode.decimal` (decimal)
- **Date/Time Types**:
  - `Encode.dateTime` - ISO 8601 format
  - `Encode.dateTimeOffset` - ISO 8601 format
  - `Encode.timeSpan` - String representation
  - `Encode.dateTimeExact` - Custom format
  - `Encode.dateTimeOffsetExact` - Custom format
  - `Encode.timeSpanExact` - Custom format

*)

let propStyleEncoder =
  Json.object [
    "name", Encode.string "John Doe"
    "age", Encode.int 42
    ("profile", Json.object [ "id", Encode.guid(Guid.NewGuid()) ])
  ]

(**
The property list style is basically just a recollection of key-value pairs in a list.
*)

let pipeStyleEncoder =
  Json.empty()
  |> Encode.property("name", Encode.string "John Doe")
  |> Encode.property("age", Encode.int 42)
  |> Encode.property(
    "profile",
    Json.empty() |> Encode.property("id", Encode.guid(Guid.NewGuid()))
  )

(**
The pipeline style is basically a "builder" like pattern where you start with an empty object and keep adding properties to it.
Both styles are equivalent, and you can choose the one that fits your style better you can even mix and match both! though I wouldn't recommend that.

Let's see a more meaningful example, we'll encode a Person object.
First let's define a couple of types to work with:
*)

type Address = {
  street: string
  city: string
  zip: string option
} with

  static member Encoder: Encoder<Address> =
    fun address ->
      Json.object [
        "street", Encode.string address.street
        "city", Encode.string address.city
        match address.zip with
        | Some zip -> "zip", Encode.string zip
        | None -> ()
      ]

(** It is recommended to define an encoder for whatever type you want to encode in order to keep your code less verbose in the main encoder.
*)

type ContactMethod =
  | Email of string
  | Phone of string

  static member Encoder: Encoder<ContactMethod> =
    fun contactMethod ->
      Json.object [
        match contactMethod with
        | Email email ->
          "type", Encode.string "email"
          "value", Encode.string email
        | Phone phone ->
          "type", Encode.string "phone"
          "value", Encode.string phone
      ]

(**
As we've discussed in other sections of this website, discriminated unions are a particular type that needs special handling when working with System.Text.Json APIs as it is not supported.

Now let's define the Person type and its encoder:
*)

type Person = {
  name: string
  age: int
  address: Address
  contactMethod: ContactMethod list
} with

  static member Encoder: Encoder<Person> =
    fun person ->
      Json.object [
        "name", Encode.string person.name
        "age", Encode.int person.age
        // here we use our previously defined encoders
        "address", Address.Encoder person.address
        // for each contact method we encode it using the ContactMethod encoder
        "contactMethod",
        Json.sequence(person.contactMethod, ContactMethod.Encoder)
      ]
(**
The defined encoder for the Person type uses the previously defined encoders for the Address and ContactMethod types.
For other discriminated unions and custom types you can customize entirely the shape of the final JSON object.
*)

(***hide***)
let person = {
  name = "John Doe"
  age = 42
  address = {
    street = "21 2nd Street"
    city = "New York"
    zip = Some "10021"
  }
  contactMethod = [ Email "abc@dfg.com"; Phone "1234567890" ]
}

(***show***)
let encodedPerson = Person.Encoder person
printfn $"%s{encodedPerson.ToJsonString()}"
(** The final JSON object will look like this:
```json
{
  "name": "John Doe",
  "age": 42,
  "address": { "street": "21 2nd Street", "city": "New York", "zip": "10021" },
  "contactMethod": [
    { "type": "email", "value": "abc@dfg.com" },
    { "type": "phone", "value": "1234567890" }
  ]
}
```
The JSON object above has been formatted for display purposes, but the actual JSON string will be minified if no options are supplied to the `ToJsonString` method.

## Mixed-Type Arrays

Sometimes you need to encode arrays with mixed types, such as discriminated unions represented as `[tag, value]` tuples. JDeck provides the `Json.sequence` overload that accepts a sequence of already-encoded `JsonNode` values, allowing you to mix different types in a single array.

*)

// Encoding a mixed-type array for a discriminated union
type Shape =
  | Box of width: float32 * height: float32
  | Circle of radius: float32

  static member Encoder: Encoder<Shape> =
    fun shape ->
      match shape with
      | Box(w, h) ->
        Json.sequence [ Encode.string "Box"; Encode.single w; Encode.single h ]
      | Circle r -> Json.sequence [ Encode.string "Circle"; Encode.single r ]

let boxShape = Shape.Encoder(Box(10.5f, 20.3f))
printfn $"%s{boxShape.ToJsonString()}" // ["Box",10.5,20.3]

let circleShape = Shape.Encoder(Circle(5.0f))
printfn $"%s{circleShape.ToJsonString()}" // ["Circle",5.0]

(**
The `Json.sequence` overload for `JsonNode seq` is particularly useful when encoding discriminated unions where array elements have different types (e.g., a string tag followed by numeric or object values).

Alternatively, you can use `Encode.mixedSeq` to add encoded values to an existing `JsonArray`:
*)

let arr = JsonArray()
Encode.mixedSeq [ Encode.int 1; Encode.string "hello"; Encode.boolean true ] arr
printfn $"%s{arr.ToJsonString()}" // [1,"hello",true]

(**
This is particularly useful when you need to incrementally build up a mixed-type array or when working with pre-existing `JsonArray` instances.

The encoding story is not set in stone yet for JDeck, and there's still room for improvement, feedback is appreciated in this regard.
*)

namespace JDeck.Tests

open System
open System.Text.Json
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck
open JDeck.Builders

[<TestClass>]
type DecodeBuilderTests() =


  [<TestMethod>]
  member _.``decode can "use", "bind" and "return" a result``() =
    let work = decode {
      use document = JsonDocument.Parse("10")
      let el = document.RootElement

      let! value = Required.int el

      return value
    }

    match work with
    | Ok value -> Assert.AreEqual(10, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``decode can be used within a decoder``() =
    let personDecoder element = decode {
      let! name = element |> Required.Property.get("name", Required.string)
      let! age = element |> Required.Property.get("age", Required.int)
      let! email = Optional.Property.get("email", Required.string) element

      return {|
        name = name
        age = age
        email = email
      |}
    }

    match
      Decode.fromString("""{ "name": "John Doe", "age": 30 }""", personDecoder)
    with
    | Ok person ->
      Assert.AreEqual("John Doe", person.name)
      Assert.AreEqual(30, person.age)
      Assert.AreEqual(None, person.email)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``decode can use "and!" in a decoder``() =

    let personDecoder element = decode {
      let! name = Required.Property.get ("name", Required.string) element
      and! age = Required.Property.get ("age", Required.int) element
      and! email = Optional.Property.get("email", Required.string) element
      and! city = Optional.Property.get("city", Required.string) element
      and! country = Required.Property.get ("country", Required.string) element
      and! phone = Optional.Property.get("phone", Required.string) element
      and! address = Optional.Property.get("address", Required.string) element

      and! postalCode =
        Required.Property.get ("postalCode", Optional.string) element

      and! street = Optional.Property.get("street", Required.string) element

      return {|
        name = name
        age = age
        email = email
        city = city
        country = country
        phone = phone
        address = address
        postalCode = postalCode
        street = street
      |}
    }

    match
      Decode.fromString(
        """{
        "name": "John Doe", "age": 30,
        "country": "USA",
        "postalCode": null,
        "street": "123 Main St"
      }""",
        personDecoder
      )
    with
    | Ok person ->
      Assert.AreEqual("John Doe", person.name)
      Assert.AreEqual(30, person.age)
      Assert.AreEqual(None, person.email)
      Assert.AreEqual(None, person.city)
      Assert.AreEqual("USA", person.country)
      Assert.AreEqual(None, person.phone)
      Assert.AreEqual(None, person.address)
      Assert.AreEqual(None, person.postalCode)
      Assert.AreEqual("123 Main St", person.street.Value)
    | Error err -> Assert.Fail(err.message)

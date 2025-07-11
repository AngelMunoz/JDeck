namespace JDeck.Tests

open System.Text.Json
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck

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
    | Ok value -> Assert.AreEqual<int>(10, value)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``decode can be used within a decoder``() =
    let personDecoder element = decode {
      let! name = element |> Required.Property.get("name", Required.string)
      let! age = element |> Required.Property.get("age", Required.int)
      let! email = Optional.Property.get ("email", Required.string) element

      return {|
        name = name
        age = age
        email = email
      |}
    }

    match
      Decoding.fromString(
        """{ "name": "John Doe", "age": 30 }""",
        personDecoder
      )
    with
    | Ok person ->
      Assert.AreEqual<string>("John Doe", person.name)
      Assert.AreEqual<int>(30, person.age)
      Assert.AreEqual<string option>(None, person.email)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``decode can use "and!" in a decoder``() =

    let personDecoder element = decode {
      let! name = Required.Property.get ("name", Required.string) element
      and! age = Required.Property.get ("age", Required.int) element
      and! email = Optional.Property.get ("email", Required.string) element
      and! city = Optional.Property.get ("city", Required.string) element
      and! country = Required.Property.get ("country", Required.string) element
      and! phone = Optional.Property.get ("phone", Required.string) element
      and! address = Optional.Property.get ("address", Required.string) element

      and! postalCode =
        Required.Property.get ("postalCode", Optional.string) element

      and! street = Optional.Property.get ("street", Required.string) element

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
      Decoding.fromString(
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
      Assert.AreEqual<string>("John Doe", person.name)
      Assert.AreEqual<int>(30, person.age)
      Assert.AreEqual<string option>(None, person.email)
      Assert.AreEqual<string option>(None, person.city)
      Assert.AreEqual<string>("USA", person.country)
      Assert.AreEqual<string option>(None, person.phone)
      Assert.AreEqual<string option>(None, person.address)
      Assert.AreEqual<string option>(None, person.postalCode)
      Assert.AreEqual<string>("123 Main St", person.street.Value)
    | Error err -> Assert.Fail(err.message)

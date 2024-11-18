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
      let! name = Required.property "name" Required.string element
      let! age = Required.property "age" Required.int element
      let! email = Optional.property "email" Required.string element

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
      let! name = Required.property "name" Required.string element
      and! age = Required.property "age" Required.int element
      and! email = Optional.property "email" Required.string element
      and! city = Optional.property "city" Required.string element
      and! country = Required.property "country" Required.string element
      and! phone = Optional.property "phone" Required.string element
      and! address = Optional.property "address" Required.string element
      and! postalCode = Required.property "postalCode" Optional.string element
      and! street = Optional.property "street" Required.string element

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

[<TestClass>]
type DecodeCollectBuilderTests() =

  [<TestMethod>]
  member _.``decode can "use", "bind" and "return" a result``() =
    let work = decodeCollect {

      use document = JsonDocument.Parse("10")
      let el = document.RootElement

      let! value = Required.int el

      return value
    }

    match work with
    | Ok value -> Assert.AreEqual(10, value)
    | Error _ -> Assert.Fail()

  member _.``decode can be used within a decoder``() =
    let personDecoder element = decodeCollect {
      let! name = Required.property "name" Required.string element
      let! age = Required.property "age" Required.int element
      let! emails = Required.arrayProperty "email" (fun _ -> Required.string) element


      return {|
        name = name
        age = age
        emails = emails
      |}
    }

    match
      Decode.validateFromString(
        """{ "name": "John Doe", "age": 30, "emails": ["sample@example.com", "sample_sample@example.com"] }""",
        personDecoder
      )
    with
    | Ok person ->
      Assert.AreEqual("John Doe", person.name)
      Assert.AreEqual(30, person.age)
      Assert.AreEqual(2, person.emails.Length)
      Assert.AreEqual("sample@example.com", person.emails[0])
      Assert.AreEqual("sample_sample@example.com", person.emails[1])

    | Error _ -> Assert.Fail()

  // TODO: Make sure we get back to this test and refactor the sequences overloads
  // [<TestMethod>]
  // member _.``collectDecode collects a list of errors``() =
  //   let work = decodeCollect {
  //     use document = JsonDocument.Parse("""{ "values":  [1, null, "3", 4] }""")
  //     let el = document.RootElement
  //
  //     let! values = Required.collectArrayProperty2 "values" (fun _-> Required.int) el
  //
  //     return values
  //   }
  //
  //   match work with
  //   | Ok _ -> Assert.Fail()
  //   | Error errs ->
  //     Assert.AreEqual(2, errs.Length)
  //     Assert.AreEqual("Expected an integer at index 1", errs[0].message)
  //     Assert.AreEqual("Expected an integer at index 2", errs[1].message)

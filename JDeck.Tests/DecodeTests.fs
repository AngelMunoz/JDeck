namespace JDeck.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck

type UnionSample =
  | A of string
  | B of int
  | C of bool


[<TestClass>]
type DecodingTests() =

  [<TestMethod>]
  member _.``JDeck sequence can decode sequences``() =
    match
      Decoding.fromString("[1,2,3]", Decode.array(fun _ el -> Required.int el))
    with
    | Ok value -> Seq.iteri (fun i v -> Assert.AreEqual(i + 1, v)) value
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck sequence can decode sequences with null values``() =
    match
      Decoding.fromString("[1,null,3]", Decode.array(fun _ el -> Optional.int el))
    with
    | Ok value ->
      let values = Seq.toArray value
      Assert.AreEqual(3, values.Length)
      Assert.AreEqual(Some 1, values[0])
      Assert.AreEqual(None, values[1])
      Assert.AreEqual(Some 3, values[2])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck can use oneOf to decode unions``() =
    let aDecoder value =
      match Required.string value with
      | Ok value -> Ok(A value)
      | Error err -> Error err

    let bDecoder value =
      match Required.int value with
      | Ok value -> Ok(B value)
      | Error err -> Error err

    let cDecoder value =
      match Required.boolean value with
      | Ok value -> Ok(C value)
      | Error err -> Error err

    let unionDecoder =
      Required.Property.get(
        "value",
        (Decode.oneOf [ cDecoder; bDecoder; aDecoder ])
      )

    match Decoding.fromString("""{ "value": "string" }""", unionDecoder) with
    | Ok(A value) -> Assert.AreEqual("string", value)
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

    let unionDecoder =
      Required.Property.get(
        "value",
        (Decode.oneOf [ aDecoder; cDecoder; bDecoder ])
      )

    match Decoding.fromString("""{ "value": 1 }""", unionDecoder) with
    | Ok(B value) -> Assert.AreEqual(1, value)
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

    let unionDecoder =
      Required.Property.get(
        "value",
        (Decode.oneOf [ aDecoder; bDecoder; cDecoder ])
      )

    match Decoding.fromString("""{ "value": true }""", unionDecoder) with
    | Ok(C value) -> Assert.AreEqual(true, value)
    | Ok _ -> Assert.Fail()
    | Error err -> Assert.Fail(err.message)

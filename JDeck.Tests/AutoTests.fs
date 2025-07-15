namespace JDeck.Tests

open System.Text.Json
open Microsoft.VisualStudio.TestTools.UnitTesting

open JDeck

[<AutoOpen>]
module AutoTypes =
  type T1 = { name: string; age: int }

  type ImportMap = {
    imports: Map<string, string>
    scopes: Map<string, Map<string, string>>
    integrity: Map<string, string>
  }

type TC =
  | A of int
  | B
  | C of bool

type T2 = { t1: T1; tcustom: TC }

module ReadmeExamples =
  type Person = {
    Name: string
    Age: int
    Emails: string list
  }

  type ServerResponse = { Data: Person; Message: string }

[<TestClass>]
type AutoTests() =

  [<TestMethod>]
  member _.``JDeck can auto decode things``() =
    match Decoding.auto """{ "name": "John Doe", "age": 30 }""" with
    | Ok person ->
      Assert.AreEqual<string>("John Doe", person.name)
      Assert.AreEqual<int>(30, person.age)
    | Error err -> Assert.Fail(err.message)


  [<TestMethod>]
  member _.``JDeck can auto decode types with nested decoders``() =
    let tcDecoder tc =
      Decode.oneOf
        [
          (Required.int >> Result.map A)
          (Required.boolean >> Result.map C)
          (Required.unit >> Result.map(fun _ -> B))
        ]
        tc

    let options = JsonSerializerOptions() |> Codec.useDecoder tcDecoder

    match
      Decoding.auto(
        """{ "t1": { "name": "John Doe", "age": 30 }, "tcustom": 10 }""",
        options
      )
    with
    | Ok t2 ->
      Assert.AreEqual<string>("John Doe", t2.t1.name)
      Assert.AreEqual<int>(30, t2.t1.age)

      match t2.tcustom with
      | A a -> Assert.AreEqual<int>(10, a)
      | _ -> Assert.Fail()

    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck readme example 1 works``() =

    let json =
      """{"name": "Alice", "age": 30, "emails": ["alice@name.com", "alice@age.com"] }"""

    let result: Result<ReadmeExamples.Person, DecodeError> =
      Decoding.auto(
        json,
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)
      )

    match result with
    | Ok person ->

      Assert.AreEqual<string>("Alice", person.Name)
      Assert.AreEqual<int>(30, person.Age)
      Assert.AreEqual<int>(2, person.Emails.Length)
      Assert.AreEqual<string>("alice@name.com", person.Emails[0])
      Assert.AreEqual<string>("alice@age.com", person.Emails[1])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``JDeck readme example 2 works``() =

    let personDecoder: Decoder<ReadmeExamples.Person> =
      fun person -> decode {
        let! name = person |> Required.Property.get("name", Optional.string)
        and! age = person |> Required.Property.get("age", Required.int)

        and! emails =
          person |> Required.Property.list("emails", Optional.string)

        return {
          Name = name |> Option.defaultValue "<missing name>"
          Age = age
          // Remove any optional value from the list
          Emails = emails |> List.choose id
        }
      }
    // Inconclusive data coming from the server
    let person =
      """{"name": null, "age": 30, "emails": ["alice@name.com", "alice@age.com", null] }"""

    let result: Result<ReadmeExamples.ServerResponse, DecodeError> =
      // ServerResponse will decode automatically while Person will use the custom decoder
      Decoding.auto(
        $$"""{ "data": {{person}}, "message": "Success" }""",
        // Include your own decoder
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)
        |> Codec.useDecoder personDecoder
      )

    match result with
    | Ok { Data = data; Message = message } ->
      Assert.AreEqual<string>("Success", message)
      Assert.AreEqual<int>(30, data.Age)
      Assert.AreEqual<int>(2, data.Emails.Length)
      Assert.AreEqual<string>("alice@name.com", data.Emails[0])
      Assert.AreEqual<string>("alice@age.com", data.Emails[1])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode Map of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    let result: Result<Map<string, string>, DecodeError> = Decoding.auto payload

    match result with
    | Ok map ->
      Assert.AreEqual<int>(3, map.Count)
      Assert.AreEqual<string>("postgres", map.["database"])
      Assert.AreEqual<string>("5432", map.["port"])
      Assert.AreEqual<string>("true", map.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode Map of integers``() =
    let payload = """{ "timeout": 30, "retries": 5, "debug": 1 }"""
    let result: Result<Map<string, int>, DecodeError> = Decoding.auto payload

    match result with
    | Ok map ->
      Assert.AreEqual<int>(3, map.Count)
      Assert.AreEqual<int>(30, map.["timeout"])
      Assert.AreEqual<int>(5, map.["retries"])
      Assert.AreEqual<int>(1, map.["debug"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode nested Map<string, Map<string, string>>``() =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432" }, "config2": { "database": "mysql", "port": "3306" } }"""

    let result: Result<Map<string, Map<string, string>>, DecodeError> =
      Decoding.auto payload

    match result with
    | Ok map ->
      Assert.AreEqual<int>(2, map.Count)
      Assert.IsTrue(map.ContainsKey("config1"))
      Assert.IsTrue(map.ContainsKey("config2"))

      let config1 = map.["config1"]
      Assert.AreEqual<string>("postgres", config1.["database"])
      Assert.AreEqual<string>("5432", config1.["port"])

      let config2 = map.["config2"]
      Assert.AreEqual<string>("mysql", config2.["database"])
      Assert.AreEqual<string>("3306", config2.["port"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode deeply nested Map<string, Map<string, Map<string, string>>>``
    ()
    =
    let payload =
      """{ "environments": { "production": { "database": "postgres", "port": "5432" }, "development": { "database": "mysql", "port": "3306" } } }"""

    let result
      : Result<Map<string, Map<string, Map<string, string>>>, DecodeError> =
      Decoding.auto payload

    match result with
    | Ok map ->
      Assert.AreEqual<int>(1, map.Count)
      Assert.IsTrue(map.ContainsKey("environments"))

      let environments = map.["environments"]
      Assert.AreEqual<int>(2, environments.Count)

      let production = environments.["production"]
      Assert.AreEqual<string>("postgres", production.["database"])
      Assert.AreEqual<string>("5432", production.["port"])

      let development = environments.["development"]
      Assert.AreEqual<string>("mysql", development.["database"])
      Assert.AreEqual<string>("3306", development.["port"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode Map with mixed value types fails gracefully``() =
    let payload = """{ "timeout": 30, "retries": "invalid", "debug": 1 }"""
    let result: Result<Map<string, int>, DecodeError> = Decoding.auto payload

    match result with
    | Ok _ -> Assert.Fail("Expected error but got success")
    | Error err ->
      // Check if the error message contains information about type mismatch
      Assert.IsTrue(
        err.message.Contains("Number")
        || err.message.Contains("String")
        || err.message.Contains("retries"),
        $"Expected error message to contain type information, but got: {err.message}"
      )

  [<TestMethod>]
  member _.``Auto decode empty Map``() =
    let payload = """{}"""
    let result: Result<Map<string, string>, DecodeError> = Decoding.auto payload

    match result with
    | Ok map -> Assert.AreEqual<int>(0, map.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode Dictionary of strings``() =
    let payload =
      """{ "database": "postgres", "port": "5432", "ssl": "true" }"""

    let result
      : Result<
          System.Collections.Generic.Dictionary<string, string>,
          DecodeError
         > =
      Decoding.auto payload

    match result with
    | Ok dict ->
      Assert.AreEqual<int>(3, dict.Count)
      Assert.AreEqual<string>("postgres", dict.["database"])
      Assert.AreEqual<string>("5432", dict.["port"])
      Assert.AreEqual<string>("true", dict.["ssl"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode nested Dictionary<string, Dictionary<string, string>>``
    ()
    =
    let payload =
      """{ "config1": { "database": "postgres", "port": "5432" }, "config2": { "database": "mysql", "port": "3306" } }"""

    let result
      : Result<
          System.Collections.Generic.Dictionary<
            string,
            System.Collections.Generic.Dictionary<string, string>
           >,
          DecodeError
         > =
      Decoding.auto payload

    match result with
    | Ok dict ->
      Assert.AreEqual<int>(2, dict.Count)
      Assert.IsTrue(dict.ContainsKey("config1"))
      Assert.IsTrue(dict.ContainsKey("config2"))

      let config1 = dict.["config1"]
      Assert.AreEqual<string>("postgres", config1.["database"])
      Assert.AreEqual<string>("5432", config1.["port"])

      let config2 = dict.["config2"]
      Assert.AreEqual<string>("mysql", config2.["database"])
      Assert.AreEqual<string>("3306", config2.["port"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode array of Maps``() =
    let payload =
      """[{ "database": "postgres", "port": "5432" }, { "database": "mysql", "port": "3306" }]"""

    let result: Result<Map<string, string>[], DecodeError> =
      Decoding.auto payload

    match result with
    | Ok array ->
      Assert.AreEqual<int>(2, array.Length)

      let first = array.[0]
      Assert.AreEqual<string>("postgres", first.["database"])
      Assert.AreEqual<string>("5432", first.["port"])

      let second = array.[1]
      Assert.AreEqual<string>("mysql", second.["database"])
      Assert.AreEqual<string>("3306", second.["port"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode Map with custom codec``() =
    let payload =
      """{ "user1": { "name": "Alice", "age": 25 }, "user2": { "name": "Bob", "age": 30 } }"""

    // Create custom codec for User type
    let userCodec: Decoder<{| name: string; age: int |}> =
      fun user -> decode {
        let! name = user |> Required.Property.get("name", Required.string)
        let! age = user |> Required.Property.get("age", Required.int)
        return {| name = name; age = age |}
      }

    let options = JsonSerializerOptions()
    options |> Codec.useDecoder userCodec |> ignore

    let result: Result<Map<string, {| name: string; age: int |}>, DecodeError> =
      Decoding.auto(payload, options)

    match result with
    | Ok map ->
      Assert.AreEqual<int>(2, map.Count)

      let user1 = map.["user1"]
      Assert.AreEqual<string>("Alice", user1.name)
      Assert.AreEqual<int>(25, user1.age)

      let user2 = map.["user2"]
      Assert.AreEqual<string>("Bob", user2.name)
      Assert.AreEqual<int>(30, user2.age)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Auto decode complex import map structure``() =
    let payload =
      """{
  "imports": {
    "lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js"
  },
  "scopes": {
    "https://ga.jspm.io/": {
      "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
      "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
      "lit-html": "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
      "lit-html/is-server.js": "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js"
    }
  }
}"""

    // First test the scopes section separately as a nested Map
    let scopesResult: Result<Map<string, Map<string, string>>, DecodeError> =
      Decoding.auto
        """{"https://ga.jspm.io/": {"@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js", "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js", "lit-html": "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js", "lit-html/is-server.js": "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js"}}"""

    match scopesResult with
    | Ok scopesMap ->
      Assert.AreEqual<int>(1, scopesMap.Count)
      Assert.IsTrue(scopesMap.ContainsKey("https://ga.jspm.io/"))

      let scopeUrls = scopesMap.["https://ga.jspm.io/"]
      Assert.AreEqual<int>(4, scopeUrls.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
        scopeUrls.["@lit/reactive-element"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
        scopeUrls.["lit-element/lit-element.js"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
        scopeUrls.["lit-html"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js",
        scopeUrls.["lit-html/is-server.js"]
      )
    | Error err -> Assert.Fail($"Scopes test failed: {err.message}")

    // Now test the imports section separately
    let importsResult: Result<Map<string, string>, DecodeError> =
      Decoding.auto """{"lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js"}"""

    match importsResult with
    | Ok importsMap ->
      Assert.AreEqual<int>(1, importsMap.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit@3.3.1/index.js",
        importsMap.["lit"]
      )
    | Error err -> Assert.Fail($"Imports test failed: {err.message}")

  [<TestMethod>]
  member _.``Auto decode import map with full nested structure``() =
    let payload =
      """{
  "imports": {
    "lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js",
    "react": "https://ga.jspm.io/npm:react@18.2.0/index.js"
  },
  "scopes": {
    "https://ga.jspm.io/": {
      "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
      "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
      "lit-html": "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
      "lit-html/is-server.js": "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js"
    },
    "https://esm.sh/": {
      "react-dom": "https://esm.sh/react-dom@18.2.0",
      "react-router": "https://esm.sh/react-router@6.8.0"
    }
  }
}"""

    // Test the full scopes section as nested Map
    let scopesResult: Result<Map<string, Map<string, string>>, DecodeError> =
      Decoding.auto
        """{
        "https://ga.jspm.io/": {
          "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
          "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
          "lit-html": "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
          "lit-html/is-server.js": "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js"
        },
        "https://esm.sh/": {
          "react-dom": "https://esm.sh/react-dom@18.2.0",
          "react-router": "https://esm.sh/react-router@6.8.0"
        }
      }"""

    match scopesResult with
    | Ok scopesMap ->
      Assert.AreEqual<int>(2, scopesMap.Count)

      // Test jspm.io scope
      let jspmScope = scopesMap.["https://ga.jspm.io/"]
      Assert.AreEqual<int>(4, jspmScope.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
        jspmScope.["@lit/reactive-element"]
      )

      // Test esm.sh scope
      let esmScope = scopesMap.["https://esm.sh/"]
      Assert.AreEqual<int>(2, esmScope.Count)

      Assert.AreEqual<string>(
        "https://esm.sh/react-dom@18.2.0",
        esmScope.["react-dom"]
      )

      Assert.AreEqual<string>(
        "https://esm.sh/react-router@6.8.0",
        esmScope.["react-router"]
      )
    | Error err -> Assert.Fail($"Scopes test failed: {err.message}")

    // Test the imports section
    let importsResult: Result<Map<string, string>, DecodeError> =
      Decoding.auto
        """{"lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js", "react": "https://ga.jspm.io/npm:react@18.2.0/index.js"}"""

    match importsResult with
    | Ok importsMap ->
      Assert.AreEqual<int>(2, importsMap.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit@3.3.1/index.js",
        importsMap.["lit"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:react@18.2.0/index.js",
        importsMap.["react"]
      )
    | Error err -> Assert.Fail($"Imports test failed: {err.message}")

  [<TestMethod>]
  member _.``Real world ImportMap scenario - full structure``() =
    let payload =
      """{
  "imports": {
    "lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js"
  },
  "scopes": {
    "https://ga.jspm.io/": {
      "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
      "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
      "lit-html": "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
      "lit-html/is-server.js": "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js"
    }
  }
}"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = defaultArg imports Map.empty
          scopes = defaultArg scopes Map.empty
          integrity = defaultArg integrity Map.empty
        }
      }

    use doc = JsonDocument.Parse(payload)
    let result = customDecoder doc.RootElement

    match result with
    | Ok importMap ->
      // Verify imports
      Assert.AreEqual<int>(1, importMap.imports.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit@3.3.1/index.js",
        importMap.imports.["lit"]
      )

      // Verify scopes
      Assert.AreEqual<int>(1, importMap.scopes.Count)
      Assert.IsTrue(importMap.scopes.ContainsKey("https://ga.jspm.io/"))

      let scopeUrls = importMap.scopes.["https://ga.jspm.io/"]
      Assert.AreEqual<int>(4, scopeUrls.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
        scopeUrls.["@lit/reactive-element"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
        scopeUrls.["lit-element/lit-element.js"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
        scopeUrls.["lit-html"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js",
        scopeUrls.["lit-html/is-server.js"]
      )

      // Verify integrity (should be empty since not provided)
      Assert.AreEqual<int>(0, importMap.integrity.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Real world ImportMap scenario - imports only with null scopes``() =
    let payload =
      """{
  "imports": {
    "lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js"
  },
  "scopes": null
}"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = imports |> Option.defaultValue Map.empty
          scopes = scopes |> Option.defaultValue Map.empty
          integrity = integrity |> Option.defaultValue Map.empty
        }
      }

    use doc = JsonDocument.Parse(payload)
    let result = customDecoder doc.RootElement

    match result with
    | Ok importMap ->
      // Verify imports
      Assert.AreEqual<int>(1, importMap.imports.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit@3.3.1/index.js",
        importMap.imports.["lit"]
      )

      // Verify scopes (should be empty due to null)
      Assert.AreEqual<int>(0, importMap.scopes.Count)

      // Verify integrity (should be empty since not provided)
      Assert.AreEqual<int>(0, importMap.integrity.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Real world ImportMap scenario - scopes only with null imports``() =
    let payload =
      """{
  "imports": null,
  "scopes": {
    "https://ga.jspm.io/": {
      "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
      "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
      "lit-html": "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
      "lit-html/is-server.js": "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js"
    }
  }
}"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = imports |> Option.defaultValue Map.empty
          scopes = scopes |> Option.defaultValue Map.empty
          integrity = integrity |> Option.defaultValue Map.empty
        }
      }

    use doc = JsonDocument.Parse(payload)
    let result = customDecoder doc.RootElement

    match result with
    | Ok importMap ->
      // Verify imports (should be empty due to null)
      Assert.AreEqual<int>(0, importMap.imports.Count)

      // Verify scopes
      Assert.AreEqual<int>(1, importMap.scopes.Count)
      Assert.IsTrue(importMap.scopes.ContainsKey("https://ga.jspm.io/"))

      let scopeUrls = importMap.scopes.["https://ga.jspm.io/"]
      Assert.AreEqual<int>(4, scopeUrls.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
        scopeUrls.["@lit/reactive-element"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
        scopeUrls.["lit-element/lit-element.js"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-html@3.3.1/development/lit-html.js",
        scopeUrls.["lit-html"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-html@3.3.1/development/is-server.js",
        scopeUrls.["lit-html/is-server.js"]
      )

      // Verify integrity (should be empty since not provided)
      Assert.AreEqual<int>(0, importMap.integrity.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Real world ImportMap scenario - with integrity field``() =
    let payload =
      """{
  "imports": {
    "lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js",
    "react": "https://ga.jspm.io/npm:react@18.2.0/index.js"
  },
  "scopes": {
    "https://ga.jspm.io/": {
      "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
      "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js"
    }
  },
  "integrity": {
    "lit": "sha384-abc123",
    "react": "sha384-def456"
  }
}"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = defaultArg imports Map.empty
          scopes = defaultArg scopes Map.empty
          integrity = defaultArg integrity Map.empty
        }
      }

    use doc = JsonDocument.Parse(payload)
    let result = customDecoder doc.RootElement

    match result with
    | Ok importMap ->
      // Verify imports
      Assert.AreEqual<int>(2, importMap.imports.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit@3.3.1/index.js",
        importMap.imports.["lit"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:react@18.2.0/index.js",
        importMap.imports.["react"]
      )

      // Verify scopes
      Assert.AreEqual<int>(1, importMap.scopes.Count)
      let scopeUrls = importMap.scopes.["https://ga.jspm.io/"]
      Assert.AreEqual<int>(2, scopeUrls.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
        scopeUrls.["@lit/reactive-element"]
      )

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js",
        scopeUrls.["lit-element/lit-element.js"]
      )

      // Verify integrity
      Assert.AreEqual<int>(2, importMap.integrity.Count)
      Assert.AreEqual<string>("sha384-abc123", importMap.integrity.["lit"])
      Assert.AreEqual<string>("sha384-def456", importMap.integrity.["react"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Real world ImportMap scenario - empty object``() =
    let payload = """{}"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = defaultArg imports Map.empty
          scopes = defaultArg scopes Map.empty
          integrity = defaultArg integrity Map.empty
        }
      }

    use doc = JsonDocument.Parse(payload)
    let result = customDecoder doc.RootElement

    match result with
    | Ok importMap ->
      // All fields should be empty
      Assert.AreEqual<int>(0, importMap.imports.Count)
      Assert.AreEqual<int>(0, importMap.scopes.Count)
      Assert.AreEqual<int>(0, importMap.integrity.Count)
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``Real world ImportMap scenario - auto decode with custom decoder``
    ()
    =
    let payload =
      """{
  "imports": {
    "lit": "https://ga.jspm.io/npm:lit@3.3.1/index.js"
  },
  "scopes": {
    "https://ga.jspm.io/": {
      "@lit/reactive-element": "https://ga.jspm.io/npm:@lit/reactive-element@2.1.1/development/reactive-element.js",
      "lit-element/lit-element.js": "https://ga.jspm.io/npm:lit-element@4.2.1/development/lit-element.js"
    }
  },
  "integrity": {
    "lit": "sha384-abc123"
  }
}"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = defaultArg imports Map.empty
          scopes = defaultArg scopes Map.empty
          integrity = defaultArg integrity Map.empty
        }
      }

    let options = JsonSerializerOptions()
    options |> Codec.useDecoder customDecoder |> ignore

    let result: Result<ImportMap, DecodeError> = Decoding.auto(payload, options)

    match result with
    | Ok importMap ->
      // Verify all fields are populated correctly
      Assert.AreEqual<int>(1, importMap.imports.Count)

      Assert.AreEqual<string>(
        "https://ga.jspm.io/npm:lit@3.3.1/index.js",
        importMap.imports.["lit"]
      )

      Assert.AreEqual<int>(1, importMap.scopes.Count)
      let scopeUrls = importMap.scopes.["https://ga.jspm.io/"]
      Assert.AreEqual<int>(2, scopeUrls.Count)

      Assert.AreEqual<int>(1, importMap.integrity.Count)
      Assert.AreEqual<string>("sha384-abc123", importMap.integrity.["lit"])
    | Error err -> Assert.Fail(err.message)

  [<TestMethod>]
  member _.``ImportMap decoder handles all null properties correctly``() =
    let payload = """{ "imports": null, "scopes": null, "integrity": null }"""

    let customDecoder: Decoder<ImportMap> =
      fun el -> decode {
        let! imports = el |> Optional.Property.map("imports", Required.string)

        let! scopes =
          el |> Optional.Property.map("scopes", Required.map Required.string)

        let! integrity =
          el |> Optional.Property.map("integrity", Required.string)

        return {
          imports = defaultArg imports Map.empty
          scopes = defaultArg scopes Map.empty
          integrity = defaultArg integrity Map.empty
        }
      }

    use doc = JsonDocument.Parse(payload)
    let result = customDecoder doc.RootElement

    match result with
    | Ok importMap ->
      // Verify all fields are empty maps (not null)
      Assert.AreEqual<int>(0, importMap.imports.Count)
      Assert.AreEqual<int>(0, importMap.scopes.Count)
      Assert.AreEqual<int>(0, importMap.integrity.Count)
      
      // Verify that when casting maps to obj, they are not null
      Assert.IsNotNull(importMap.imports :> obj)
      Assert.IsNotNull(importMap.scopes :> obj)
      Assert.IsNotNull(importMap.integrity :> obj)
    | Error err -> Assert.Fail(err.message)

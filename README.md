# JDeck

A [Thoth.Json]-like Json decoder based on `System.Text.Json.JsonDocument` in a single file with no external dependencies.

Just copy and paste the `Library.fs` (while I get the NuGet out) and you're ready to go, feel free to check out the tests to see examples of the usage

Keep in mind this is not as thoughtful and complete as Thoth.Json, and perhaps a thoth "frontend" can be done using the [Advanced API](https://github.com/thoth-org/Thoth.Json/blob/main/packages/Thoth.Json.Core/Decode.fs#L84)

However I'm in a situation where I need a thoth-like decoder and have to deal with the BCL, so I figured out it was worth trying something out

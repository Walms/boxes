module BoxTracker.Api.Tests.LabelsTests

open Xunit
open BoxTracker.Labels

[<Fact>]
let ``generateQrSvg produces an SVG document`` () : unit =
    let svg = generateQrSvg "BOX-001" 10
    Assert.Contains("<svg", svg)
    Assert.Contains("</svg>", svg)

[<Fact>]
let ``boxLabelHtml includes the box id and label`` () : unit =
    let html = boxLabelHtml "BOX-001" (Some "Kitchen") (Some "GARAGE") (Some "Garage")
    Assert.Contains("BOX-001", html)
    Assert.Contains("Kitchen", html)
    Assert.Contains("<svg", html)
    Assert.Contains("<!DOCTYPE html>", html)

[<Fact>]
let ``boxLabelHtml without a label uses the large id layout`` () : unit =
    let html = boxLabelHtml "BOX-002" None None None
    Assert.Contains("BOX-002", html)
    Assert.Contains("label-id-large", html)

[<Fact>]
let ``boxLabelHtml escapes HTML-special characters in the label`` () : unit =
    let html = boxLabelHtml "BOX-003" (Some "Tom & Jerry <toys>") None None
    Assert.Contains("Tom &amp; Jerry &lt;toys&gt;", html)
    Assert.DoesNotContain("<toys>", html)

[<Fact>]
let ``locationLabelHtml includes the code and name`` () : unit =
    let html = locationLabelHtml "GARAGE" "Main Garage"
    Assert.Contains("GARAGE", html)
    Assert.Contains("Main Garage", html)
    Assert.Contains("<svg", html)

[<Fact>]
let ``locationLabelHtml escapes the name`` () : unit =
    let html = locationLabelHtml "GARAGE" "A & B"
    Assert.Contains("A &amp; B", html)

[<Fact>]
let ``batchBoxLabelHtml renders one label block per box`` () : unit =
    let boxes =
        [ ("BOX-001", Some "Tools", Some "GARAGE", Some "Garage")
          ("BOX-002", None, None, None)
          ("BOX-003", Some "Books", None, None) ]
    let html = batchBoxLabelHtml boxes
    Assert.Contains("BOX-001", html)
    Assert.Contains("BOX-002", html)
    Assert.Contains("BOX-003", html)
    // Each box produces exactly one <div class="label"> block.
    let occurrences (needle: string) (haystack: string) : int =
        haystack.Split([| needle |], System.StringSplitOptions.None).Length - 1
    Assert.Equal(3, occurrences "class=\"label\"" html)

[<Fact>]
let ``batchBoxLabelHtml on an empty list still produces a valid document`` () : unit =
    let html = batchBoxLabelHtml []
    Assert.Contains("<!DOCTYPE html>", html)
    Assert.Contains("</html>", html)

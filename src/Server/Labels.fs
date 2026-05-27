module BoxTracker.Labels

open QRCoder

let generateQrSvg (text: string) (pixelsPerModule: int) : string =
    use generator : QRCodeGenerator = new QRCodeGenerator()
    generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q)
    |> fun data ->
        use svg : SvgQRCode = new SvgQRCode(data)
        svg.GetGraphic(pixelsPerModule)

let private escapeHtml (s: string) : string =
    s.Replace("&", "&amp;")
     .Replace("<", "&lt;")
     .Replace(">", "&gt;")
     .Replace("\"", "&quot;")

let private labelPageCss : string =
    """
    @page {
        size: 100mm 60mm;
        margin: 0;
    }
    body {
        margin: 0;
        padding: 0;
    }
    .label {
        width: 100mm;
        height: 60mm;
        box-sizing: border-box;
        padding: 3mm;
        display: flex;
        align-items: center;
        gap: 3mm;
        page-break-after: always;
        font-family: Arial, Helvetica, sans-serif;
    }
    .label-qr {
        flex-shrink: 0;
    }
    .label-qr svg {
        width: 48mm;
        height: 48mm;
        display: block;
    }
    .label-text {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
        justify-content: center;
        overflow: hidden;
    }
    .label-id {
        font-size: 14pt;
        font-weight: bold;
        word-break: break-all;
    }
    .label-sub {
        font-size: 10pt;
        margin-top: 1mm;
        word-break: break-all;
    }
    .label-location {
        font-size: 9pt;
        margin-top: 1mm;
        color: #555;
        word-break: break-all;
    }
    .no-print {
        display: block;
        padding: 10px;
        font-family: Arial, Helvetica, sans-serif;
    }
    @media print {
        .no-print { display: none !important; }
    }
    """

let boxLabelHtml (boxId: string) (label: string option) (locationCode: string option) (locationName: string option) : string =
    let qrSvg : string = generateQrSvg boxId 10
    let labelLine : string =
        match label with
        | Some l -> $"<div class=\"label-sub\">%s{escapeHtml l}</div>"
        | None -> ""
    let locationLine : string =
        match locationCode, locationName with
        | Some code, Some name -> $"<div class=\"label-location\">%s{escapeHtml name} (%s{escapeHtml code})</div>"
        | Some code, None -> $"<div class=\"label-location\">%s{escapeHtml code}</div>"
        | _ -> ""
    $"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Label %s{boxId}</title>
<style>%s{labelPageCss}</style>
</head>
<body>
<div class="no-print">
  <button onclick="window.print()">Print Label</button>
</div>
<div class="label">
  <div class="label-qr">%s{qrSvg}</div>
  <div class="label-text">
    <div class="label-id">%s{escapeHtml boxId}</div>
    %s{labelLine}
    %s{locationLine}
  </div>
</div>
</body>
</html>"""

let locationLabelHtml (code: string) (name: string) : string =
    let qrSvg : string = generateQrSvg code 10
    $"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Label %s{code}</title>
<style>%s{labelPageCss}</style>
</head>
<body>
<div class="no-print">
  <button onclick="window.print()">Print Label</button>
</div>
<div class="label">
  <div class="label-qr">%s{qrSvg}</div>
  <div class="label-text">
    <div class="label-id">%s{escapeHtml code}</div>
    <div class="label-sub">%s{escapeHtml name}</div>
  </div>
</div>
</body>
</html>"""

let batchBoxLabelHtml (boxes: (string * string option * string option * string option) list) : string =
    let labelsHtml : string =
        boxes
        |> List.map (fun (boxId, label, locationCode, locationName) ->
            let qrSvg : string = generateQrSvg boxId 10
            let labelLine : string =
                match label with
                | Some l -> $"<div class=\"label-sub\">%s{escapeHtml l}</div>"
                | None -> ""
            let locationLine : string =
                match locationCode, locationName with
                | Some code, Some name -> $"<div class=\"label-location\">%s{escapeHtml name} (%s{escapeHtml code})</div>"
                | Some code, None -> $"<div class=\"label-location\">%s{escapeHtml code}</div>"
                | _ -> ""
            $"""<div class="label">
  <div class="label-qr">%s{qrSvg}</div>
  <div class="label-text">
    <div class="label-id">%s{escapeHtml boxId}</div>
    %s{labelLine}
    %s{locationLine}
  </div>
</div>""")
        |> String.concat "\n"
    $"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Box Labels</title>
<style>%s{labelPageCss}</style>
</head>
<body>
<div class="no-print">
  <button onclick="window.print()">Print All Labels</button>
</div>
%s{labelsHtml}
</body>
</html>"""

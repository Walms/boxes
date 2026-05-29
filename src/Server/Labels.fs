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
        padding: 0;
    }
    body {
        margin: 0;
        padding: 0;
        font-family: 'Courier New', 'Courier', monospace;
        background: white;
    }
    .label {
        width: 100mm;
        height: 60mm;
        box-sizing: border-box;
        padding: 2mm;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 1mm;
        page-break-after: always;
        font-family: 'Courier New', 'Courier', monospace;
        background: white !important;
        border: 2px solid #000 !important;
        box-shadow: inset 0 0 0 1px #000;
    }
    .label-qr {
        flex-shrink: 0;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .label-qr svg {
        width: 42mm;
        height: 42mm;
        display: block;
    }
    .label-text {
        width: 100%;
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        font-family: 'Courier New', 'Courier', monospace;
    }
    .label-id {
        font-size: 10pt;
        font-weight: bold;
        letter-spacing: 0.5px;
        line-height: 1.2;
        word-break: break-all;
    }
    .label-sub {
        font-size: 9pt;
        margin-top: 0.5mm;
        word-break: break-word;
        line-height: 1.3;
    }
    .no-print {
        display: block;
        padding: 10px;
        font-family: 'Courier New', 'Courier', monospace;
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
            $"""<div class="label">
  <div class="label-qr">%s{qrSvg}</div>
  <div class="label-text">
    <div class="label-id">%s{escapeHtml boxId}</div>
    %s{labelLine}
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

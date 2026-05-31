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
        padding: 2mm 3mm;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 1.5mm;
        page-break-after: always;
        font-family: 'Courier New', 'Courier', monospace;
        background: white !important;
        border: 2px solid #000 !important;
        box-shadow: inset 0 0 0 1px #000;
    }
    .label-name {
        font-size: 30pt;
        font-weight: bold;
        line-height: 1.1;
        word-break: break-word;
        text-align: center;
        width: 100%;
    }
    .label-footer {
        display: flex;
        flex-direction: row;
        align-items: center;
        justify-content: center;
        gap: 3mm;
        width: 100%;
    }
    .label-qr {
        flex-shrink: 0;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .label-qr svg {
        width: 20mm;
        height: 20mm;
        display: block;
    }
    .label-qr-large svg {
        width: 34mm;
        height: 34mm;
        display: block;
    }
    .label-id {
        font-size: 11pt;
        font-weight: bold;
        letter-spacing: 0.5px;
        line-height: 1.3;
        word-break: break-all;
        text-align: center;
    }
    .label-id-large {
        font-size: 20pt;
        font-weight: bold;
        letter-spacing: 0.5px;
        line-height: 1.2;
        word-break: break-all;
        text-align: center;
        width: 100%;
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
    let body : string =
        match label with
        | Some l ->
            $"""  <div class="label-name">%s{escapeHtml l}</div>
  <div class="label-footer">
    <div class="label-qr">%s{qrSvg}</div>
    <div class="label-id">%s{escapeHtml boxId}</div>
  </div>"""
        | None ->
            $"""  <div class="label-qr label-qr-large">%s{qrSvg}</div>
  <div class="label-id-large">%s{escapeHtml boxId}</div>"""
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
%s{body}
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
  <div class="label-name">%s{escapeHtml name}</div>
  <div class="label-footer">
    <div class="label-qr">%s{qrSvg}</div>
    <div class="label-id">%s{escapeHtml code}</div>
  </div>
</div>
</body>
</html>"""

let batchBoxLabelHtml (boxes: (string * string option * string option * string option) list) : string =
    let labelsHtml : string =
        boxes
        |> List.map (fun (boxId, label, locationCode, locationName) ->
            let qrSvg : string = generateQrSvg boxId 10
            let body : string =
                match label with
                | Some l ->
                    $"""  <div class="label-name">%s{escapeHtml l}</div>
  <div class="label-footer">
    <div class="label-qr">%s{qrSvg}</div>
    <div class="label-id">%s{escapeHtml boxId}</div>
  </div>"""
                | None ->
                    $"""  <div class="label-qr label-qr-large">%s{qrSvg}</div>
  <div class="label-id-large">%s{escapeHtml boxId}</div>"""
            $"""<div class="label">
%s{body}
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

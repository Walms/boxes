module BoxTracker.Client.App

#if FABLE_COMPILER
open Browser.Types
open Browser.Dom
open Fable.Core.JsInterop

let private createRoot (el: Element) : obj = import "createRoot" "react-dom/client"
#endif
open Feliz
open Feliz.UseElmish
open BoxTracker.Client.State
open BoxTracker.Client.Pages.Router

[<ReactComponent>]
let App () : ReactElement =
    let (state, dispatch) = React.useElmish(init, update)
    renderPage state dispatch

#if FABLE_COMPILER
let root = createRoot (document.getElementById "root")
root?render(App())
#endif

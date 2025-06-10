open Suave
open Suave.Filters
open Suave.Operators
open Newtonsoft.Json

let app = 
    choose [
        POST >=> path "/webhook" >=> Payload.check
    ]

[<EntryPoint>]
let main argv =
    startWebServer defaultConfig app
    0 

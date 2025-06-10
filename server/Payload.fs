module Payload 

open Suave
open System.Text
open Newtonsoft.Json
open Types
open Api
open System.Collections.Concurrent
open System

let (>>=) r f = Result.bind f r
let processedTransactions = ConcurrentDictionary<string, bool>()

let validateToken (token: string) : bool =
    token = "meu-token-secreto"

let parseJsonResult (context: HttpContext) : payload =
    let body = context.request.rawForm |> Encoding.UTF8.GetString
    if String.IsNullOrWhiteSpace body then
        { event = None; transaction_id = None; amount = None; currency = None; timestamp = None }
    else
        try
            let result = JsonConvert.DeserializeObject<payloadIn> body
            {
                event = Some result.event
                transaction_id = Some result.transaction_id
                amount = Some result.amount
                currency = Some result.currency
                timestamp = Some result.timestamp
            }
        with ex ->
            { event = None; transaction_id = None; amount = None; currency = None; timestamp = None }

let agent = MailboxProcessor.Start(fun inbox ->
    let rec loop () = async {
        let! transaction_id = inbox.Receive()
        match transaction_id with
        | Confirm id -> do! confirmCall id |> Async.Ignore
        | Cancel id -> do! cancelCall id |> Async.Ignore
        return! loop ()
    }
    loop ()
)

let payloadVerify (pld: payload) (context: HttpContext) : Async<HttpContext option> =
    let transaction_id = pld.transaction_id

    if processedTransactions.ContainsKey transaction_id.Value then
        let response = { message = "Transação já processada" }
        RequestErrors.BAD_REQUEST (JsonConvert.SerializeObject response) context

    else
        processedTransactions.TryAdd(transaction_id.Value, true) |> ignore
        if pld.amount.Value > 0.0 && pld.event.Value = "payment_success" && pld.currency.Value <> "" && pld.timestamp.Value <> ""  then
            agent.Post(Confirm transaction_id.Value)
            let response = { message = "Transação processada com sucesso" }
            Successful.OK (JsonConvert.SerializeObject response) context
        else
            processedTransactions.TryRemove(transaction_id.Value) |> ignore
            agent.Post(Cancel transaction_id.Value)
            let response = { message = "Transação já processada" }
            RequestErrors.BAD_REQUEST (JsonConvert.SerializeObject response) context

let check (context: HttpContext) : Async<HttpContext option> =
    async {
        match context.request.header "X-Webhook-Token" with
        | Choice1Of2 token -> 
            if validateToken token then
                let data = parseJsonResult context
                return! payloadVerify data context
            else 
                let response = { message = "invalid token" }
                return! RequestErrors.UNAUTHORIZED (JsonConvert.SerializeObject response) context
        | Choice2Of2 err -> 
                let response = { message = "invalid token" }
                return! RequestErrors.BAD_REQUEST (JsonConvert.SerializeObject response) context
    }
    



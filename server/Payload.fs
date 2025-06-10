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
    printfn "Validando token: %s" token
    token = "meu-token-secreto"

let parseJsonResult (context: HttpContext) : payload =
    let body = context.request.rawForm |> Encoding.UTF8.GetString
    if String.IsNullOrWhiteSpace body then
        printfn "Corpo da requisição vazio ou nulo"
        { event = None; transaction_id = None; amount = None; currency = None; timestamp = None }
    else
        try
            let result = JsonConvert.DeserializeObject<payloadIn> body
            printfn "JSON parseado com sucesso: %A" result
            {
                event = Some result.event
                transaction_id = Some result.transaction_id
                amount = Some result.amount
                currency = Some result.currency
                timestamp = Some result.timestamp
            }
        with ex ->
            printfn "Erro ao fazer parse do JSON: %s" ex.Message
            { event = None; transaction_id = None; amount = None; currency = None; timestamp = None }

let agent = MailboxProcessor.Start(fun inbox ->
    let rec loop () = async {
        let! transaction_id = inbox.Receive()

        match transaction_id with
        | Confirm id ->
            let! result = confirmCall id
            printfn "Confirmação enviada: %A" result
        | Cancel id ->
            let! result = cancelCall id
            printfn "Cancelamento enviado: %A" result

        return! loop ()
    }
    loop ()
)

let payloadVerify (pld: payload) (context: HttpContext) : Async<HttpContext option> =
    let transaction_id = pld.transaction_id

    if processedTransactions.ContainsKey transaction_id.Value then
        printfn "Transação já processada: %s" transaction_id.Value
        let response = { message = "Transação já processada" }
        RequestErrors.BAD_REQUEST (JsonConvert.SerializeObject response) context

    else
        processedTransactions.TryAdd(transaction_id.Value, true) |> ignore
        if pld.amount.Value > 0.0 && pld.currency.Value <> "" && pld.timestamp.Value <> "" && pld.event.Value <> "" then
            printfn "Transação válida: %A" pld
            agent.Post(Confirm transaction_id.Value)
            let response = { message = "Transação processada com sucesso" }
            Successful.OK (JsonConvert.SerializeObject response) context
        else
            printfn "Transação inválida: %A" pld
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
                printfn "Dados recebidos: %A" data
                return! payloadVerify data context
            else 
                let response = { message = "invalid token" }
                return! RequestErrors.UNAUTHORIZED (JsonConvert.SerializeObject response) context
        | Choice2Of2 err -> 
                let response = { message = "invalid token" }
                return! RequestErrors.BAD_REQUEST (JsonConvert.SerializeObject response) context
    }
    



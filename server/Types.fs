module Types

open Newtonsoft.Json

type payloadIn = {
    [<JsonProperty("event")>]
    event: string

    [<JsonProperty("transaction_id")>]
    transaction_id: string

    [<JsonProperty("amount")>]
    amount: float

    [<JsonProperty("currency")>]
    currency: string
    
    [<JsonProperty("timestamp")>]
    timestamp: string
}

type payload = {
    event: string option
    transaction_id: string option
    amount: float option
    currency: string option
    timestamp: string option
}

type ConfirmMessage =
    | Confirm of string
    | Cancel of string 

type Output = {
  transaction_id: string
}

type ResponseMessage = {
  message: string
}
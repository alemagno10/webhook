module Api

open System.Net.Http
open Newtonsoft.Json
open System.Text
open Types

let httpClient = new HttpClient()

let apiCall (url: string) (id: string) : Async<Result<string, string>> = async {
    try
        let id = { transaction_id = id }
        let json = JsonConvert.SerializeObject id
        use content = new StringContent(json, Encoding.UTF8, "application/json")
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! body = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Ok body
        else
            return Error (sprintf "Erro %d: %s" (int response.StatusCode) response.ReasonPhrase)
    with ex ->
        return Error ex.Message
}

let confirmCall id = apiCall "http://127.0.0.1:5001/confirmar/" id
let cancelCall id = apiCall "http://127.0.0.1:5001/cancelar/" id

# Webhook de Pagamento Funcional (F#)
Este repositório contém uma implementação de um webhook de pagamento desenvolvido em F#, utilizando princípios de programação funcional para garantir robustez, testabilidade e manutenibilidade.

## Objetivo do Projeto
O objetivo principal deste webhook é confirmar e registrar pagamentos recebidos de forma assíncrona. Ele lida com requisições HTTP, realiza validações do payload, gerencia a idempotência das transações e comunica o status para serviços externos.

## Principais Características
- Validação de Payload: Verifica a integridade e correção dos dados recebidos no payload JSON.

- Segurança: Autentica requisições através de um token de segurança (X-Webhook-Token).

- Idempotência: Garante que cada transação seja processada apenas uma vez, prevenindo duplicações, mesmo em caso de retentativas.

- Comunicação Assíncrona: Utiliza MailboxProcessor para gerenciar chamadas de API externas de confirmação/cancelamento de forma eficiente e sem bloquear o processamento principal.

- Lógica Condicional: Realiza ações específicas (confirmação, cancelamento) baseadas no status da transação e na validação do payload.

- Respostas HTTP Controladas: Retorna códigos de status HTTP apropriados para indicar o resultado do processamento.

## Por Que Programação Funcional?
A escolha da programação funcional para este webhook trouxe diversos benefícios:

- Ausência de Efeitos Colaterais: Funções puras facilitam o teste e a depuração, pois a saída é sempre a mesma para a mesma entrada, sem depender ou modificar estados externos.

- Imutabilidade: Reduz drasticamente a chance de bugs relacionados a concorrência, já que os dados não são alterados após a criação.

- Funções Puras: Promovem um isolamento claro da lógica de negócio, tornando o código mais previsível e fácil de raciocinar.

- Composição: Permite a construção de "pipelines" de transformação de dados complexos a partir de funções menores e mais simples, aumentando a legibilidade e a flexibilidade.

## Como Executar
Para rodar o webhook, você precisará ter o .NET SDK (com suporte a F#) instalado.

Clone o Repositório:

    git clone https://github.com/alemagno10/webhook
    cd webhook

Execute o Servidor:

    dotnet run

Isso iniciará o servidor Suave na porta 8080.

### Servidor de Confirmação/Cancelamento (Mock)
O webhook realiza chamadas para ```http://127.0.0.1:5001/confirmar/``` e ```http://127.0.0.1:5001/cancelar/```. Para testar a funcionalidade completa, você pode usar um servidor mock simples. Aqui está um exemplo de um servidor Python Flask que pode ser usado:

## Test_webhook.py

Para rodar o servidor mock (em um terminal separado):

    pip install -r requirements.txt
    python test_webhook.py

O webhook espera requisições POST para o endpoint /webhook.

Exemplo de Payload Válido:
```
{
    "event": "payment_success",
    "transaction_id": "abc123",
    "amount": 49.90,
    "currency": "BRL",
    "timestamp": "2025-05-11T16:00:00Z"
}
```

## Estrutura do Projeto
O projeto é organizado em módulos lógicos:

- program.fs: O ponto de entrada da aplicação, onde o servidor Suave é configurado e iniciado.

- Types.fs: Define os tipos de dados utilizados na aplicação, incluindo a estrutura do payload de entrada e saída.

- Api.fs: Contém a lógica para realizar chamadas HTTP assíncronas para os endpoints de confirmação e cancelamento.

- Payload.fs: O módulo principal que lida com o processamento do webhook:

    - Validação do token (validateToken).

    - Desserialização do JSON (parseJsonResult).

    - Lógica de idempotência (processedTransactions).

    - Validação de regras de negócio (payloadVerify).

  - Envio assíncrono de mensagens para o MailboxProcessor (agent) para chamadas de confirmação/cancelamento.


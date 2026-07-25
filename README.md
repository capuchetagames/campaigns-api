# Campaigns API

API para gerenciamento de campanhas e doações, construída com ASP.NET Core 8.

## Visão geral

A aplicação expõe endpoints para:
- gestão de campanhas (criação, atualização, listagem e remoção);
- registro de doações;
- painel público de campanhas ativas;
- health check e métricas Prometheus.

Também integra com:
- PostgreSQL (persistência);
- Redis (cache);
- RabbitMQ (publicação de eventos);
- DynamoDB (logs estruturados);
- JWT (autenticação/autorização por papel).

## Estrutura do repositório

- `/CampaignsApi`: projeto Web API (controllers, middlewares, DI e configuração).
- `/Core`: entidades, contratos, DTOs e enums de domínio.
- `/Infrastructure`: EF Core, contexto, repositórios e migrations.
- `/docker-compose.api.yaml`: serviço da API.
- `/docker-compose.local.yaml`: suporte local para banco PostgreSQL.

## Requisitos

- .NET SDK 8
- Docker e Docker Compose (opcional para execução em containers)
- PostgreSQL
- Redis
- RabbitMQ
- DynamoDB (local ou AWS)

## Variáveis de ambiente

Use o arquivo `.env.example` como base:

| Variável | Descrição |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Ambiente da aplicação (ex.: `Development`) |
| `ASPNETCORE_HTTP_PORTS` | Porta HTTP da aplicação |
| `DB_CONNECTION_STRING` | Connection string PostgreSQL |
| `PG_USER` / `PG_PASSWORD` | Credenciais do PostgreSQL no Compose local |
| `Jwt__Key` | Chave usada para validação JWT |
| `DynamoDb__LogTableName` | Nome da tabela de logs |
| `DynamoDb__UseLocal` | `true` para DynamoDB local |
| `DynamoDb__LocalUrl` | URL do DynamoDB local |
| `DynamoDb__Region` | Região AWS do DynamoDB |
| `DynamoDb__ProfileName` | Perfil AWS (quando aplicável) |
| `REDIS_CONNECTION` | Endereço de conexão do Redis |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_DEFAULT_REGION` | Credenciais/Região AWS para serviços em nuvem |

## Executando localmente com .NET

1. Configure as variáveis de ambiente.
2. Restaure as dependências:
   ```bash
   dotnet restore
   ```
3. Compile:
   ```bash
   dotnet build
   ```
4. Execute a API:
   ```bash
   dotnet run --project /home/runner/work/campaigns-api/campaigns-api/CampaignsApi/CampaignsApi.csproj
   ```

No ambiente `Development`, as migrations são aplicadas automaticamente na inicialização.

## Executando com Docker

1. Configure um arquivo `.env` na raiz (baseado em `.env.example`).
2. Suba PostgreSQL local:
   ```bash
   docker compose -f /home/runner/work/campaigns-api/campaigns-api/docker-compose.local.yaml up -d campaigns-db
   ```
3. Suba a API:
   ```bash
   docker compose -f /home/runner/work/campaigns-api/campaigns-api/docker-compose.api.yaml up --build -d campaigns-api
   ```

## Endpoints principais

### Campanhas

- `GET /api/campaigns/public` (anônimo): lista campanhas ativas com valor arrecadado.
- `GET /api/campaigns` (Manager): lista todas as campanhas.
- `GET /api/campaigns/{id}` (Manager): busca campanha por ID.
- `POST /api/campaigns` (Manager): cria campanha.
- `PUT /api/campaigns` (Manager): atualiza campanha.
- `DELETE /api/campaigns/{id}` (Manager): remove campanha.
- `GET /api/campaigns/health` (público): health simplificado do controller.

### Doações

- `GET /api/donations` (Manager): lista doações.
- `GET /api/donations/{id}` (Donor): busca doação por ID.
- `POST /api/donations` (Donor): registra nova doação.

## Observabilidade

- Swagger/ReDoc (somente em `Development`):
    - `/swagger`
    - `/redoc` (via configuração ReDoc)
- Health check global: `/health`
- Métricas Prometheus: `/metrics`

## Fluxo de CI

O workflow em `.github/workflows/ci.yml` executa:
- `dotnet restore`
- `dotnet build`
- `dotnet test`
- build da imagem Docker

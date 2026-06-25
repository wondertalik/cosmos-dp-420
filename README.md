# Introduction

Cosmos implementation.

## DEV WORKFLOW

### Run during development

Authentication is required to pull images from Azure Container Registry.
Signin with the corporate account and use an "Opticom Subscription" subscription.

```bash
az login
```

```bash
az acr login -n caropticom
```

We use [docker compose](https://docs.docker.com/compose/) to run dependencies.

From a root directory of project run commands:

- run all development services

```bash
docker compose -f docker-compose.yaml --env-file .env.dev -p cosmos-dp420 up --build --remove-orphans
```

- run all development services with observability tools

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 up --build --remove-orphans
```

- stop and remove all services

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 down
```

- connect to cosmos db shell

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 exec cosmos cosmoshell.sh
```
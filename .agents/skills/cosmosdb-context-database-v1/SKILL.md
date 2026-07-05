---
name: cosmosdb-context-database-v1
description: |
  Schema context for Azure Cosmos DB database-v1 in this repo's DP-420 emulator environment: a normalized schema with 9 containers (customer, customerAddress, customerPassword, product, productCategory, productTag, productTags, salesOrder, salesOrderDetail) totaling ~210,827 documents. Every container partitions on /id. Part of the same skill family as cosmosdb-sdk, cosmosdb-data-and-queries, cosmosdb-best-practices, cosmosdb-operations, and cosmosdb-ai-and-search.
  USE FOR: querying, modeling, or seeding against database-v1 specifically; knowing its container list, partition keys, and foreign-key relationships (customerAddress.customerId, customerPassword.id, product.categoryId, the productTags junction container, salesOrder.customerId, salesOrderDetail.salesOrderId).
  DO NOT USE FOR: database-v2/v3/v4 (different container sets and partition keys); general SDK client setup (cosmosdb-sdk); generic modeling/indexing/query technique (cosmosdb-data-and-queries); throughput/global distribution/monitoring/security (cosmosdb-operations); vector/full-text search and AI agents (cosmosdb-ai-and-search).
license: MIT
metadata:
  author: repo-local
  version: "1.0.0"
---

# Cosmos DB schema context: database-v1

## Database-level throughput

database-v1 has no shared throughput provisioned at the database level — there is no database-wide RU/s ceiling that containers draw from. Whether a database has shared throughput enabled is fixed at creation time and cannot be retrofitted afterward; a database created without it (as database-v1 was) can never gain a database-level RU/s ceiling later, and the reverse is also true. See @ai-context/cosmosdb-context-database-v1.md for how this was confirmed live against the running emulator.

## Containers

| Container | Partition key |
|---|---|
| customer | /id |
| customerAddress | /id |
| customerPassword | /id |
| product | /id |
| productCategory | /id |
| productTag | /id |
| productTags | /id |
| salesOrder | /id |
| salesOrderDetail | /id |

No container in database-v1 has a unique-key constraint or its own dedicated (container-level) throughput — every container relies on the default indexing policy and whatever throughput mode applies at the database/account level.

## When you need more than this

For per-container field shapes, exact document counts, indexing policy defaults, naming gotchas (e.g. `productTag` vs `productTags`), and runnable example queries against the live emulator, see @ai-context/cosmosdb-context-database-v1.md.

## When to Apply

Reference this skill when querying, modeling, or seeding data specifically against **database-v1** in this repo's Cosmos DB emulator environment. Do not use it for database-v2, database-v3, or database-v4 — those have different container sets, partition keys, and schema shapes.

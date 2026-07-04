# Cosmos DB deep-dive: database-v1

database-v1 is the **normalized** schema variant in this repo's four-database DP-420 lineup: 9 containers, one per relational entity, joined by id-style foreign keys (no embedding). Contrast with database-v2/v3 (hybrid schema, 5 containers, partitioned by `categoryId`/`customerId`/`type` instead of `/id`) and database-v4 (fully denormalized, 3 containers, with `salesOrders` embedded directly inside each customer document). database-v1 is the only version where every relationship is a genuine cross-container reference rather than an embedded array or a partition-key-driven co-location.

## Structure and configuration

Field shapes below are sourced from the seed data at `mslearn-cosmosdb-modules-central-main/data/fullset/database-v1/*.json`, cross-checked against the `mkitem` sample payloads in `cosmos-init/database-v1/02-*.csh` through `10-*.csh`. No container in database-v1 embeds an array — that's the defining contrast with database-v4.

| Container | Fields | Documents |
|---|---|---|
| customer | id, title, firstName, lastName, emailAddress, phoneNumber, creationDate | 19,119 |
| customerAddress | id, customerId, addressLine1, addressLine2, city, state, country, zipCode | 18,508 |
| customerPassword | id, hash, salt | 19,119 |
| product | id, categoryId, sku, name, description, price | 295 |
| productCategory | id, name | 37 |
| productTag | id, name | 200 |
| productTags | id, productId, productTagId | 767 |
| salesOrder | id, customerId, orderDate, shipDate | 31,465 |
| salesOrderDetail | id, salesOrderId, sku, name, price, quantity | 121,317 |

Total: **210,827 documents**, matching the `database-v1` row in `README.md`'s "Data volumes per database" table.

### Database-level config

Throughput mode is **`none`** at the database-v1 scope — confirmed live via the cosmosdb MCP shell: `throughput show --database database-v1` returned `{"mode":"none","throughput":null,"autoscaleMaxThroughput":null,"minThroughput":null}`. This is not specific to database-v1: the same account-wide `info` call reports `databaseCount: 6` on a single emulator account, and re-running `throughput show --database database-v4` returns the identical `mode: none` shape — so no database in this emulator has a shared throughput ceiling.

As of this writing, a live `info --database database-v1` call also returned `documentCount: 0` — the 9 containers exist in the running emulator, but no data has been loaded into them in this session. The counts and field shapes in the table above come from the seed JSON source files, not a live scan.

### Per-container config

| Property | Value | Immutable or changeable | Source |
|---|---|---|---|
| Partition key | `/id` on all 9 containers | Immutable after creation | `cosmos-init/database-v1/01-init-database-v1.csh` (`mkcon <name> /id --database=database-v1` on every line); spot-checked live via `info --container customer`, `info --container salesOrderDetail`, `info --container productTags` — all three returned `"partitionKey":["/id"]` |
| Unique keys | None configured on any container | Changeable only by recreating the container (unique-key policy can only be set at creation) | No `--unique-key` flag appears on any `mkcon` line in `01-init-database-v1.csh` |
| Dedicated (container-level) throughput | None — every spot-checked container reports `mode: none` | Changeable at any time (RU/s, unlike partition key or unique keys, is not locked in) | Live `throughput show --database database-v1 --container customer` |
| Indexing policy | Default shape on every spot-checked container: consistent, automatic, 1 included path, 1 excluded path, 0 composite/spatial/vector indexes | Changeable at any time | Live `info --container customer`, `info --container salesOrderDetail`, `info --container productTags` — identical `indexingPolicy` shape on all three |

## Gotchas

- **`productTag` vs `productTags`**: two different containers. `productTag` (singular) holds tag definitions — `id`, `name` (200 docs). `productTags` (plural) is the many-to-many junction between `product` and `productTag` — `id`, `productId`, `productTagId` (767 docs). Easy to typo one for the other.
- **`productTags.id` is a composite key** formed by concatenating `productId + "." + productTagId` (e.g. `027D0B9A-F9D9-4C96-8213-C8546C4AAE71.0573D684-9140-4DEE-89AF-4E4A90E65666`) — it is not an independently generated GUID.
- **`customerPassword.id` equals the owning `customer.id`** (a shared primary key / implicit 1:1 relationship) rather than having its own generated id or a separate `customerId` foreign-key field.
- **Every relationship is cross-partition.** All 9 containers partition on `/id`, so `customerAddress.customerId → customer.id`, `product.categoryId → productCategory.id`, `productTags.productId/productTagId → product.id/productTag.id`, `salesOrder.customerId → customer.id`, and `salesOrderDetail.salesOrderId → salesOrder.id` are all cross-partition lookups with no co-location. This differs from database-v2/v3 (partitioned by `categoryId`/`customerId`/`type`, so related documents can land in the same logical partition) and database-v4 (denormalized — no separate lookup needed at all).

## Example MCP-shell queries

Run these against the live cosmosdb MCP connection (note: they'll return no rows until database-v1 is seeded in this emulator session — see the live document-count caveat above).

```
query "SELECT * FROM c WHERE c.id = '0012D555-C7DE-4C4B-B4A4-2E8A6B8E1161'" --db database-v1 --con customer
```
Point read of a single customer by id — the cheapest possible query shape given the `/id` partition key.

```
query "SELECT VALUE COUNT(1) FROM c" --db database-v1 --con salesOrderDetail
```
Total document count in `salesOrderDetail` once seeded.

```
query "SELECT c.id, c.name, c.price FROM c WHERE c.categoryId = 'AE48F0AA-4F65-4734-A4CF-D48B8F82267F'" --db database-v1 --con product
```
All products in one category — necessarily cross-partition, since `product` partitions on `/id`, not `/categoryId`.

```
query "SELECT VALUE t.productTagId FROM t WHERE t.productId = '027D0B9A-F9D9-4C96-8213-C8546C4AAE71'" --db database-v1 --con productTags
```
Tag ids attached to one product via the `productTags` junction container.

# Introduction to Change Feed

1. Create a new product in the `products` container with the following JSON document:

```
{
    "category": {
        "name": "category-name-1"
    },
    "name": "product-1",
    "categoryId": "category-1"
}
```

Then in `products` container you will see the new product document created.

```json
{
    "category": {
        "name": "category-name-1"
    },
    "name": "product-1",
    "categoryId": "category-1",
    "id": "4c2fb0a0-4977-47e2-834f-859b397b05a0",
    "_rid": "O7RwAKylJbYCAAAAAAAAAA==",
    "_self": "dbs/O7RwAA==/colls/O7RwAKylJbY=/docs/O7RwAKylJbYCAAAAAAAAAA==/",
    "_etag": "\"ba007dcd-0000-0800-0000-6a410b2f0000\"",
    "_attachments": "attachments/",
    "_ts": 1782647599
}
```

In the `lease` container you will see a few lease document created for the `products` container.

```json
[
  {
    "id": "my-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=..0",
    "partitionKey": "my-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=..0",
    "version": 0,
    "_etag": "\"1c00e6ff-0000-0800-0000-6a4132e00000\"",
    "LeaseToken": "0",
    "FeedRange": {
      "Range": {
        "min": "",
        "max": "FF"
      }
    },
    "Owner": "ce68680e-76c0-437c-983a-45cd39474490",
    "ContinuationToken": "\"24\"",
    "properties": {},
    "timestamp": "2026-06-28T14:42:40.403196Z",
    "Mode": "Incremental Feed",
    "_rid": "O7RwAIUOojIYAAAAAAAAAA==",
    "_self": "dbs/O7RwAA==/colls/O7RwAIUOojI=/docs/O7RwAIUOojIYAAAAAAAAAA==/",
    "_attachments": "attachments/",
    "_ts": 1782657760
  },
  {
    "id": "my-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=.info",
    "partitionKey": "my-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=.info",
    "_rid": "O7RwAIUOojIZAAAAAAAAAA==",
    "_self": "dbs/O7RwAA==/colls/O7RwAIUOojI=/docs/O7RwAIUOojIZAAAAAAAAAA==/",
    "_etag": "\"1c00e2ff-0000-0800-0000-6a4132c50000\"",
    "_attachments": "attachments/",
    "_ts": 1782657733
  }
]
```

The console app outputs the following message to the console:

```
[2026-06-28T14:42:14.107Z] Host lock lease acquired by instance ID '0000000000000000000000004164DDA9'.
[2026-06-28T14:42:19.822Z] Executing 'Functions.ItemsListenerCosmosDBTrigger' (Reason='New changes on container products at 2026-06-28T14:42:19.7883720Z', Id=2e5eea02-7d51-4a63-8d12-bec083f437d0)
[2026-06-28T14:42:19.889Z] info: DP420_13_ChangeFeed_AzureFunction.Functions.ItemsListenerCosmosDbTrigger[0]
[2026-06-28T14:42:19.889Z]       ItemsListenerCosmosDBTrigger invoked. InvocationId: 2e5eea02-7d51-4a63-8d12-bec083f437d0. Changes received: 1
[2026-06-28T14:42:19.891Z] info: DP420_13_ChangeFeed_AzureFunction.Functions.ItemsListenerCosmosDbTrigger[0]
[2026-06-28T14:42:19.891Z]       Detected Operation:
[2026-06-28T14:42:19.891Z]       id: [091b24c1-bc0a-4a80-a272-bed318d4bcad]
[2026-06-28T14:42:19.891Z]       name: product-1
[2026-06-28T14:42:19.891Z]       category name: category-name-1
[2026-06-28T14:42:19.891Z]       categoryId: category-1
[2026-06-28T14:42:19.914Z] Executed 'Functions.ItemsListenerCosmosDBTrigger' (Succeeded, Id=2e5eea02-7d51-4a63-8d12-bec083f437d0, Duration=123ms)
```




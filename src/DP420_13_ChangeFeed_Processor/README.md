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
    "id": "productsProcessormy-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=..0",
    "partitionKey": "productsProcessormy-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=..0",
    "version": 0,
    "_etag": "\"0200161e-0000-0800-0000-6a410bfe0000\"",
    "LeaseToken": "0",
    "FeedRange": {
      "Range": {
        "min": "",
        "max": "FF"
      }
    },
    "Owner": "consoleApp",
    "ContinuationToken": null,
    "properties": {},
    "timestamp": "2026-06-28T11:56:46.335876Z",
    "Mode": "Incremental Feed",
    "_rid": "O7RwAIUOojIFAAAAAAAAAA==",
    "_self": "dbs/O7RwAA==/colls/O7RwAIUOojI=/docs/O7RwAIUOojIFAAAAAAAAAA==/",
    "_attachments": "attachments/",
    "_ts": 1782647806
  },
  {
    "id": "productsProcessormy-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=.info",
    "partitionKey": "productsProcessormy-cosmos-tst.documents.azure.com_O7RwAA==_O7RwAKylJbY=.info",
    "_rid": "O7RwAIUOojIGAAAAAAAAAA==",
    "_self": "dbs/O7RwAA==/colls/O7RwAIUOojI=/docs/O7RwAIUOojIGAAAAAAAAAA==/",
    "_etag": "\"0200001e-0000-0800-0000-6a410a090000\"",
    "_attachments": "attachments/",
    "_ts": 1782647305
  }
]
```

The console app outputs the following message to the console:

```
RUN     Listening for changes...
Press any key to stop
START   Handling batch of changes...
Detected Operation:
id: [12ff3d42-25c0-4ff9-a22a-24aa1f07e45b]
name: product-1
category name: category-name-1
categoryId: category-1
```




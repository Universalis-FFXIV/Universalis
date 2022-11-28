using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess;

internal class DynamoDBTableInitializer
{
    internal static async Task InitializeTables(IAmazonDynamoDB dynamoDb)
    {
        Console.WriteLine("Initializing DynamoDB tables...");
        await Task.WhenAll(
            CreateMarketItemTable(dynamoDb),
            CreateSaleTable(dynamoDb),
            CreateApiKeyTable(dynamoDb),
            CreateCharacterTable(dynamoDb),
            CreateFlaggedUploaderTable(dynamoDb));
        Console.WriteLine("DynamoDB tables initialized.");
    }

    private static async Task CreateMarketItemTable(IAmazonDynamoDB dynamoDb)
    {
        try
        {
            var createMarketItemTable = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("item_id", ScalarAttributeType.N),
                    new AttributeDefinition("world_id", ScalarAttributeType.N),
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                TableName = "market_item",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("item_id", KeyType.HASH),
                    new KeySchemaElement("world_id", KeyType.RANGE),
                },
            };

            await dynamoDb.CreateTableAsync(createMarketItemTable);
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine("Failed to create market item table; it may already exist. Exception: {0}", e.Message);
        }
    }

    private static async Task CreateSaleTable(IAmazonDynamoDB dynamoDb)
    {
        try
        {
            var createSaleTable = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("id", ScalarAttributeType.S),
                    new AttributeDefinition("sale_time", ScalarAttributeType.N),
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                TableName = "sale",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("id", KeyType.HASH),
                    new KeySchemaElement("sale_time", KeyType.RANGE),
                },
            };

            await dynamoDb.CreateTableAsync(createSaleTable);
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine("Failed to create sale table; it may already exist. Exception: {0}", e.Message);
        }
    }

    private static async Task CreateApiKeyTable(IAmazonDynamoDB dynamoDb)
    {
        try
        {
            var createCharacterTable = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("token_sha512", ScalarAttributeType.S),
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                TableName = "api_key",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("token_sha512", KeyType.HASH),
                },
            };

            await dynamoDb.CreateTableAsync(createCharacterTable);
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine("Failed to create API key table; it may already exist. Exception: {0}", e.Message);
        }
    }

    private static async Task CreateCharacterTable(IAmazonDynamoDB dynamoDb)
    {
        try
        {
            var createCharacterTable = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("content_id_sha256", ScalarAttributeType.S),
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                TableName = "character",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("content_id_sha256", KeyType.HASH),
                },
            };

            await dynamoDb.CreateTableAsync(createCharacterTable);
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine("Failed to create character table; it may already exist. Exception: {0}", e.Message);
        }
    }

    private static async Task CreateFlaggedUploaderTable(IAmazonDynamoDB dynamoDb)
    {
        try
        {
            var createFlaggedUploaderTable = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("id_sha256", ScalarAttributeType.S),
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                TableName = "flagged_uploader",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("id_sha256", KeyType.HASH),
                },
            };

            await dynamoDb.CreateTableAsync(createFlaggedUploaderTable);
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine("Failed to create flagged uploader table; it may already exist. Exception: {0}", e.Message);
        }
    }
}

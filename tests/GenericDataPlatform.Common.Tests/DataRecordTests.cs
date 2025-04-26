using System;
using System.Collections.Generic;
using GenericDataPlatform.Common.Models;
using Xunit;

namespace GenericDataPlatform.Common.Tests
{
    public class DataRecordTests
    {
        [Fact]
        public void GetValue_ReturnsCorrectValue_WhenKeyExists()
        {
            // Arrange
            var record = new DataRecord
            {
                Id = "test-id",
                SchemaId = "schema-id",
                SourceId = "source-id",
                Data = new Dictionary<string, object>
                {
                    { "stringValue", "test" },
                    { "intValue", 42 },
                    { "boolValue", true },
                    { "dateValue", DateTime.Parse("2023-01-01") }
                }
            };

            // Act
            var stringValue = record.GetValue<string>("stringValue");
            var intValue = record.GetValue<int>("intValue");
            var boolValue = record.GetValue<bool>("boolValue");
            var dateValue = record.GetValue<DateTime>("dateValue");

            // Assert
            Assert.Equal("test", stringValue);
            Assert.Equal(42, intValue);
            Assert.True(boolValue);
            Assert.Equal(DateTime.Parse("2023-01-01"), dateValue);
        }

        [Fact]
        public void GetValue_ReturnsDefaultValue_WhenKeyDoesNotExist()
        {
            // Arrange
            var record = new DataRecord
            {
                Id = "test-id",
                Data = new Dictionary<string, object>()
            };

            // Act
            var stringValue = record.GetValue<string>("nonExistentKey", "default");
            var intValue = record.GetValue<int>("nonExistentKey", 100);

            // Assert
            Assert.Equal("default", stringValue);
            Assert.Equal(100, intValue);
        }

        [Fact]
        public void TryGetValue_ReturnsTrueAndValue_WhenKeyExists()
        {
            // Arrange
            var record = new DataRecord
            {
                Id = "test-id",
                Data = new Dictionary<string, object>
                {
                    { "stringValue", "test" },
                    { "intValue", 42 }
                }
            };

            // Act
            bool stringResult = record.TryGetValue<string>("stringValue", out var stringValue);
            bool intResult = record.TryGetValue<int>("intValue", out var intValue);

            // Assert
            Assert.True(stringResult);
            Assert.Equal("test", stringValue);
            Assert.True(intResult);
            Assert.Equal(42, intValue);
        }

        [Fact]
        public void TryGetValue_ReturnsFalseAndDefault_WhenKeyDoesNotExist()
        {
            // Arrange
            var record = new DataRecord
            {
                Id = "test-id",
                Data = new Dictionary<string, object>()
            };

            // Act
            bool result = record.TryGetValue<string>("nonExistentKey", out var value);

            // Assert
            Assert.False(result);
            Assert.Equal(default, value);
        }
    }
}

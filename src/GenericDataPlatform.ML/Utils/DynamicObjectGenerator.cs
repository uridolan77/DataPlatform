using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GenericDataPlatform.ML.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace GenericDataPlatform.ML.Utils
{
    /// <summary>
    /// Utility for generating dynamic objects from schema definitions
    /// </summary>
    public interface IDynamicObjectGenerator
    {
        /// <summary>
        /// Creates a dynamic type from a schema
        /// </summary>
        /// <param name="inputSchema">Input schema</param>
        /// <param name="outputSchema">Output schema</param>
        /// <returns>Dynamic type definition</returns>
        Type CreateType(List<FeatureDefinition> inputSchema, List<LabelDefinition> outputSchema = null);
        
        /// <summary>
        /// Creates an object from a dictionary and schema
        /// </summary>
        /// <param name="data">Data to populate the object with</param>
        /// <param name="schema">Schema definition</param>
        /// <returns>Dynamic object instance</returns>
        object CreateObject(Dictionary<string, object> data, List<FeatureDefinition> schema);
        
        /// <summary>
        /// Converts an object to a dictionary
        /// </summary>
        /// <param name="obj">Object to convert</param>
        /// <returns>Dictionary representation</returns>
        Dictionary<string, object> ConvertToDictionary(object obj);
        
        /// <summary>
        /// Creates a prediction engine for a model
        /// </summary>
        /// <param name="mlContext">ML context</param>
        /// <param name="model">Trained model</param>
        /// <param name="inputSchema">Input schema</param>
        /// <param name="outputSchema">Output schema</param>
        /// <returns>Prediction engine</returns>
        dynamic CreatePredictionEngine(MLContext mlContext, ITransformer model, List<FeatureDefinition> inputSchema, List<LabelDefinition> outputSchema);
    }
    
    /// <summary>
    /// Implementation of the dynamic object generator
    /// </summary>
    public class DynamicObjectGenerator : IDynamicObjectGenerator
    {
        private readonly ILogger<DynamicObjectGenerator> _logger;
        private readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        
        public DynamicObjectGenerator(ILogger<DynamicObjectGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Creates a dynamic type from a schema
        /// </summary>
        public Type CreateType(List<FeatureDefinition> inputSchema, List<LabelDefinition> outputSchema = null)
        {
            // Create a unique key for the schema
            var schemaKey = GenerateSchemaKey(inputSchema, outputSchema);
            
            // Check cache
            if (_typeCache.TryGetValue(schemaKey, out var cachedType))
            {
                return cachedType;
            }
            
            try
            {
                // Create a dynamic assembly and module
                var assemblyName = new AssemblyName($"DynamicModels.{Guid.NewGuid()}");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
                
                // Create a type builder for the dynamic type
                var typeBuilder = moduleBuilder.DefineType(
                    $"DynamicType_{Guid.NewGuid()}",
                    TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
                    typeof(object));
                
                // Add properties from input schema
                if (inputSchema != null)
                {
                    foreach (var field in inputSchema)
                    {
                        AddProperty(typeBuilder, field.Name, MapToClrType(field.DataType), isInput: true);
                    }
                }
                
                // Add properties from output schema
                if (outputSchema != null)
                {
                    foreach (var field in outputSchema)
                    {
                        AddProperty(typeBuilder, field.Name, MapToClrType(field.DataType), isInput: false);
                    }
                }
                
                // Create the type
                var type = typeBuilder.CreateType();
                
                // Cache the type
                _typeCache[schemaKey] = type;
                
                _logger.LogInformation("Created dynamic type with {InputFieldCount} input fields and {OutputFieldCount} output fields", 
                    inputSchema?.Count ?? 0, outputSchema?.Count ?? 0);
                
                return type;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dynamic type");
                throw;
            }
        }
        
        /// <summary>
        /// Creates an object from a dictionary and schema
        /// </summary>
        public object CreateObject(Dictionary<string, object> data, List<FeatureDefinition> schema)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            
            try
            {
                // Create dynamic type
                var type = CreateType(schema);
                
                // Create instance
                var obj = Activator.CreateInstance(type);
                
                // Set properties
                foreach (var field in schema)
                {
                    var propertyInfo = type.GetProperty(field.Name);
                    if (propertyInfo != null)
                    {
                        // Check if data contains the field
                        if (data.TryGetValue(field.Name, out var value))
                        {
                            // Convert value to the property type
                            var convertedValue = ConvertValue(value, propertyInfo.PropertyType, field);
                            
                            // Set the property value
                            propertyInfo.SetValue(obj, convertedValue);
                        }
                        else if (field.IsRequired)
                        {
                            // For required fields, use default value if specified
                            if (!string.IsNullOrEmpty(field.DefaultValue))
                            {
                                var defaultValue = ConvertValue(field.DefaultValue, propertyInfo.PropertyType, field);
                                propertyInfo.SetValue(obj, defaultValue);
                            }
                            else
                            {
                                throw new ArgumentException($"Required field {field.Name} is missing and has no default value");
                            }
                        }
                    }
                }
                
                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating object from dictionary");
                throw;
            }
        }
        
        /// <summary>
        /// Converts an object to a dictionary
        /// </summary>
        public Dictionary<string, object> ConvertToDictionary(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            
            try
            {
                var result = new Dictionary<string, object>();
                
                // Get all properties
                var properties = obj.GetType().GetProperties();
                
                // Add each property to the dictionary
                foreach (var property in properties)
                {
                    result[property.Name] = property.GetValue(obj);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting object to dictionary");
                throw;
            }
        }
        
        /// <summary>
        /// Creates a prediction engine for a model
        /// </summary>
        public dynamic CreatePredictionEngine(MLContext mlContext, ITransformer model, List<FeatureDefinition> inputSchema, List<LabelDefinition> outputSchema)
        {
            try
            {
                // Create dynamic types
                var inputType = CreateType(inputSchema);
                var outputType = CreateType(null, outputSchema);
                
                // Get the generic method
                var createPredictionEngineMethod = mlContext.Model.GetType()
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "CreatePredictionEngine" && m.GetGenericArguments().Length == 2);
                
                if (createPredictionEngineMethod == null)
                {
                    throw new InvalidOperationException("CreatePredictionEngine method not found");
                }
                
                // Make the generic method concrete with our types
                var genericMethod = createPredictionEngineMethod.MakeGenericMethod(inputType, outputType);
                
                // Call the method to create the prediction engine
                var predictionEngine = genericMethod.Invoke(mlContext.Model, new object[] { model, null });
                
                _logger.LogInformation("Created prediction engine for model");
                
                return predictionEngine;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prediction engine");
                throw;
            }
        }
        
        #region Private Methods
        
        /// <summary>
        /// Adds a property to a type builder
        /// </summary>
        private void AddProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType, bool isInput)
        {
            // Create backing field
            var fieldBuilder = typeBuilder.DefineField(
                $"_{propertyName}",
                propertyType,
                FieldAttributes.Private);
            
            // Create property
            var propertyBuilder = typeBuilder.DefineProperty(
                propertyName,
                PropertyAttributes.HasDefault,
                propertyType,
                null);
            
            // Create getter method
            var getterMethodBuilder = typeBuilder.DefineMethod(
                $"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes);
            
            var getterIL = getterMethodBuilder.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);
            
            // Create setter method
            var setterMethodBuilder = typeBuilder.DefineMethod(
                $"set_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new[] { propertyType });
            
            var setterIL = setterMethodBuilder.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, fieldBuilder);
            setterIL.Emit(OpCodes.Ret);
            
            // Assign getter and setter methods to the property
            propertyBuilder.SetGetMethod(getterMethodBuilder);
            propertyBuilder.SetSetMethod(setterMethodBuilder);
            
            // Add ML.NET column attribute
            var columnAttrType = typeof(ColumnNameAttribute);
            var columnAttrCtor = columnAttrType.GetConstructor(new[] { typeof(string) });
            
            if (columnAttrCtor != null)
            {
                var attrBuilder = new CustomAttributeBuilder(
                    columnAttrCtor,
                    new object[] { propertyName });
                
                propertyBuilder.SetCustomAttribute(attrBuilder);
            }
            
            // Add ML.NET load/score column attributes
            if (isInput)
            {
                var loadAttrType = typeof(LoadColumnAttribute);
                var loadAttrCtor = loadAttrType.GetConstructor(new[] { typeof(string) });
                
                if (loadAttrCtor != null)
                {
                    var attrBuilder = new CustomAttributeBuilder(
                        loadAttrCtor,
                        new object[] { propertyName });
                    
                    propertyBuilder.SetCustomAttribute(attrBuilder);
                }
            }
            else
            {
                var scoreAttrType = typeof(ScoreColumnAttribute);
                var scoreAttrCtor = scoreAttrType.GetConstructor(Type.EmptyTypes);
                
                if (scoreAttrCtor != null)
                {
                    var attrBuilder = new CustomAttributeBuilder(
                        scoreAttrCtor,
                        Array.Empty<object>());
                    
                    propertyBuilder.SetCustomAttribute(attrBuilder);
                }
            }
        }
        
        /// <summary>
        /// Maps feature data type to CLR type
        /// </summary>
        private Type MapToClrType(FeatureDataType dataType)
        {
            switch (dataType)
            {
                case FeatureDataType.String:
                case FeatureDataType.Categorical:
                case FeatureDataType.Text:
                    return typeof(string);
                
                case FeatureDataType.Integer:
                    return typeof(int);
                
                case FeatureDataType.Float:
                    return typeof(float);
                
                case FeatureDataType.Boolean:
                    return typeof(bool);
                
                case FeatureDataType.DateTime:
                    return typeof(DateTime);
                
                case FeatureDataType.Image:
                    return typeof(byte[]);
                
                default:
                    return typeof(string);
            }
        }
        
        /// <summary>
        /// Maps label data type to CLR type
        /// </summary>
        private Type MapToClrType(LabelDataType dataType)
        {
            switch (dataType)
            {
                case LabelDataType.Binary:
                    return typeof(bool);
                
                case LabelDataType.Categorical:
                    return typeof(string);
                
                case LabelDataType.Continuous:
                    return typeof(float);
                
                default:
                    return typeof(string);
            }
        }
        
        /// <summary>
        /// Converts a value to the target type
        /// </summary>
        private object ConvertValue(object value, Type targetType, FeatureDefinition field = null)
        {
            if (value == null)
            {
                return GetDefaultValue(targetType);
            }
            
            try
            {
                if (targetType == typeof(string))
                {
                    return value.ToString();
                }
                else if (targetType == typeof(int))
                {
                    if (value is int intValue)
                    {
                        return intValue;
                    }
                    return Convert.ToInt32(value);
                }
                else if (targetType == typeof(float))
                {
                    if (value is float floatValue)
                    {
                        return floatValue;
                    }
                    return Convert.ToSingle(value);
                }
                else if (targetType == typeof(bool))
                {
                    if (value is bool boolValue)
                    {
                        return boolValue;
                    }
                    return Convert.ToBoolean(value);
                }
                else if (targetType == typeof(DateTime))
                {
                    if (value is DateTime dateValue)
                    {
                        return dateValue;
                    }
                    return Convert.ToDateTime(value);
                }
                else if (targetType == typeof(byte[]))
                {
                    if (value is byte[] byteValue)
                    {
                        return byteValue;
                    }
                    throw new InvalidCastException($"Cannot convert {value.GetType()} to byte[]");
                }
                else
                {
                    return Convert.ChangeType(value, targetType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting value {Value} to type {TargetType}", value, targetType);
                
                // If conversion fails, return default value
                return GetDefaultValue(targetType);
            }
        }
        
        /// <summary>
        /// Gets the default value for a type
        /// </summary>
        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            
            return null;
        }
        
        /// <summary>
        /// Generates a unique key for a schema
        /// </summary>
        private string GenerateSchemaKey(List<FeatureDefinition> inputSchema, List<LabelDefinition> outputSchema)
        {
            var key = "";
            
            if (inputSchema != null)
            {
                foreach (var field in inputSchema)
                {
                    key += $"{field.Name}:{field.DataType};";
                }
            }
            
            key += "|";
            
            if (outputSchema != null)
            {
                foreach (var field in outputSchema)
                {
                    key += $"{field.Name}:{field.DataType};";
                }
            }
            
            return key;
        }
        
        #endregion
    }
}
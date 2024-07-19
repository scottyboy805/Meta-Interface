using System;
using System.Collections.Generic;
using System.Reflection;

namespace UltimateReplay.Storage
{
    public struct ReplayToken
    {
        // Private
        private string identifier;
        private FieldInfo field;
        private PropertyInfo property;
        private bool isOptional;

        // Public
        public static readonly ReplayToken invalid = new ReplayToken();

        // Properties
        public bool IsValid
        {
            get { return identifier != null && (field != null || property != null); }
        }

        public string Identifier
        {
            get { return identifier; }
        }

        public bool IsOptional
        {
            get { return isOptional; }
        }

        public Type ValueType
        {
            get
            {
                // Get field type
                if (field != null)
                    return field.FieldType;

                // Get property type
                if(property != null)
                    return property.PropertyType;

                return null;
            }
        }

        internal FieldInfo Field
        {
            get { return field; }
        }

        internal PropertyInfo Property
        {
            get { return property; }
        }

        // Constructor
        internal ReplayToken(string identifier, FieldInfo field, bool isOptional)
        {
            this.identifier = identifier;
            this.field = field;
            this.property = null;
            this.isOptional = isOptional;
        }

        internal ReplayToken(string identifier, PropertyInfo property, bool isOptional)
        {
            this.identifier = identifier;
            this.property = property;
            this.field = null;
            this.isOptional = isOptional;
        }

        // Methods
        public object FetchValue(object instance)
        {
            // Check for null instance
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            // Check for field
            if(field != null)
            {
                return field.GetValue(instance);
            }
            // Check for property
            else if(property != null)
            {
                return property.GetValue(instance);
            }

            return null;
        }

        public void StoreValue(object instance, object value)
        {
            // Check for null instance
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            // Check for field
            if(field != null)
            {
                field.SetValue(instance, value);
            }

            // Check for property
            if(property != null)
            {
                property.SetValue(instance, value);
            }
        }

        public static ReplayToken Create(string fieldOrPropertyName, Type declaringType, bool isOptional = false)
        {
            // Check for empty name
            if (string.IsNullOrEmpty(fieldOrPropertyName) == true)
                throw new ArgumentException(nameof(fieldOrPropertyName) + " cannot be null or empty");

            // Check for null type
            if (declaringType == null)
                throw new ArgumentNullException(nameof(declaringType));

            // Try to find field
            FieldInfo field = declaringType.GetField(fieldOrPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            // Check for found
            if (field != null)
                return Create(field, isOptional);

            // Try to find property
            PropertyInfo property = declaringType.GetProperty(fieldOrPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            // Check for found
            if (property != null)
                return Create(property, isOptional);

            // Failed to find valid toke
            return invalid;
        }

        public static ReplayToken Create(FieldInfo field, bool isOptional = false)
        {
            if (field != null)
            {
                // Check for attribute
                ReplayTokenSerializeAttribute attrib = field.GetCustomAttribute<ReplayTokenSerializeAttribute>();

                if (attrib != null)
                {
                    // Create from field with attribute override name
                    return new ReplayToken(attrib.GetSerializeName(field.Name), field, isOptional);
                }

                // Create from field
                return new ReplayToken(field.Name, field, isOptional);
            }
            return invalid;
        }

        public static ReplayToken Create(PropertyInfo property, bool isOptional = false)
        {
            if (property != null)
            {
                // Check for attribute
                ReplayTokenSerializeAttribute attrib = property.GetCustomAttribute<ReplayTokenSerializeAttribute>();

                if (attrib != null)
                {
                    // Create from property with attribute override name
                    return new ReplayToken(attrib.GetSerializeName(property.Name), property, isOptional);
                }

                // Create from property
                return new ReplayToken(property.Name, property, isOptional);
            }
            return invalid;
        }

        public static IEnumerable<ReplayToken> Tokenize(object instance)
        {
            // Check for null
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            // Get tokens from type
            return Tokenize(instance.GetType());
        }

        public static IEnumerable<ReplayToken> Tokenize(Type type)
        {
            List<ReplayToken> tokens = new List<ReplayToken>();

            // Process all fields
            foreach(FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                // Check for attribute
                ReplayTokenSerializeAttribute attrib = field.GetCustomAttribute<ReplayTokenSerializeAttribute>();

                // Check for valid and return token
                if (attrib != null)
                    tokens.Add(new ReplayToken(attrib.GetSerializeName(field.Name), field, attrib.IsOptional));
            }

            // Process all properties
            foreach(PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                // Check for attribute
                ReplayTokenSerializeAttribute attrib = property.GetCustomAttribute<ReplayTokenSerializeAttribute>();

                // Check for valid and return token
                if (attrib != null)
                    tokens.Add(new ReplayToken(attrib.GetSerializeName(property.Name), property, attrib.IsOptional));
            }

            return tokens;
        }

        public static IEnumerable<ReplayToken> Tokenize<T>()
        {
            return Tokenize(typeof(T));
        }
    }
}

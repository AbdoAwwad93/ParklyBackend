using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Parkly_Backend.Swagger;

public class ResponseExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses == null)
        {
            return;
        }

        foreach (var kvp in operation.Responses)
        {
            if (!int.TryParse(kvp.Key, out var statusCode))
            {
                continue;
            }

            if (!kvp.Value.Content.TryGetValue("application/json", out var media))
            {
                continue;
            }

            var isSuccess = statusCode >= 200 && statusCode < 300;

            var responseType = context.ApiDescription.SupportedResponseTypes
                ?.FirstOrDefault(r => r.StatusCode == statusCode)?.Type;

            JsonNode? example;
            if (responseType != null && responseType != typeof(void))
            {
                example = GenerateFromType(responseType, new HashSet<Type>());
            }
            else
            {
                example = new JsonObject
                {
                    ["isSuccess"] = isSuccess,
                    ["message"] = isSuccess ? "Request succeeded" : "Request failed",
                    ["errors"] = isSuccess ? null : ErrorSample()
                };
            }

            NormalizeEnvelope(example, isSuccess, kvp.Value.Description);
            media.Example = example;
        }
    }

    private static JsonNode GenerateFromType(Type type, HashSet<Type> visiting)
    {
        if (type == null)
        {
            return JsonValue.Create("string");
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return GenerateFromType(underlying, visiting);
        }

        if (type == typeof(string) || type == typeof(char) || type == typeof(Uri))
        {
            return JsonValue.Create("string");
        }
        if (type == typeof(Guid))
        {
            return JsonValue.Create(Guid.NewGuid().ToString());
        }
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return JsonValue.Create(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
        if (type == typeof(bool))
        {
            return JsonValue.Create(false);
        }
        if (type == typeof(byte[]))
        {
            return JsonValue.Create("");
        }
        if (IsNumeric(type))
        {
            return JsonValue.Create(0);
        }
        if (IsEnum(type))
        {
            var first = Enum.GetNames(type).FirstOrDefault();
            return JsonValue.Create(first ?? "0");
        }

        if (TryGetElementType(type, out var elementType))
        {
            var arr = new JsonArray();
            if (elementType != null)
            {
                arr.Add(GenerateFromType(elementType, visiting));
            }
            return arr;
        }

        if (!visiting.Add(type))
        {
            return new JsonObject();
        }

        try
        {
            if (type.IsValueType || type.IsClass)
            {
                var obj = new JsonObject();
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }
                    if (prop.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Length > 0)
                    {
                        continue;
                    }

                    var name = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                        .Cast<JsonPropertyNameAttribute>()
                        .FirstOrDefault()?.Name ?? ToCamelCase(prop.Name);

                    obj[name] = GenerateFromType(prop.PropertyType, visiting);
                }
                return obj;
            }
        }
        finally
        {
            visiting.Remove(type);
        }

        return JsonValue.Create("string");
    }

    private static void NormalizeEnvelope(JsonNode? example, bool isSuccess, string? description)
    {
        if (example is not JsonObject obj)
        {
            return;
        }

        var message = !string.IsNullOrWhiteSpace(description)
            ? description
            : (isSuccess ? "Request succeeded" : "Request failed");

        if (obj["isSuccess"] != null)
        {
            obj.Remove("isSuccess");
        }
        obj.Insert(0, "isSuccess", isSuccess);

        if (obj["message"] != null)
        {
            obj.Remove("message");
        }
        obj.Insert(1, "message", message);

        if (obj["errors"] != null)
        {
            obj.Remove("errors");
            obj["errors"] = isSuccess ? null : ErrorSample();
        }

        if (!isSuccess && obj["data"] != null)
        {
            obj["data"] = null;
        }
    }

    private static bool IsNumeric(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    private static bool IsEnum(Type type)
    {
        return type.IsEnum;
    }

    private static bool TryGetElementType(Type type, out Type? elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }
        if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            var arg = type.GetGenericArguments().FirstOrDefault();
            if (arg != null)
            {
                elementType = arg;
                return true;
            }
        }
        elementType = null;
        return false;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static JsonArray ErrorSample()
    {
        var arr = new JsonArray();
        arr.Add("An error occurred. See details for more information.");
        return arr;
    }
}
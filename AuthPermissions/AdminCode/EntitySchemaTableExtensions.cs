// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthPermissions.AdminCode;
/// <summary>
/// These methods come from the library https://github.com/JonPSmith/EfCore.SchemaCompare
/// </summary>
public static class EntitySchemaTableExtensions
{
    /// <summary>
    /// This returns a string in the format "table" or "{schema}.{table}" that this entity is mapped to
    /// This also handles "ToView" entities, in which case it will map the 
    /// It it isn't mapped to a table it returns null
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public static string FormSchemaTableFromModel(this IEntityType entityType)
    {
        var tableOrViewName = !string.IsNullOrEmpty((string)entityType.GetAnnotation(RelationalAnnotationNames.TableName).Value)
            ? RelationalAnnotationNames.TableName
            : RelationalAnnotationNames.ViewName;

        var tableOrViewSchema = !string.IsNullOrEmpty((string)entityType.GetAnnotation(RelationalAnnotationNames.TableName).Value)
            ? RelationalAnnotationNames.Schema
            : RelationalAnnotationNames.ViewSchema;

        var viewAnnotations = entityType.GetAnnotations()
            .Where(a => a.Name == tableOrViewName ||
                        a.Name == tableOrViewSchema)
            .ToArray();

        return viewAnnotations.Any()
            ? FormSchemaTable((string)viewAnnotations.First(a => a.Name == tableOrViewSchema).Value, (string)viewAnnotations.First(a => a.Name == tableOrViewName).Value)
            : entityType.GetTableName() == null
                ? null
                : FormSchemaTable(entityType.GetSchema(), entityType.GetTableName());
    }

    /// <summary>
    /// Use this on Model side, where the schema is null for the default schema
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns></returns>
    public static string FormSchemaTable(this string schema, string table)
    {
        return string.IsNullOrEmpty(schema)
            ? table
            : $"{schema}.{table}";
    }
}
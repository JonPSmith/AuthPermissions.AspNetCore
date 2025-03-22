// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.BaseCode.DataLayer.Classes;

/// <summary>
/// This class holds the information about each database used by the AuthP sharding feature
/// </summary>
public class ShardingEntry : IEquatable<ShardingEntry>
{
    /// <summary>
    /// This holds the name for this database information, which will be seen by admin users and in a claim
    /// This is used as a reference to this <see cref="ShardingEntry"/>
    /// The <see cref="Name"/> should not be null, and should be unique
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MaxLength(AuthDbConstants.TenantFullNameSize)]
    public string Name { get; set; }

    /// <summary>
    /// This contains the name of the database. Can be null or empty string, in which case it will use the database name found in the connection string
    /// NOTE: for some reason the <see cref="DatabaseName"/> is an empty string, when the actual json says it is null
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// This contains the name of the connection string in the appsettings' "ConnectionStrings" part
    /// If not set, then is default value is "DefaultConnection"
    /// </summary>
    public string ConnectionName { get; set; }

    /// <summary>
    /// This defines the short name of the of database provider, e,g. "SqlServer".
    /// </summary>
    public string DatabaseType { get; set; }

    /// <summary>
    /// Useful for debugging
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(DatabaseName)}: {DatabaseName ?? " < null > "}, {nameof(ConnectionName)}: {ConnectionName}, {nameof(DatabaseType)}: {DatabaseType}";
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
    public bool Equals(ShardingEntry other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && DatabaseName == other.DatabaseName && 
               ConnectionName == other.ConnectionName && DatabaseType == other.DatabaseType;
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>
    /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ShardingEntry)obj);
    }

    /// <summary>Serves as the default hash function.</summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, DatabaseName, ConnectionName, DatabaseType);
    }
}
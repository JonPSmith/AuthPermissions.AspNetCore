// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Domain;

/// <summary>
/// This is used on entity classes where the DataKey isn't set by setting the DataKey directly.
/// </summary>
public interface IDataKeyFilterReadOnly
{
    /// <summary>
    /// The DataKey to be used for multi-tenant applications.
    /// </summary>
    string DataKey { get; }
}
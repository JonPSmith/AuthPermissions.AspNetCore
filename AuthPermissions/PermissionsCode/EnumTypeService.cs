// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace AuthPermissions.PermissionsCode
{
    /// <summary>
    /// This has to be registered with the external DI provider as a singleton. It carries the 
    /// </summary>
    public class EnumTypeService
    {
        public EnumTypeService(Type enumPermissionsType)
        {
            if (!enumPermissionsType.IsEnum)
                throw new ArgumentException("Must be an enum");
            if (Enum.GetUnderlyingType(enumPermissionsType) != typeof(short))
                throw new InvalidOperationException(
                    $"The enum permissions {enumPermissionsType.Name} should by 16 bits in size to work.\n" +
                    $"Please add ': short' to your permissions declaration, i.e. public enum {enumPermissionsType.Name} : short " + "{...};");

            EnumPermissionsType = enumPermissionsType;

        }

        public Type EnumPermissionsType { get; }
    }
}
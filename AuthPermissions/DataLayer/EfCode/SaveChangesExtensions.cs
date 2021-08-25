// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StatusGeneric;

namespace AuthPermissions.DataLayer.EfCode
{
    /// <summary>
    /// This contains extension methods which will call SaveChanges / SaveChangesAsync
    /// with code to to detect unique constraint errors
    /// It uses the https://github.com/Giorgi/EntityFramework.Exceptions library as I plan to support SQL Server and PostgreSQL
    /// </summary>
    public static class SaveChangesExtensions
    {
        /// <summary>
        /// This calls SaveChanges, but detects unique constraint and concurrency exception
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Status</returns>
        public static IStatusGeneric SaveChangesWithChecks(this DbContext context)
        {
            try
            {
                context.SaveChanges();
            }
            catch (UniqueConstraintException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.Duplicate);
            }
            catch (DbUpdateConcurrencyException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.ConcurrencyError);
            }

            return new StatusGenericHandler();
        }

        /// <summary>
        /// This calls SaveChangesAsync, but detects unique constraint and concurrency exception
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Status</returns>
        public static async Task<IStatusGeneric> SaveChangesWithChecksAsync(this DbContext context)
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (UniqueConstraintException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.Duplicate);
            }
            catch (DbUpdateConcurrencyException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.ConcurrencyError);
            }

            return new StatusGenericHandler();
        }

        private enum ExceptionTypes {Duplicate, ConcurrencyError}

        private static IStatusGeneric ConvertExceptionToStatus(this IReadOnlyList<EntityEntry> entities, ExceptionTypes exceptionType)
        {
            var status = new StatusGenericHandler();

            //NOTE: These is only one entity in an exception
            if (entities.Any())
            {
                var name = (entities[0].Entity as INameToShowOnException)?.NameToUseForError ?? "<unknown>";
                var typeName = entities[0].Entity.GetType().Name;

                switch (exceptionType)
                {
                    case ExceptionTypes.Duplicate:
                        return status.AddError($"There is already a {typeName} with a value: name = {name}");
                    case ExceptionTypes.ConcurrencyError:
                        return status.AddError($"Another user changed the {typeName} with the name = {name}. Please re-read the entity and add you change again.");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(exceptionType), exceptionType, null);
                }
            }
            else
            {
                //This shouldn't happen, but just in case
                status.AddError($"There was a {exceptionType} on an auth class.");
            }

            return status;
        }
    }
}
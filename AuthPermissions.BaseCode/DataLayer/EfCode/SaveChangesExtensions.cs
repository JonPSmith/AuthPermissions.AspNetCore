// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using EntityFramework.Exceptions.Common;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StatusGeneric;

namespace AuthPermissions.BaseCode.DataLayer.EfCode
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
        /// <param name="localizeDefault"></param>
        /// <returns>Status</returns>
        public static IStatusGeneric SaveChangesWithChecks(this DbContext context,
            IDefaultLocalizer localizeDefault)
        {
            try
            {
                context.SaveChanges();
            }
            catch (UniqueConstraintException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.Duplicate, localizeDefault);
            }
            catch (DbUpdateConcurrencyException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.ConcurrencyError, localizeDefault);
            }

            //This doesn't be changed to StatusGenericLocalizer because this is just sending a valid status
            return new StatusGenericHandler(); 
        }

        /// <summary>
        /// This calls SaveChangesAsync, but detects unique constraint and concurrency exception
        /// </summary>
        /// <param name="context"></param>
        /// <param name="localizeDefault"></param>
        /// <returns>Status</returns>
        public static async Task<IStatusGeneric> SaveChangesWithChecksAsync(this DbContext context,
            IDefaultLocalizer localizeDefault)
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (UniqueConstraintException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.Duplicate, localizeDefault);
            }
            catch (DbUpdateConcurrencyException e)
            {
                return ConvertExceptionToStatus(e.Entries, ExceptionTypes.ConcurrencyError, localizeDefault);
            }

            //This doesn't be changed to StatusGenericLocalizer because this is just sending a valid status
            return new StatusGenericHandler();
        }

        private enum ExceptionTypes {Duplicate, ConcurrencyError}

        private static IStatusGeneric ConvertExceptionToStatus(this IReadOnlyList<EntityEntry> entities,
            ExceptionTypes exceptionType, IDefaultLocalizer localizeDefault)
        {
            var status = new StatusGenericLocalizer(localizeDefault);

            //NOTE: These is only one entity in an exception
            if (entities.Any())
            {
                var name = (entities[0].Entity as INameToShowOnException)?.NameToUseForError ?? "<unknown>";
                var typeName = entities[0].Entity.GetType().Name;

                return exceptionType switch
                {
                    ExceptionTypes.Duplicate => status.AddErrorFormatted(
                        "DuplicateDb".StaticClassLocalizeKey(typeof(SaveChangesExtensions), true),
                        $"There is already a {typeName} with a value: name = {name}"),
                    ExceptionTypes.ConcurrencyError => status.AddErrorFormatted(
                        "ConcurrencyError".StaticClassLocalizeKey(typeof(SaveChangesExtensions), true),
                        $"Another user changed the {typeName} with the name = {name}. Please re-read the entity and add you change again."),
                    _ => throw new ArgumentOutOfRangeException(nameof(exceptionType), exceptionType, null)
                };
            }

            //This shouldn't happen, but just in case
            status.AddErrorFormatted(
                "UnknownException".StaticClassLocalizeKey(typeof(SaveChangesExtensions), true), 
                $"There was a {exceptionType} on an auth class.");

            return status;
        }
    }
}
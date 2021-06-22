// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
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
        public static IStatusGeneric SaveChangesWithUniqueCheck(this DbContext context)
        {
            try
            {
                context.SaveChanges();
            }
            catch (UniqueConstraintException e)
            {
                return ConvertExceptionToStatus(e);
            }

            return new StatusGenericHandler();
        }

        public static async Task<IStatusGeneric> SaveChangesWithUniqueCheckAsync(this DbContext context)
        {

            try
            {
                await context.SaveChangesAsync();
            }
            catch (UniqueConstraintException e)
            {
                return ConvertExceptionToStatus(e);
            }

            return new StatusGenericHandler();
        }

        private static IStatusGeneric ConvertExceptionToStatus(this UniqueConstraintException e)
        {
            var status = new StatusGenericHandler();

            //NOTE: At this time the e.Entries only has one error
            if (e.Entries.Any())
            {
                var name = (e.Entries.First().Entity as INameToShowOnException)?.NameToUseForError ?? "<unknown>";

                status.AddError($"There is already a {e.Entries.First().Entity.GetType().Name} with a value: name = {name}");
            }
            else
            {
                status.AddError("There was a duplicate of an auth class.");
            }

            return status;
        }
    }
}
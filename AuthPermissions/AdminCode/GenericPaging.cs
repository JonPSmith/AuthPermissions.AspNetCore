// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This is a generic paging code
    /// </summary>
    public static class GenericPaging
    {
        /// <summary>
        /// This will add LINQ Skip and Take methods to provide a paging approach
        /// </summary>
        /// <param name="query">an IQueryable query of the type you want to show</param>
        /// <param name="pageNumZeroStart">Page number, starting at zero</param>
        /// <param name="pageSize">The number of entities you want to show</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static IQueryable<T> Page<T>(this IQueryable<T> query, int pageNumZeroStart, int pageSize)
        {
            if (pageSize == 0)
                throw new ArgumentOutOfRangeException
                    (nameof(pageSize), "pageSize cannot be zero.");

            if (pageNumZeroStart != 0)
                query = query
                    .Skip(pageNumZeroStart * pageSize); //#A

            return query.Take(pageSize); //#B
        }
    }
}
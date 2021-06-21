// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices.Concrete
{
    /// <summary>
    /// This setups Tenants from a string containing a line for each tenant
    /// non-hierarchical: Tenant1
    /// hierarchical:     Company | West Coast | SanFran 
    /// </summary>
    public class BulkLoadTenantsService : IBulkLoadTenantsService
    {
        private readonly AuthPermissionsDbContext _context;

        /// <summary>
        /// requires access to the AuthPermissionsDbContext
        /// </summary>
        /// <param name="context"></param>
        public BulkLoadTenantsService(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This allows you to define tenants in a bulk load from a string. Each line in that string should hold a tenant
        /// (a line is ended with <see cref="Environment.NewLine"/>)
        /// If you are using a hierarchical tenant design, then you must define the higher company first
        /// </summary>
        /// <param name="linesOfText">If you are using a single layer then each line contains the a tenant name
        /// If you are using hierarchical tenant, then each line contains the whole hierarchy with '|' as separator, e.g.
        /// Holding company
        /// Holding company | USA branch 
        /// Holding company | USA branch | East Coast 
        /// Holding company | USA branch | East Coast | Washington
        /// Holding company | USA branch | East Coast | NewYork
        /// </param>
        /// <param name="options">The IAuthPermissionsOptions to check what type of tenant setting you have</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddTenantsToDatabaseAsync(string linesOfText, IAuthPermissionsOptions options)
        {
            var status = new StatusGenericHandler();

            if (string.IsNullOrEmpty(linesOfText))
                return status;

            //Check the options are set
            if (options.TenantType == TenantTypes.NotUsingTenants)
                return status.AddError(
                    $"You must set the options {nameof(AuthPermissionsOptions.TenantType)} to allow tenants to be processed");

            var lines = linesOfText.Split( Environment.NewLine);

            //Check for duplicate tenant names
            var dups = lines.GroupBy(line => line).Where(name => name.Count() > 1).ToList();
            if (dups.Any())
                return status.AddError("There were tenants with duplicate names, they are: " + string.Join(Environment.NewLine, dups.Select(x => x.Key)));

            if (options.TenantType == TenantTypes.SingleTenant)
            {
                foreach (var line in lines)
                {
                    _context.Add(new Tenant(line.Trim()));
                }

                return await _context.SaveChangesWithUniqueCheckAsync();
            }
            
            //--------------------------------------------------------------------
            //otherwise hierarchical, which is much more complex.
            //This decodes the hierarchical tenants
            var entries = new List<TenantNameDecoded>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                entries.Add(new TenantNameDecoded(lines[i], i));
            }

            var tenantLookup = new Dictionary<string, Tenant>();
            //This creates a group with the higher levels first
            var groupByLayers = entries.GroupBy(x => x.TenantNamesInOrder.Count);

            //This uses a transactions because its going to be calling SaveChanges for each layer
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                //This will save a layer, so that the next layer down can be saved
                foreach (var groupByLayer in groupByLayers)
                {
                    var tenantsToAddToDb = new List<Tenant>();
                    foreach (var tenantNameDecoded in groupByLayer)
                    {
                        Tenant parent = null;
                        if (tenantNameDecoded.ParentFullName != null)
                        {
                            if (!tenantLookup.TryGetValue(tenantNameDecoded.ParentFullName, out parent))
                                status.AddError(
                                    $"The tenant {tenantNameDecoded.TenantFullName} on line {tenantNameDecoded.LineNum} parent {tenantNameDecoded.ParentFullName} was not found");
                        }

                        if (tenantLookup.ContainsKey(tenantNameDecoded.TenantFullName))
                            status.AddError(
                                $"The tenant {tenantNameDecoded.TenantFullName} on line {tenantNameDecoded.LineNum} is a duplicate of the same name defined earlier");
                        var newTenant = Tenant.SetupHierarchicalTenant(tenantNameDecoded.TenantFullName, parent);
                        tenantsToAddToDb.Add(newTenant);
                        tenantLookup[tenantNameDecoded.TenantFullName] = newTenant;
                    }

                    if (status.IsValid)
                    {
                        //we add all the tenants in this layer
                        _context.AddRange(tenantsToAddToDb);

                        status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());
                    }
                }

                if (status.IsValid)
                    await transaction.CommitAsync();
            }

            return status;
        }

        //-----------------------------------------------------------
        //private parts

        private class TenantNameDecoded
        {
            public TenantNameDecoded(string line, int lineNum)
            {
                DecodeNamesDelimitedBy(line, '|');
                LineNum = lineNum;

                if (!TenantNamesInOrder.Any())
                    throw new AuthPermissionsException($"line {lineNum} produced no tenant names");

                ParentFullName = TenantNamesInOrder.Count > 1
                    ? CombineName(TenantNamesInOrder.Take(TenantNamesInOrder.Count - 1))
                    : (string)null;
                TenantFullName = CombineName(TenantNamesInOrder);
            }

            public List<string> TenantNamesInOrder { get; } = new List<string>();
            public List<int> TenantNameStartCharNum { get; } = new List<int>();

            public int LineNum { get; }

            public string ParentFullName { get; }
            public string TenantFullName { get; }

            private string CombineName(IEnumerable<string> names)
            {
                return string.Join(" | ", names);
            }

            private void DecodeNamesDelimitedBy(string line, char delimiterChar)
            {
                var charNum = 0;
                while (charNum < line.Length)
                {
                    if (line[charNum] == ' ')
                    {
                        charNum++;
                        continue;
                    }

                    var foundName = "";
                    var startOfName = charNum;
                    while (charNum < line.Length && line[charNum] != delimiterChar)
                    {
                        foundName += line[charNum];
                        charNum++;
                    }
                    if (foundName.Length > 0)
                    {
                        TenantNamesInOrder.Add(foundName.TrimEnd());
                        TenantNameStartCharNum.Add( startOfName);
                    }
                    charNum++;
                }
            }
        }

    }
}
// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Example1.RazorPages.IndividualAccounts.Services
{
    public class TestIHostedService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(10000);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
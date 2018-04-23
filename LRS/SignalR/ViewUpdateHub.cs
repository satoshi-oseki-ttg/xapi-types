using System;
using System.Threading.Tasks;
using bracken_lrs.Model;
using Microsoft.AspNetCore.SignalR;

namespace bracken_lrs.SignalR
{
    public class ViewUpdateHub : Hub
    {
        public Task Init(string username)
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("Update-view", "");
        }

        public Task Send(string message)
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("Send", message);
        }

        public Task Update(IViewModel viewModel)
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("Update-view", viewModel.ToJson());
        }

        public Task UpdateStatementCount()
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("update-statement-count");
        }

        public Task UpdateLastStatements()
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("update-last-statements");
        }

        public Task UpdateVerbStats()
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("update-verb-stats");
        }

        public Task UpdateProgress()
        {
            if (Clients == null) return Task.FromResult<string>(null);

            return Clients.All.InvokeAsync("update-progress");
        }
    }
}

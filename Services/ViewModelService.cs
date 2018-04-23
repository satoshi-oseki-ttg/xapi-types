using bracken_lrs.Model;
using bracken_lrs.SignalR;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    public class ViewModelService : IViewModelService
    {
        private readonly ViewUpdateHub _viewUpdateHub;
        // private readonly IViewModelCreationService _userViewModelCreationService;
        private readonly IViewModelCreationService _vmStatementCountService;
        private readonly IVmLastStatementsService _vmLastStatementsService;
        private readonly IVmVerbStatsService _vmVerbStatsService;
        private readonly IVmProgressService _progressService;
        
        private StatementSummaryViewModel _statementSummary = new StatementSummaryViewModel();

        // private UserViewModel userViewModel;

        public ViewModelService(ViewUpdateHub viewUpdateHub,
            IViewModelCreationService vmStatementCountService,
            IVmLastStatementsService vmLastStatementsService,
            IVmVerbStatsService vmVerbStatsService,
            IVmProgressService progressService)
        {
            _viewUpdateHub = viewUpdateHub;
           // _userViewModelCreationService = userViewModelCreationService;
            _vmStatementCountService = vmStatementCountService;
            _vmLastStatementsService = vmLastStatementsService;
            _vmVerbStatsService = vmVerbStatsService;
            _progressService = progressService;
            _vmLastStatementsService.Limit = 5;
        }

        public void Update(JObject statement)
        {
            var st = new Statement(statement);
            //_userViewModelCreationService.Process(st);
            _vmStatementCountService.Process(st);
            _vmLastStatementsService.Process(st);
            _vmVerbStatsService.Process(st);
            _progressService.Process(st);
            _viewUpdateHub.UpdateStatementCount();
            _viewUpdateHub.UpdateLastStatements();
            _viewUpdateHub.UpdateVerbStats();
            _viewUpdateHub.UpdateProgress();
            // _statementSummary.Add(st.verb.id);

            // _viewUpdateHub.Send("View updated");
            // _viewUpdateHub.Update(_statementSummary);
        }
    }
}

using System;
using GraphQL.Types;
using bracken_lrs.GraphQL.Types;
using bracken_lrs.GraphQL.Services;

namespace bracken_lrs.GraphQL
{
    class LrsQuery : ObjectGraphType
    {
        private readonly ILrsQueryService _lrsQueryService;
        private const string _statementIDParamName = "statementID";
        private const string _publishedResultIDParamName = "publishedResultID";
        private const string _usernameParamName = "username";
        private const string _courseNameParamName = "courseName";

        public LrsQuery(ILrsQueryService lrsQueryService)
        {
            _lrsQueryService = lrsQueryService;

            Field<LrsType>(
                "lrs",
                "Get statements",
                // new QueryArguments(
                //     new QueryArgument<StringGraphType> { Name = _lessonIDParamName },
                //     new QueryArgument<StringGraphType> { Name = _publishedResultIDParamName }
                // ),
                resolve: context =>
                {
                    // var lessonID = context.GetArgument<Guid>(_lessonIDParamName);
                    // var publishedResultID = context.GetArgument<Guid>(_publishedResultIDParamName);
                    return _lrsQueryService.GetLrsModel();
                }
            );

            Field<StringGraphType>(
                "statementCount",
                "Get number of statements",
                resolve: context =>
                {
                    return _lrsQueryService.GetNumberOfStatements();
                }
            );

            Field<StatementType>(
                "statement",
                "Get statement by id",
                new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = _statementIDParamName }
                ),
                resolve: context =>
                {
                    var statementID = context.GetArgument<Guid>(_statementIDParamName);
                    // var publishedResultID = context.GetArgument<Guid>(_publishedResultIDParamName);
                    return _lrsQueryService.GetStatement(statementID);
                }
            );

            Field<ListGraphType<ProgressType>>(
                "lastStatements",
                "Get list of last 5 statements",
                resolve: context =>
                {
                    return _lrsQueryService.GetLastStatements();
                }
            );

            Field<ListGraphType<VerbStatsType>>(
                "verbStats",
                "Get verb stats",
                resolve: context =>
                {
                    return _lrsQueryService.GetVerbStats();
                }
            );

            Field<ListGraphType<ProgressType>>(
                "progress",
                "Get users' progress",
                resolve: context =>
                {
                    return _lrsQueryService.GetProgress();
                }
            );

            Field<ListGraphType<AgentType>>(
                "agents",
                "Get list of actors",
                resolve: context =>
                {
                    return _lrsQueryService.GetActors();
                }
            );

            Field<ListGraphType<AgentType>>(
                "agentsInCourse",
                "Get list of actors",
                new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = _courseNameParamName }
                ),
                resolve: context =>
                {
                    var courseName = context.GetArgument<string>(_courseNameParamName);

                    return _lrsQueryService.GetActorsInCourse(courseName);
                }
            );

            Field<ListGraphType<StatementObjectType>>(
                "courses",
                "Get list of courses",
                resolve: context =>
                {
                    return _lrsQueryService.GetCourses();
                }
            );

            Field<ListGraphType<StatementType>>(
                "userCourseActivities",
                "Get list of activities that the user performed",
                new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = _courseNameParamName },
                    new QueryArgument<StringGraphType> { Name = _usernameParamName }
                ),
                resolve: context =>
                {
                    var courseName = context.GetArgument<string>(_courseNameParamName);
                    var username = context.GetArgument<string>(_usernameParamName);

                    return _lrsQueryService.GetCourseUserStatements(courseName, username);
                }
            );
        }
    }
}

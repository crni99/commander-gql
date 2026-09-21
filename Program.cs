using CommanderGQL.Data;
using CommanderGQL.GraphQL;
using CommanderGQL.GraphQL.Commands;
using CommanderGQL.GraphQL.Platforms;
using GraphQL.Server.Ui.Voyager;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("CommandConStr");
builder.Services.AddPooledDbContextFactory<AppDbContext>(opt => opt.UseSqlServer(connectionString));

builder.Services
	.AddGraphQLServer()
	.RegisterDbContextFactory<AppDbContext>()
	.AddQueryType<Query>()
	.AddMutationType<Mutation>()
	.AddSubscriptionType<Subscription>()
	.AddType<PlatformType>()
	.AddType<AddPlatformInputType>()
	.AddType<AddPlatformPayloadType>()
	.AddType<CommandType>()
	.AddType<AddCommandInputType>()
	.AddType<AddCommandPayloadType>()
	.AddFiltering()
	.AddSorting()
	.AddInMemorySubscriptions();

var app = builder.Build();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
	endpoints.MapGraphQL();
});

app.MapGraphQLVoyager("/graphql-voyager", new VoyagerOptions
{
	GraphQLEndPoint = "/graphql"
});

app.Run();

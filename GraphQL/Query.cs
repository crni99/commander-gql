using CommanderGQL.Data;
using CommanderGQL.Models;

namespace CommanderGQL.GraphQL
{
	/// <summary>
	/// Represents the queries available.
	/// </summary>
	[GraphQLDescription("Represents the queries available.")]
	public class Query
	{
		/// <summary>
		/// Gets the queryable <see cref="Platform"/>.
		/// </summary>
		/// <param name="context">The <see cref="AppDbContext"/>.</param>
		/// <returns>The queryable <see cref="Platform"/>.</returns>
		[UseFiltering]
		[UseSorting]
		[GraphQLDescription("Gets the queryable platform.")]
		public IQueryable<Platform> GetPlatform(AppDbContext context)
		{
			return context.Platforms;
		}

		/// <summary>
		/// Gets the queryable <see cref="Command"/>.
		/// </summary>
		/// <param name="context">The <see cref="AppDbContext"/>.</param>
		/// <returns>The queryable <see cref="Command"/>.</returns>
		[UseFiltering]
		[UseSorting]
		[GraphQLDescription("Gets the queryable command.")]
		public IQueryable<Command> GetCommand(AppDbContext context)
		{
			return context.Commands;
		}
	}
}
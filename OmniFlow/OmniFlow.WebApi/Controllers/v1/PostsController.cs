using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Features.Posts.Commands.CreatePost;
using OmniFlow.Application.Features.Posts.Commands.DeletePost;
using OmniFlow.Application.Features.Posts.Commands.UpdatePost;
using OmniFlow.Application.Features.Posts.Commands.UpvotePost;
using OmniFlow.Application.Features.Posts.Queries.GetPostById;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Posts API endpoints - CRUD operations and upvote support.
/// All endpoints require authentication.
/// </summary>
public class PostsController : BaseApiController
{
	/// <summary>Get a specific post by ID.</summary>
	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById([FromRoute] Guid id)
	{
		var query = new GetPostByIdQuery
		{
			PostId = id
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	/// <summary>Create a new post.</summary>
	[HttpPost]
	[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
	{
		var command = new CreatePostCommand
		{
			TripId = request.TripId,
			PlaceId = request.PlaceId,
			PostType = request.PostType,
			Content = request.Content,
			Photos = request.Photos,
			Tags = request.Tags,
			AiTags = request.AiTags,
			LocationLatitude = request.LocationLatitude,
			LocationLongitude = request.LocationLongitude,
			City = request.City,
			Country = request.Country
		};

		var postId = await Mediator.Send(command);
		return CreatedAtAction(nameof(GetById), new { id = postId }, postId);
	}

	/// <summary>Update an existing post.</summary>
	[HttpPut("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePostRequest request)
	{
		var command = new UpdatePostCommand
		{
			PostId = id,
			Content = request.Content,
			Tags = request.Tags
		};

		await Mediator.Send(command);
		return NoContent();
	}

	/// <summary>Delete a post (soft delete).</summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] Guid id)
	{
		var command = new DeletePostCommand
		{
			PostId = id
		};

		await Mediator.Send(command);
		return NoContent();
	}

	/// <summary>Upvote a post.</summary>
	[HttpPost("{id:guid}/upvote")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Upvote([FromRoute] Guid id)
	{
		var command = new UpvotePostCommand
		{
			PostId = id
		};

		await Mediator.Send(command);
		return NoContent();
	}
}

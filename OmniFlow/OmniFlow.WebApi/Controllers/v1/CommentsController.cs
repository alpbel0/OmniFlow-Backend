using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.Features.Comments.Commands.CreateComment;
using OmniFlow.Application.Features.Comments.Commands.DeleteComment;
using OmniFlow.Application.Features.Comments.Commands.UpvoteComment;
using OmniFlow.Application.Features.Comments.Queries.GetCommentsByPost;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Comment API endpoints for post comments, replies, delete, and upvote.
/// All endpoints require authentication.
/// </summary>
public class CommentsController : BaseApiController
{
	/// <summary>Get comments for a post, ordered by creation time.</summary>
	[HttpGet("/api/v1/posts/{postId:guid}/comments")]
	[ProducesResponseType(typeof(OmniFlow.Application.Wrappers.PagedResponse<CommentResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetByPost([FromRoute] Guid postId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		var query = new GetCommentsByPostQuery
		{
			PostId = postId,
			PageNumber = pageNumber,
			PageSize = pageSize
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	/// <summary>Create a comment or reply on a post.</summary>
	[HttpPost("/api/v1/posts/{postId:guid}/comments")]
	[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> Create([FromRoute] Guid postId, [FromBody] CreateCommentRequest request)
	{
		var command = new CreateCommentCommand
		{
			PostId = postId,
			ParentCommentId = request.ParentCommentId,
			Content = request.Content,
			Mentions = request.Mentions
		};

		var commentId = await Mediator.Send(command);
		return CreatedAtAction(nameof(GetByPost), new { postId }, commentId);
	}

	/// <summary>Delete a comment (soft delete).</summary>
	[HttpDelete("/api/v1/comments/{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] Guid id)
	{
		var command = new DeleteCommentCommand
		{
			CommentId = id
		};

		await Mediator.Send(command);
		return NoContent();
	}

	/// <summary>Upvote a comment.</summary>
	[HttpPost("/api/v1/comments/{id:guid}/upvote")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Upvote([FromRoute] Guid id)
	{
		var command = new UpvoteCommentCommand
		{
			CommentId = id
		};

		await Mediator.Send(command);
		return NoContent();
	}
}
